using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace C2D;

internal enum Stage
{
    Idle = 0,
    Scanning = 1,
    Copying = 2,
    Verifying = 3,
    Switching = 4,
    Done = 5,
    Failed = 6,
}

internal sealed class ProgressState
{
    public volatile int Stage;
    public long TotalBytes = 1;
    public long DoneBytes;
    public int TotalFiles;
    public int DoneFiles;
    public volatile string CurrentFile = "";
    public volatile string Detail = "";
    public volatile bool CancelRequested;
    public int SkippedLinks;
    public int LinksPreserved;

    public void SetFile(string path)
    {
        CurrentFile = path;
    }

    public double Fraction
    {
        get
        {
            long total = Interlocked.Read(ref TotalBytes);
            long done = Interlocked.Read(ref DoneBytes);
            if (total <= 0) return 0;
            double f = (double)done / total;
            return f < 0 ? 0 : (f > 1 ? 1 : f);
        }
    }
}

internal sealed class ScanResult
{
    public long TotalBytes;
    public int TotalFiles;
    public int TotalDirs;
    public long MaxFileBytes;
    public int LongestPath;
    public int Over260;
    public int DeniedDirs;
    public readonly List<string> ReparsePoints = new();
    public readonly List<string> SparseFiles = new();
    public readonly List<string> DeniedPaths = new();
    public long ElapsedMs;
}

internal sealed class CopyResult
{
    public int FilesCopied;
    public int Errors;
    public long BytesCopied;
    public long ElapsedMs;
    public int LinksPreserved;
    public int LinksSkipped;
    public readonly List<string> ErrorMessages = new();
}

internal sealed class LinkCounters
{
    public int Preserved;
    public int Skipped;
}

internal sealed class VerifyResult
{
    public int Checked;
    public int Missing;
    public int SizeMismatch;
    public int TimeMismatch;
    public long ElapsedMs;
    public readonly List<string> Samples = new();
}

internal sealed class FileJob
{
    public string Src = "";
    public string Dst = "";
    public long Size;
}

internal sealed class SwapResult
{
    public bool Ok;
    public string Error = "";
    public string BakPath = "";
}

internal static class Engine
{
    // ---------- 扫描 ----------

    public static ScanResult Scan(string root, ProgressState? p = null)
    {
        var r = new ScanResult();
        var sw = Stopwatch.StartNew();
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            if (p is not null && p.CancelRequested) break;

            string dir = stack.Pop();
            string pattern = PathUtil.Long(dir) + "\\*";
            var fd = new NativeFs.WIN32_FIND_DATA();
            IntPtr h = NativeFs.FindFirstFileExW(pattern, 0, out fd, 0, IntPtr.Zero, 2);
            if (h == NativeFs.INVALID_HANDLE_VALUE)
            {
                r.DeniedDirs++;
                if (r.DeniedPaths.Count < 10) r.DeniedPaths.Add(dir);
                continue;
            }

            try
            {
                do
                {
                    string name = fd.cFileName;
                    if (name == "." || name == "..") continue;

                    string full = PathUtil.Join(dir, name);
                    if (full.Length > r.LongestPath) r.LongestPath = full.Length;
                    if (full.Length > 259 && r.Over260 < 50) r.Over260++;

                    if (fd.IsReparse)
                    {
                        r.ReparsePoints.Add(full);
                        continue;
                    }

                    if (fd.IsDirectory)
                    {
                        r.TotalDirs++;
                        stack.Push(full);
                        continue;
                    }

                    long size = fd.Size;
                    r.TotalFiles++;
                    r.TotalBytes += size;
                    if (size > r.MaxFileBytes) r.MaxFileBytes = size;
                    if (fd.IsSparse) r.SparseFiles.Add(full);

                    if (p is not null)
                    {
                        Interlocked.Exchange(ref p.TotalBytes, Math.Max(1, r.TotalBytes));
                        p.TotalFiles = r.TotalFiles;
                        p.SetFile(full);
                    }
                } while (NativeFs.FindNextFileW(h, out fd));
            }
            finally
            {
                NativeFs.FindClose(h);
            }
        }

