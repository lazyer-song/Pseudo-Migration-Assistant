using System;
using System.IO;
using System.Runtime.InteropServices;

namespace C2D;

/// <summary>目录联接（Junction）的创建与删除，不需要管理员权限。</summary>
internal static unsafe class Junction
{
    private const uint FSCTL_SET_REPARSE_POINT = 0x000900A4;
    private const uint FSCTL_GET_REPARSE_POINT = 0x000900A8;
    private const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;

    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint FILE_SHARE_DELETE = 0x00000004;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;

    private static readonly IntPtr InvalidHandle = (IntPtr)(-1);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
        IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    /// <summary>linkPath 必须是不存在或空目录；targetPath 必须是已存在的本地绝对路径。</summary>
    public static bool Create(string linkPath, string targetPath, out int error)
    {
        error = 0;
        try
        {
            Directory.CreateDirectory(PathUtil.Long(linkPath));
        }
        catch (Exception)
        {
            error = Marshal.GetLastWin32Error();
            return false;
        }

        string substitute = @"\??\" + targetPath;
        string print = targetPath;

        int subBytes = substitute.Length * 2;
        int printBytes = print.Length * 2;

        // MOUNT_POINT_REPARSE_BUFFER：4 个 USHORT + PathBuffer（无 Flags）
        int reparseDataLength = 8 + subBytes + 2 + printBytes + 2;
        int total = 8 + reparseDataLength;

        IntPtr buffer = Marshal.AllocHGlobal(total);
        try
        {
            byte* p = (byte*)buffer;
            *(uint*)p = IO_REPARSE_TAG_MOUNT_POINT;
            *(ushort*)(p + 4) = (ushort)reparseDataLength;
            *(ushort*)(p + 6) = 0;

            *(ushort*)(p + 8) = 0;                        // SubstituteNameOffset
            *(ushort*)(p + 10) = (ushort)subBytes;        // SubstituteNameLength
            *(ushort*)(p + 12) = (ushort)(subBytes + 2);  // PrintNameOffset
            *(ushort*)(p + 14) = (ushort)printBytes;      // PrintNameLength

            char* path = (char*)(p + 16);
            substitute.AsSpan().CopyTo(new Span<char>(path, substitute.Length));
            path[substitute.Length] = '\0';

            char* path2 = path + substitute.Length + 1;
            print.AsSpan().CopyTo(new Span<char>(path2, print.Length));
            path2[print.Length] = '\0';

            IntPtr h = CreateFileW(PathUtil.Long(linkPath), GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING,
                FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT, IntPtr.Zero);

            if (h == InvalidHandle)
            {
                error = Marshal.GetLastWin32Error();
                return false;
            }

            try
            {
                if (!DeviceIoControl(h, FSCTL_SET_REPARSE_POINT, buffer, (uint)total,
                        IntPtr.Zero, 0, out _, IntPtr.Zero))
                {
                    error = Marshal.GetLastWin32Error();
                    return false;
                }
            }
            finally
            {
                CloseHandle(h);
            }

            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static bool IsJunction(string path)
    {
        try
        {
            var attr = File.GetAttributes(PathUtil.Long(path));
            return (attr & FileAttributes.ReparsePoint) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>读出一个目录联接指向哪里（只支持 MOUNT_POINT，符号链接返回 false）。</summary>
    public static bool TryGetTarget(string linkPath, out string target)
    {
        target = "";

        IntPtr h = CreateFileW(PathUtil.Long(linkPath), 0,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            IntPtr.Zero, OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT, IntPtr.Zero);

        if (h == InvalidHandle) return false;

        IntPtr buffer = Marshal.AllocHGlobal(16 * 1024);
        try
        {
            if (!DeviceIoControl(h, FSCTL_GET_REPARSE_POINT, IntPtr.Zero, 0,
                    buffer, 16 * 1024, out _, IntPtr.Zero))
            {
                return false;
            }

            uint tag = (uint)Marshal.ReadInt32(buffer, 0);
            if (tag != IO_REPARSE_TAG_MOUNT_POINT) return false;

            int subOffset = Marshal.ReadInt16(buffer, 8);
            int subLength = Marshal.ReadInt16(buffer, 10);
            if (subLength <= 0) return false;

            string s = Marshal.PtrToStringUni(buffer + 16 + subOffset, subLength / 2) ?? "";
            if (s.StartsWith(@"\??\", StringComparison.Ordinal)) s = s.Substring(4);
            if (s.Length == 0) return false;

            target = s;
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
            CloseHandle(h);
        }
    }

    /// <summary>只删链接本身，绝不碰目标内容。</summary>
    public static bool Delete(string linkPath)
    {
        try
        {
            if (!IsJunction(linkPath)) return false;
            Directory.Delete(PathUtil.Long(linkPath), false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
