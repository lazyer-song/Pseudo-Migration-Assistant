using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace C2D;

internal static class SelfTest
{
    /// <summary>台账读写的往返测试。</summary>
    public static int RunStateTest(string reportPath)
    {
        var sb = new StringBuilder();
        void Log(string s)
        {
            sb.AppendLine(s);
            try { File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8); } catch { }
        }

        try
        {
            string dir = Path.Combine(Path.GetTempPath(), "c2d-statetest-" + Environment.ProcessId);
            State.RootOverride = dir;
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }

            string id = "test_" + DateTime.Now.Ticks;
            var r = new MigrationRecord
            {
                Id = id,
                Src = @"C:\测试路径\SomeApp",
                Dst = @"D:\Migrated\C\测试路径\SomeApp",
                Bak = @"C:\测试路径\SomeApp.__bak_20261020",
                Bytes = 13314398617,
                Files = 38201,
                At = DateTime.Now.ToString("O"),
                Status = "linked",
            };
            State.Append(r);

            var list = State.Load();
            var found = list.Find(x => x.Id == id);
            Log($"count={list.Count}");
            Log($"src={found?.Src}");
            Log($"dst={found?.Dst}");
            Log($"bak={found?.Bak}");
            Log($"bytes={found?.Bytes} files={found?.Files} status={found?.Status}");

            State.Append(new MigrationRecord { Id = id, Status = "rolledback" });
            var list2 = State.Load();
            var f2 = list2.Find(x => x.Id == id);
            Log($"after-update status={f2?.Status} src={f2?.Src} bytes={f2?.Bytes}");

            bool pass = found is not null
                        && found.Src == r.Src
                        && found.Dst == r.Dst
                        && found.Bak == r.Bak
                        && found.Bytes == r.Bytes
                        && found.Files == r.Files
                        && found.Status == "linked"
                        && f2 is not null
                        && f2.Status == "rolledback"
                        && f2.Src == r.Src;

