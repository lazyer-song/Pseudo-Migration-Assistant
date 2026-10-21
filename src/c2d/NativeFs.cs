using System;
using System.Runtime.InteropServices;

namespace C2D;

internal static class NativeFs
{
    public const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
    public const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x200;
    public const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x400;

    public static readonly IntPtr INVALID_HANDLE_VALUE = (IntPtr)(-1);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WIN32_FIND_DATA
    {
        public uint dwFileAttributes;
        public uint ftCreationLow;
        public uint ftCreationHigh;
        public uint ftAccessLow;
        public uint ftAccessHigh;
        public uint ftWriteLow;
        public uint ftWriteHigh;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public string cAlternateFileName;

        public readonly bool IsDirectory => (dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0;
        public readonly bool IsReparse => (dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0;
        public readonly bool IsSparse => (dwFileAttributes & FILE_ATTRIBUTE_SPARSE_FILE) != 0;
        public readonly long Size => ((long)nFileSizeHigh << 32) | nFileSizeLow;
        public readonly long WriteTimeUtc =>
            ((long)ftWriteHigh << 32) | ftWriteLow;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr FindFirstFileExW(string lpFileName, int fInfoLevelId,
        out WIN32_FIND_DATA lpFindFileData, int fSearchOp, IntPtr lpSearchFilter, int dwAdditionalFlags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool FindNextFileW(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FindClose(IntPtr hFindFile);

    public delegate int CopyProgressRoutine(long totalFileSize, long totalBytesTransferred,
        long streamSize, long streamBytesTransferred, uint streamNumber, uint callbackReason,
        IntPtr sourceFile, IntPtr destinationFile, IntPtr data);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CopyFileExW(string lpExistingFileName, string lpNewFileName,
        CopyProgressRoutine lpProgressRoutine, IntPtr lpData, ref int pbCancel, uint dwCopyFlags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool GetDiskFreeSpaceExW(string lpDirectoryName,
        out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);
}

internal static class PathUtil
{
    /// <summary>给本机绝对路径加上 \\?\ 前缀，绕开 260 字符限制。</summary>
    public static string Long(string path)
    {
        if (path.StartsWith(@"\\?\", StringComparison.Ordinal)) return path;
        if (path.Length >= 2 && path[1] == ':') return @"\\?\" + path;
        return path;
    }

    public static string Join(string a, string b)
    {
        if (a.EndsWith('\\')) return a + b;
        return a + "\\" + b;
    }

    public static string TrimLong(string path)
    {
        return path.StartsWith(@"\\?\", StringComparison.Ordinal) ? path.Substring(4) : path;
    }
}