        sw.Stop();
        r.ElapsedMs = sw.ElapsedMilliseconds;
        return r;
    }

    // ---------- 复制 ----------

    public static CopyResult Copy(string srcRoot, string dstRoot, ProgressState p, int workers = 8)
    {
        var result = new CopyResult();
        var sw = Stopwatch.StartNew();
        var queue = new BlockingCollection<FileJob>(Math.Max(16, workers * 4));
        var dirs = new List<(string src, string dst)>();
        var errors = new ConcurrentBag<string>();
        var links = new LinkCounters();
        long bytesCopied = 0;
        int filesCopied = 0;

        Directory.CreateDirectory(PathUtil.Long(dstRoot));

        var producer = new Thread(() =>
        {
            try
            {
                Produce(srcRoot, dstRoot, queue, dirs, p, links);
            }
            catch (Exception ex)
            {
                errors.Add("枚举失败: " + ex.Message);
            }
            finally
            {
                queue.CompleteAdding();
            }
        })
        { IsBackground = true, Name = "c2d-producer" };
        producer.Start();

        var consumers = new List<Thread>();
        for (int i = 0; i < workers; i++)
        {
            var t = new Thread(() =>
            {
                foreach (var job in queue.GetConsumingEnumerable())
                {
                    if (p.CancelRequested) continue;

                    p.SetFile(job.Src);
                    if (CopyOne(job, p, out string err))
                    {
                        Interlocked.Increment(ref filesCopied);
                        Interlocked.Add(ref bytesCopied, job.Size);
                        Interlocked.Increment(ref p.DoneFiles);
                    }
                    else
                    {
                        errors.Add(err);
                    }
                }
            })
            { IsBackground = true, Name = "c2d-copy" };
            t.Start();
            consumers.Add(t);
        }

        producer.Join();
        foreach (var t in consumers) t.Join();

        // 目录时间戳：子目录先于父目录，倒序回填
        for (int i = dirs.Count - 1; i >= 0; i--)
        {
            try
            {
                string s = PathUtil.Long(dirs[i].src);
                string d = PathUtil.Long(dirs[i].dst);
                Directory.SetLastWriteTimeUtc(d, Directory.GetLastWriteTimeUtc(s));
            }
            catch
            {
                // 目录时间戳失败不影响数据正确性
            }
        }

        sw.Stop();
        result.FilesCopied = filesCopied;
        result.BytesCopied = bytesCopied;
        result.Errors = errors.Count;
        result.ElapsedMs = sw.ElapsedMilliseconds;
        result.LinksPreserved = links.Preserved;
        result.LinksSkipped = links.Skipped;
        result.ErrorMessages.AddRange(errors);
        return result;
    }

    private static void Produce(string srcRoot, string dstRoot, BlockingCollection<FileJob> queue,
        List<(string src, string dst)> dirs, ProgressState p, LinkCounters links)
    {
        var stack = new Stack<(string src, string dst)>();
        stack.Push((srcRoot, dstRoot));

        while (stack.Count > 0)
        {
            if (p.CancelRequested) return;

            var (sdir, ddir) = stack.Pop();
            dirs.Add((sdir, ddir));

            string pattern = PathUtil.Long(sdir) + "\\*";
            var fd = new NativeFs.WIN32_FIND_DATA();
            IntPtr h = NativeFs.FindFirstFileExW(pattern, 0, out fd, 0, IntPtr.Zero, 2);
            if (h == NativeFs.INVALID_HANDLE_VALUE) continue;

            try
            {
                do
                {
                    string name = fd.cFileName;
                    if (name == "." || name == "..") continue;

                    string s = PathUtil.Join(sdir, name);
                    string d = PathUtil.Join(ddir, name);

                    if (fd.IsReparse)
                    {
                        // 链接不是数据，按原样在新位置重建，而不是跟进去复制
                        if (Junction.TryGetTarget(s, out string target) &&
                            Junction.Create(d, target, out _))
                        {
                            links.Preserved++;
                        }
                        else
                        {
                            links.Skipped++;
                        }
                        continue;
                    }

                    if (fd.IsDirectory)
                    {
                        Directory.CreateDirectory(PathUtil.Long(d));
                        stack.Push((s, d));
                    }
                    else
                    {
                        queue.Add(new FileJob { Src = s, Dst = d, Size = fd.Size }, CancellationToken.None);
                    }
                } while (NativeFs.FindNextFileW(h, out fd));
            }
            finally
            {
                NativeFs.FindClose(h);
            }
        }
    }

    private static bool CopyOne(FileJob job, ProgressState p, out string error)
    {
        error = "";
        long added = 0;

        try
        {
            NativeFs.CopyProgressRoutine cb = (total, transferred, streamSize, streamTransferred,
                streamNumber, reason, hSrc, hDst, data) =>
            {
                long delta = transferred - added;
                if (delta > 0)
                {
                    added = transferred;
                    Interlocked.Add(ref p.DoneBytes, delta);
                }
                return p.CancelRequested ? 1 : 0; // PROGRESS_CANCEL
            };

            int cancel = 0;
            bool ok = NativeFs.CopyFileExW(
                PathUtil.Long(job.Src), PathUtil.Long(job.Dst),
                cb, IntPtr.Zero, ref cancel, 0);

            if (!ok)
            {
                int code = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                error = $"{job.Src}\n    0x{code:X8}  {Hint(code)}";
                return false;
            }

            long rest = job.Size - added;
            if (rest > 0) Interlocked.Add(ref p.DoneBytes, rest);

            GC.KeepAlive(cb);
            return true;
        }
        catch (Exception ex)
        {
            error = job.Src + ": " + ex.Message;
            return false;
        }
    }

    // ---------- 校验（L1：大小 + 最后写入时间） ----------

    public static VerifyResult Verify(string srcRoot, string dstRoot, ProgressState p)
    {
        var r = new VerifyResult();
        var sw = Stopwatch.StartNew();
        var stack = new Stack<(string src, string dst)>();
        stack.Push((srcRoot, dstRoot));

        while (stack.Count > 0)
        {
            if (p.CancelRequested) break;

            var (sdir, ddir) = stack.Pop();
            string pattern = PathUtil.Long(ddir) + "\\*";
            var fd = new NativeFs.WIN32_FIND_DATA();
            IntPtr h = NativeFs.FindFirstFileExW(pattern, 0, out fd, 0, IntPtr.Zero, 2);
            if (h == NativeFs.INVALID_HANDLE_VALUE) continue;

            try
            {
                do
                {
                    string name = fd.cFileName;
                    if (name == "." || name == "..") continue;

                    string s = PathUtil.Join(sdir, name);
                    string d = PathUtil.Join(ddir, name);

                    // 链接不跟进去：既不算文件，也避免自引用导致死循环
                    if (fd.IsReparse) continue;

                    if (fd.IsDirectory)
                    {
                        stack.Push((s, d));
                        continue;
                    }

                    r.Checked++;
                    Interlocked.Increment(ref p.DoneFiles);
                    p.SetFile(d);

                    var fi = new FileInfo(PathUtil.Long(s));
                    if (!fi.Exists)
                    {
                        r.Missing++;
                        if (r.Samples.Count < 10) r.Samples.Add("缺失源文件: " + s);
                        continue;
                    }

                    if (fi.Length != fd.Size)
                    {
                        r.SizeMismatch++;
                        if (r.Samples.Count < 10)
                            r.Samples.Add($"大小不一致 {fi.Length} != {fd.Size}: {s}");
                        continue;
                    }

                    long srcTime = fi.LastWriteTimeUtc.ToFileTimeUtc();
                    if (srcTime != fd.WriteTimeUtc)
                    {
                        r.TimeMismatch++;
                        if (r.Samples.Count < 10)
                            r.Samples.Add($"时间不一致: {s}");
                    }
                } while (NativeFs.FindNextFileW(h, out fd));
            }
            finally
            {
                NativeFs.FindClose(h);
            }
        }

        sw.Stop();
        r.ElapsedMs = sw.ElapsedMilliseconds;
        return r;
    }

    // ---------- 换位：改名 → 建链 → 探针 ----------

    public static bool IsEmptyDir(string path)
    {
        try
        {
            if (!Directory.Exists(path)) return true;
            using var e = Directory.EnumerateFileSystemEntries(PathUtil.Long(path)).GetEnumerator();
            return !e.MoveNext();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 试改名：改名是迁移最后一步的硬指标。提前探一次，能避免白复制几个 G 才发现改不了名。
    /// 改完立刻改回来，前后不到 1 毫秒。
    /// </summary>
    public static bool ProbeRename(string dir, out string error)
    {
        error = "";
        string tmp = dir + ".__c2d_probe__";

        try
        {
            if (Directory.Exists(PathUtil.Long(tmp))) Directory.Delete(PathUtil.Long(tmp), false);
        }
        catch
        {
            // 清理不掉不影响判定
        }

        try
        {
            Directory.Move(PathUtil.Long(dir), PathUtil.Long(tmp));
        }
        catch (UnauthorizedAccessException)
        {
            error = "拒绝访问：目录里有文件正被其他程序打开着。";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        try
        {
            Directory.Move(PathUtil.Long(tmp), PathUtil.Long(dir));
            return true;
        }
        catch (Exception ex)
        {
            error = "改名成功但没能改回来，请手动把\n" + tmp + "\n改回\n" + dir + "\n\n" + ex.Message;
            return false;
        }
    }

    public static SwapResult Swap(string src, string dst, string incoming, ProgressState p)
    {
        var r = new SwapResult();
        string bak = src + ".__bak_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        bool dstCreated = false;

        try
        {
            p.Stage = (int)Stage.Switching;

            // 1. 目标就位（incoming → 最终名，同卷原子改名）
            if (Directory.Exists(dst)) Directory.Delete(PathUtil.Long(dst), false);
            Directory.Move(PathUtil.Long(incoming), PathUtil.Long(dst));
            dstCreated = true;

            // 2. 源让位
            Directory.Move(PathUtil.Long(src), PathUtil.Long(bak));

            // 3. 建链接
            if (!Junction.Create(src, dst, out int err))
            {
                Directory.Move(PathUtil.Long(bak), PathUtil.Long(src));
                DeleteTree(dst, out _);
                r.Error = $"建链接失败（Win32 {err}），源目录已恢复原样。";
                return r;
            }

            // 4. 探针：通过 C 盘路径写，确认落在目标目录
            try
            {
                string probe = Path.Combine(src, ".c2d-probe.tmp");
                File.WriteAllText(probe, "c2d");
                if (!File.Exists(Path.Combine(dst, ".c2d-probe.tmp")))
                    throw new IOException("探针文件没有出现在目标目录，链接可能没生效");
                File.Delete(probe);
            }
            catch (Exception ex)
            {
                Junction.Delete(src);
                Directory.Move(PathUtil.Long(bak), PathUtil.Long(src));
                DeleteTree(dst, out _);
                r.Error = "探针验证失败，已自动回滚：" + ex.Message;
                return r;
            }

            r.Ok = true;
            r.BakPath = bak;
            return r;
        }
        catch (Exception ex)
        {
            // 尽力回到"源还是原样"的状态
            try
            {
                if (!Directory.Exists(src) && Directory.Exists(bak))
                    Directory.Move(PathUtil.Long(bak), PathUtil.Long(src));
            }
            catch { }

            // 已经就位的副本也删掉，别留 4 GB 垃圾在目标盘
            if (dstCreated) DeleteTree(dst, out _);

            r.Error = ex is UnauthorizedAccessException
                ? "原目录改不了名——里面的文件正被其他程序占用。\n\n" +
                  "关掉窗口往往不够，要彻底退出（微信/QQ 要退出托盘图标），\n" +
                  "或者有资源管理器窗口正停在这个目录里。"
                : "换位失败：" + ex.Message;
            return r;
        }
    }

    /// <summary>
    /// 递归删除。比 Directory.Delete(recursive) 多做三件事：
    /// 1) 先去掉只读属性（DeleteFile 对只读文件会直接拒绝）
    /// 2) 遇到链接只删链接本身，绝不跟进去
    /// 3) 全程用 \\?\ 前缀，长路径也能删
    /// </summary>
    public static bool DeleteTree(string root, out string error)
    {
        error = "";
        try
        {
            if (!Directory.Exists(root)) return true;

            if (Junction.IsJunction(root))
            {
                Directory.Delete(PathUtil.Long(root), false);
                return true;
            }

            var stack = new Stack<string>();
            stack.Push(root);
            var dirs = new List<string>();

            while (stack.Count > 0)
            {
                string dir = stack.Pop();
                dirs.Add(dir);

                foreach (var entry in new DirectoryInfo(PathUtil.Long(dir)).EnumerateFileSystemInfos())
                {
                    string full = PathUtil.Join(dir, entry.Name);

                    FileAttributes attr;
                    try { attr = entry.Attributes; }
                    catch { continue; }

                    if ((attr & FileAttributes.Directory) != 0)
                    {
                        if ((attr & FileAttributes.ReparsePoint) != 0)
                        {
                            try { Directory.Delete(PathUtil.Long(full), false); } catch { }
                        }
                        else
                        {
                            stack.Push(full);
                        }
                    }
                    else
                    {
                        if ((attr & FileAttributes.ReadOnly) != 0)
                        {
                            try { File.SetAttributes(PathUtil.Long(full), attr & ~FileAttributes.ReadOnly); }
                            catch { }
                        }
                        File.Delete(PathUtil.Long(full));
                    }
                }
            }

            for (int i = dirs.Count - 1; i >= 0; i--)
                Directory.Delete(PathUtil.Long(dirs[i]), false);

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// 回滚到迁移前的状态：删链接 → 备份改回原名 → 删掉目标副本。
    /// 只有"源目录恢复失败"才算失败；目标副本删不掉只给警告。
    /// </summary>
    public static bool Rollback(string src, string bak, string dst, out string error, out string warning)
    {
        error = "";
        warning = "";
        try
        {
            if (Junction.IsJunction(src))
            {
                if (!Junction.Delete(src))
                {
                    error = "删除链接失败，可能有程序正在使用这个目录。";
                    return false;
                }
            }
            else if (Directory.Exists(src))
            {
                error = "原路径上已经有一个真实目录（可能被程序重建），不能直接覆盖。";
                return false;
            }

            if (!Directory.Exists(bak))
            {
                error = "备份目录不存在：" + bak;
                return false;
            }

            Directory.Move(PathUtil.Long(bak), PathUtil.Long(src));

            // 目标副本是我们复制过去的，回滚就该一并清掉
            if (!DeleteTree(dst, out string delErr))
                warning = "源目录已恢复，但目标副本没删干净：" + delErr + "\n可手动删除：" + dst;

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static string Human(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double v = bytes;
        int i = 0;
        while (v >= 1024 && i < units.Length - 1)
        {
            v /= 1024;
            i++;
        }
        return v.ToString(i == 0 ? "0" : "0.0") + " " + units[i];
    }

    /// <summary>目标目录可写性预检。返回 null 表示可写，否则返回原因。</summary>
    public static string? CheckWritable(string dst)
    {
        try
        {
            Directory.CreateDirectory(PathUtil.Long(dst));
            string probe = PathUtil.Join(dst, ".c2d-write-test.tmp");
            File.WriteAllText(PathUtil.Long(probe), "c2d");
            File.Delete(PathUtil.Long(probe));
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            return "拒绝访问：" + ex.Message + "\n（探测位置：" + dst + "）";
        }
        catch (IOException ex)
        {
            return "写入失败：" + ex.Message + "\n（探测位置：" + dst + "）";
        }
        catch (Exception ex)
        {
            return ex.GetType().Name + "：" + ex.Message + "\n（探测位置：" + dst + "）";
        }
    }

    private static string TryWrite(string dir)
    {
        try
        {
            Directory.CreateDirectory(PathUtil.Long(dir));
            string f = PathUtil.Join(dir, ".c2d-wtest.tmp");
            File.WriteAllText(PathUtil.Long(f), "x");
            File.Delete(PathUtil.Long(f));
            return "可以";
        }
        catch (Exception ex)
        {
            return "不行（" + ex.GetType().Name + "：" + ex.Message + "）";
        }
    }

    /// <summary>读进程完整性级别：低 = 被沙箱/容器限制，中 = 普通桌面进程。</summary>
    public static string IntegrityLevel()
    {
        IntPtr token = IntPtr.Zero;
        IntPtr buffer = IntPtr.Zero;
        try
        {
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, out token)) return "未知";
            if (!GetTokenInformation(token, 25 /*TokenIntegrityLevel*/, IntPtr.Zero, 0, out int need))
            {
                const int ERROR_INSUFFICIENT_BUFFER = 122;
                if (Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER) return "未知";
            }

            buffer = Marshal.AllocHGlobal(need);
            if (!GetTokenInformation(token, 25, buffer, need, out _)) return "未知";

            IntPtr sid = Marshal.ReadIntPtr(buffer);
            int count = Marshal.ReadByte(sid, 1);
            if (count <= 0) return "未知";

            int last = Marshal.ReadInt32(sid, 8 + (count - 1) * 4);
            int level = last >> 12;
            return level switch
            {
                0 => "不受信任（Untrusted）",
                1 => "低（Low）",
                2 => "中（Medium，正常）",
                3 => "高（High，管理员）",
                4 => "系统（System）",
                _ => "未知(" + level + ")",
            };
        }
        catch
        {
            return "未知";
        }
        finally
        {
            if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
            if (token != IntPtr.Zero) CloseHandle(token);
        }
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr process, uint desiredAccess, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(IntPtr token, int infoClass,
        IntPtr info, int infoLength, out int returnLength);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    private const uint TOKEN_QUERY = 0x0008;

    /// <summary>写不进去的时候，把各位置的写入能力一次性测出来，省得来回猜。</summary>
    public static string Diagnose(string dst)
    {
        var sb = new System.Text.StringBuilder();

        string il = IntegrityLevel();
        string tempDir = Path.GetTempPath();
        string tempResult = TryWrite(tempDir);
        bool lowIl = il.StartsWith("低") || il.StartsWith("不受信任");
        bool restricted = lowIl || tempResult != "可以";

        if (restricted)
        {
            sb.AppendLine("⚠ 检测到受限进程");
            sb.AppendLine("   完整性级别：" + il + "，%TEMP% 写入：" + (tempResult == "可以" ? "可以" : "失败"));
            sb.AppendLine("   正常桌面进程不可能写不了自己的 %TEMP%。");
            sb.AppendLine("   请关掉本窗口，从资源管理器里直接双击 exe 运行。");
            sb.AppendLine();
        }

        sb.AppendLine("—— 环境自检 ——");
        sb.AppendLine("账户：" + Environment.UserDomainName + "\\" + Environment.UserName);
        sb.AppendLine("完整性级别：" + il);

        string exeDir;
        try { exeDir = Path.GetDirectoryName(Environment.ProcessPath ?? "") ?? "."; }
        catch { exeDir = "."; }

        sb.AppendLine("%TEMP% 写入：" + tempResult);
        sb.AppendLine("程序目录写入：" + TryWrite(exeDir));

        string? parent = null;
        try { parent = Path.GetDirectoryName(dst.TrimEnd('\\')); } catch { }
        if (!string.IsNullOrEmpty(parent))
            sb.AppendLine("目标父目录写入：" + TryWrite(parent));

        sb.AppendLine("目标位置写入：" + TryWrite(dst));
        return sb.ToString();
    }

    /// <summary>把 Win32 错误码翻成人话，弹窗里直接给用户看。</summary>
    public static string Hint(int code) => code switch
    {
        2 => "找不到路径",
        3 => "找不到路径",
        5 => "拒绝访问（目标不可写 / 权限不足 / 被安全软件拦截）",
        19 => "介质写保护",
        32 => "文件正被其他程序占用",
        33 => "文件被锁定",
        39 => "磁盘已满",
        112 => "磁盘空间不足",
        123 => "文件名语法错误",
        206 => "路径或文件名太长",
        1392 => "文件或目录已损坏",
        _ => "未知错误",
    };
}
