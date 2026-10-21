using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace C2D;

/// <summary>目录改不了名时，找出到底是谁占着。</summary>
internal static class Locks
{
    private const uint ERROR_MORE_DATA = 234;
    private const int CCH_RM_MAX_APP_NAME = 255;
    private const int CCH_RM_MAX_SVC_NAME = 63;

    [StructLayout(LayoutKind.Sequential)]
    private struct RM_UNIQUE_PROCESS
    {
        public int dwProcessId;
        public long ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RM_PROCESS_INFO
    {
        public RM_UNIQUE_PROCESS Process;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
        public string strAppName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
        public string strServiceShortName;
        public int ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;
        [MarshalAs(UnmanagedType.Bool)] public bool bRestartable;
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, StringBuilder strSessionKey);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint pSessionHandle);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(uint pSessionHandle, uint nFiles, string[] rgsFilenames,
        uint nApplications, RM_UNIQUE_PROCESS[]? rgApplications, uint nServices, string[]? rgsServiceNames);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded,
        ref uint pnProcInfo, [In, Out] RM_PROCESS_INFO[]? rgAffectedApps, out uint lpdwRebootReasons);

    /// <summary>先试改名再改回来，判断这一项能不能被移动。</summary>
    private static bool CanRename(string path)
    {
        string tmp = path + ".__c2d_t__";
        try
        {
            Directory.Move(PathUtil.Long(path), PathUtil.Long(tmp));
        }
        catch
        {
            return false;
        }

        try
        {
            Directory.Move(PathUtil.Long(tmp), PathUtil.Long(path));
            return true;
        }
        catch
        {
            // 改回来失败，尽力恢复
            try
            {
                if (!Directory.Exists(PathUtil.Long(path))) Directory.Move(PathUtil.Long(tmp), PathUtil.Long(path));
            }
            catch { }
            return false;
        }
    }

    private static bool CanRenameFile(string path)
    {
        string tmp = path + ".__c2d_t__";
        try
        {
            File.Move(PathUtil.Long(path), PathUtil.Long(tmp));
        }
        catch
        {
            return false;
        }

        try
        {
            File.Move(PathUtil.Long(tmp), PathUtil.Long(path));
            return true;
        }
        catch
        {
            try
            {
                if (!File.Exists(PathUtil.Long(path))) File.Move(PathUtil.Long(tmp), PathUtil.Long(path));
            }
            catch { }
            return false;
        }
    }

    /// <summary>用重启管理器查出哪些进程占着这个路径。</summary>
    public static string FindProcesses(string path)
    {
        uint session;
        var key = new StringBuilder(64);
        int rc = RmStartSession(out session, 0, key);
        if (rc != 0) return $"[RM start rc={rc}]";

        try
        {
            rc = RmRegisterResources(session, 1, new[] { path }, 0, null, 0, null);
            if (rc != 0) return $"[RM register rc={rc}]";

            uint needed = 0;
            uint count = 0;
            rc = RmGetList(session, out needed, ref count, null, out _);
            if (rc != ERROR_MORE_DATA || needed == 0) return $"[RM list rc={rc} needed={needed}]";

            count = needed;
            var infos = new RM_PROCESS_INFO[count];
            rc = RmGetList(session, out needed, ref count, infos, out _);
            if (rc != 0) return $"[RM list2 rc={rc}]";

            var names = new List<string>();
            for (int i = 0; i < count && i < 5; i++)
            {
                if (string.IsNullOrEmpty(infos[i].strAppName)) continue;
                names.Add(infos[i].strAppName + " (PID " + infos[i].Process.dwProcessId + ")");
            }
            return names.Count > 0 ? string.Join("、", names) : $"[RM 返回 {count} 条但没有进程名]";
        }
        catch (Exception ex)
        {
            return "[RM 异常] " + ex.Message;
        }
        finally
        {
            RmEndSession(session);
        }
    }

    /// <summary>逐层下钻，找出第一个改不了名的子项。</summary>
    public static string Report(string dir)
    {
        var sb = new StringBuilder();

        if (CanRename(dir))
            return "（这一次目录能正常改名，可能刚才只是瞬时占用，直接重试即可）";

        sb.AppendLine("逐层排查，被占用的是：");
        string current = dir;

        for (int depth = 0; depth < 8; depth++)
        {
            string? locked = null;
            bool lockedIsFile = false;

            try
            {
                foreach (var entry in new DirectoryInfo(PathUtil.Long(current)).EnumerateFileSystemInfos())
                {
                    string full = PathUtil.Join(current, entry.Name);

                    FileAttributes attr;
                    try { attr = entry.Attributes; }
                    catch { continue; }

                    if ((attr & FileAttributes.ReparsePoint) != 0) continue;

                    if ((attr & FileAttributes.Directory) != 0)
                    {
                        if (!CanRename(full)) { locked = full; break; }
                    }
                    else
                    {
                        if ((attr & FileAttributes.ReadOnly) != 0) continue;
                        if (!CanRenameFile(full)) { locked = full; lockedIsFile = true; break; }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("  读取 " + current + " 失败：" + ex.Message);
                break;
            }

            if (locked is null)
            {
                sb.AppendLine("  " + current);
                sb.AppendLine("  （它的子项都能改名，可能是目录本身被某个进程打开着）");
                break;
            }

            current = locked;
            sb.AppendLine("  · " + current);

            if (lockedIsFile) break;
        }

        string procs = FindProcesses(current);
        if (procs.StartsWith("[", StringComparison.Ordinal)) procs = "";
        if (procs.Length == 0)
        {
            procs = FindProcesses(dir);
            if (procs.StartsWith("[", StringComparison.Ordinal)) procs = "";
        }

        sb.AppendLine();
        sb.AppendLine(procs.Length > 0
            ? "占用进程：" + procs
            : "占用进程：没能自动查出（可能是杀毒软件、系统服务，或文件被内存映射）。\n" +
              "可以按上面的路径手动确认，或者重启电脑后第一时间再试。");

        return sb.ToString();
    }
}