            Log(pass ? "RESULT = PASS" : "RESULT = FAIL");
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
            return pass ? 0 : 1;
        }
        catch (Exception ex)
        {
            Log("EXCEPTION: " + ex);
            return 9;
        }
    }

    /// <summary>端到端：复制 → 校验 → 换位 → 建链 → 探针 → 回滚。</summary>
    public static int RunSwap(string src, string dst, string reportPath)
    {
        var sb = new StringBuilder();
        void Log(string s)
        {
            sb.AppendLine(s);
            try { File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8); } catch { }
        }

        var p = new ProgressState();
        string incoming = dst + ".__incoming__";

        try
        {
            Log($"src = {src}");
            Log($"dst = {dst}");

            var scan = Engine.Scan(src, p);
            Log($"[scan] files={scan.TotalFiles} dirs={scan.TotalDirs} bytes={scan.TotalBytes} links={scan.ReparsePoints.Count}");
            if (scan.SparseFiles.Count > 0)
            {
                Log("RESULT = REFUSED");
                return 3;
            }

            string parent = Path.GetDirectoryName(dst.TrimEnd('\\')) ?? dst;
            string? werr = Engine.CheckWritable(parent);
            if (werr is not null)
            {
                Log("[preflight] " + werr);
                Log("RESULT = REFUSED");
                return 4;
            }

            if (Directory.Exists(dst)) Directory.Delete(PathUtil.Long(dst), true);

            p.TotalBytes = Math.Max(1, scan.TotalBytes);
            p.TotalFiles = scan.TotalFiles;
            var copy = Engine.Copy(src, incoming, p, 8);
            Log($"[copy] files={copy.FilesCopied} errors={copy.Errors} linksPreserved={copy.LinksPreserved} linksSkipped={copy.LinksSkipped}");

            p.DoneFiles = 0;
            var verify = Engine.Verify(src, incoming, p);
            Log($"[verify] checked={verify.Checked} missing={verify.Missing} " +
                $"size={verify.SizeMismatch} time={verify.TimeMismatch}");

            var swap = Engine.Swap(src, dst, incoming, p);
            Log($"[swap] ok={swap.Ok} bak={swap.BakPath} err={swap.Error}");
            if (!swap.Ok) { Log("RESULT = FAIL"); return 1; }

            bool isLink = Junction.IsJunction(src);
            bool contentViaLink = Directory.Exists(Path.Combine(src, "dir00"));
            bool sameFileCount = verify.Checked == scan.TotalFiles;

            bool linkPreserved = true;
            if (scan.ReparsePoints.Count > 0)
            {
                string name = Path.GetFileName(scan.ReparsePoints[0].TrimEnd('\\'));
                linkPreserved = Junction.IsJunction(Path.Combine(dst, name));
            }

            Log($"[check] junction={isLink} contentViaLink={contentViaLink} dstFiles={sameFileCount} linkPreserved={linkPreserved}");

            bool rb = Engine.Rollback(src, swap.BakPath, dst, out string rberr, out string rbwarn);
            bool restored = Directory.Exists(src) && !Junction.IsJunction(src);
            bool bakGone = !Directory.Exists(swap.BakPath);
            bool dstGone = !Directory.Exists(dst);
            Log($"[rollback] ok={rb} err={rberr} warn={rbwarn} restored={restored} bakGone={bakGone} dstGone={dstGone}");

            bool pass = isLink && contentViaLink && sameFileCount && linkPreserved && rb && restored && bakGone && dstGone;
            Log(pass ? "RESULT = PASS" : "RESULT = FAIL");
            return pass ? 0 : 1;
        }
        catch (Exception ex)
        {
            Log("EXCEPTION: " + ex);
            Log("RESULT = FAIL");
            return 9;
        }
    }

    public static int Run(string src, string dst, string reportPath)
    {
        var sb = new StringBuilder();
        void Log(string s)
        {
            sb.AppendLine(s);
            try { File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8); } catch { }
        }

        try
        {
            Log($"src = {src}");
            Log($"dst = {dst}");

            try
            {
                NativeFs.GetDiskFreeSpaceExW(Path.GetPathRoot(dst) ?? "D:\\",
                    out ulong free, out ulong total, out _);
                Log($"[disk] free={Engine.Human((long)free)} total={Engine.Human((long)total)}");
            }
            catch (Exception ex)
            {
                Log("[disk] " + ex.Message);
            }

            var p = new ProgressState();
            p.Stage = (int)Stage.Scanning;

            // 1. 扫描
            var scan = Engine.Scan(src, p);
            Log($"[scan] files={scan.TotalFiles} dirs={scan.TotalDirs} bytes={scan.TotalBytes} ({Engine.Human(scan.TotalBytes)}) " +
                $"max={Engine.Human(scan.MaxFileBytes)} in {scan.ElapsedMs} ms");
            Log($"[scan] reparse={scan.ReparsePoints.Count} sparse={scan.SparseFiles.Count} over260={scan.Over260} " +
                $"longest={scan.LongestPath} denied={scan.DeniedDirs}");

            if (scan.SparseFiles.Count > 0)
            {
                Log("[scan] ABORT: 含稀疏文件，按预检规则拒绝");
                Log("RESULT = REFUSED");
                File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
                return 3;
            }

            string? werr = Engine.CheckWritable(dst);
            if (werr is not null)
            {
                Log("[preflight] 目标不可写: " + werr);
                Log("RESULT = REFUSED");
                File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
                return 4;
            }

            // 2. 复制
            p.TotalBytes = Math.Max(1, scan.TotalBytes);
            p.TotalFiles = scan.TotalFiles;
            p.DoneBytes = 0;
            p.DoneFiles = 0;
            p.Stage = (int)Stage.Copying;

            var copySw = Stopwatch.StartNew();
            var copy = Engine.Copy(src, dst, p, 8);
            copySw.Stop();

            double mbps = copySw.ElapsedMilliseconds > 0
                ? copy.BytesCopied / 1024.0 / 1024.0 / (copySw.ElapsedMilliseconds / 1000.0)
                : 0;

            Log($"[copy] files={copy.FilesCopied} bytes={copy.BytesCopied} errors={copy.Errors} " +
                $"in {copySw.ElapsedMilliseconds} ms ({mbps:F1} MB/s)");
            foreach (var e in copy.ErrorMessages) Log("  ! " + e);

            // 3. 校验
            p.Stage = (int)Stage.Verifying;
            p.DoneFiles = 0;
            var verify = Engine.Verify(src, dst, p);
            Log($"[verify] checked={verify.Checked} missing={verify.Missing} size={verify.SizeMismatch} " +
                $"time={verify.TimeMismatch} in {verify.ElapsedMs} ms");
            foreach (var s in verify.Samples) Log("  ! " + s);

            bool ok = copy.Errors == 0 && verify.Missing == 0 &&
                      verify.SizeMismatch == 0 && verify.TimeMismatch == 0;

            Log(ok ? "RESULT = PASS" : "RESULT = FAIL");
            File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
            return ok ? 0 : 1;
        }
        catch (Exception ex)
        {
            Log("EXCEPTION: " + ex);
            try { File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8); } catch { }
            return 9;
        }
    }
}
