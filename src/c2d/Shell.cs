using System;
using System.Runtime.InteropServices;

namespace C2D;

/// <summary>
/// 现代文件夹选择器（IFileOpenDialog）。AOT 下不能用 COM 封送，
/// 所以直接按 vtable 调函数指针——没有任何反射/封送依赖。
/// </summary>
internal static unsafe class Shell
{
    public const uint FOS_PICKFOLDERS = 0x20;
    public const uint FOS_FORCEFILESYSTEM = 0x40;
    public const uint FOS_PATHMUSTEXIST = 0x800;
    public const uint FOS_NOCHANGEDIR = 0x8;
    public const uint SIGDN_FILESYSPATH = 0x80058000;
    private const uint CLSCTX_INPROC_SERVER = 1;

    private static readonly Guid ClsidFileOpenDialog = new("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7");
    private static readonly Guid IidFileOpenDialog = new("d57c7288-d4ad-4768-be02-9d969532d960");

    // vtable 槽位（IUnknown 占 0-2）
    private const int SlotRelease = 2;
    private const int SlotShow = 3;          // IModalWindow
    private const int SlotSetOptions = 9;    // IFileDialog
    private const int SlotGetOptions = 10;
    private const int SlotSetTitle = 17;
    private const int SlotGetResult = 20;
    private const int SlotGetDisplayName = 5; // IShellItem

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter, uint dwClsContext,
        ref Guid riid, out IntPtr ppv);

    [DllImport("ole32.dll")]
    public static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [DllImport("ole32.dll")]
    private static extern void CoTaskMemFree(IntPtr pv);

    private static void Release(IntPtr punk)
    {
        IntPtr* vt = *(IntPtr**)punk;
        var release = (delegate* unmanaged<IntPtr, uint>)vt[SlotRelease];
        release(punk);
    }

    public static string? PickFolder(IntPtr owner, string title)
    {
        IntPtr dialog = IntPtr.Zero;
        IntPtr item = IntPtr.Zero;
        IntPtr namePtr = IntPtr.Zero;

        try
        {
            Guid clsid = ClsidFileOpenDialog;
            Guid iid = IidFileOpenDialog;
            if (CoCreateInstance(ref clsid, IntPtr.Zero, CLSCTX_INPROC_SERVER, ref iid, out dialog) != 0
                || dialog == IntPtr.Zero)
            {
                return Native.PickFolderLegacy(owner, title);
            }

            IntPtr* vt = *(IntPtr**)dialog;
            var getOptions = (delegate* unmanaged<IntPtr, uint*, int>)vt[SlotGetOptions];
            var setOptions = (delegate* unmanaged<IntPtr, uint, int>)vt[SlotSetOptions];
            var setTitle = (delegate* unmanaged<IntPtr, char*, int>)vt[SlotSetTitle];
            var show = (delegate* unmanaged<IntPtr, IntPtr, int>)vt[SlotShow];
            var getResult = (delegate* unmanaged<IntPtr, IntPtr*, int>)vt[SlotGetResult];

            uint opts;
            getOptions(dialog, &opts);
            setOptions(dialog, opts | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST | FOS_NOCHANGEDIR);

            fixed (char* t = title)
            {
                setTitle(dialog, t);
            }

            if (show(dialog, owner) != 0) return null; // 取消

            if (getResult(dialog, &item) != 0 || item == IntPtr.Zero) return null;

            IntPtr* ivt = *(IntPtr**)item;
            var getDisplayName = (delegate* unmanaged<IntPtr, uint, IntPtr*, int>)ivt[SlotGetDisplayName];
            if (getDisplayName(item, SIGDN_FILESYSPATH, &namePtr) != 0 || namePtr == IntPtr.Zero)
                return null;

            return Marshal.PtrToStringUni(namePtr);
        }
        catch (Exception)
        {
            return Native.PickFolderLegacy(owner, title);
        }
        finally
        {
            if (namePtr != IntPtr.Zero) CoTaskMemFree(namePtr);
            if (item != IntPtr.Zero) Release(item);
            if (dialog != IntPtr.Zero) Release(dialog);
        }
    }

    /// <summary>不弹 UI，只验证裸 vtable 调用链是否可用。</summary>
    public static string ComTest()
    {
        IntPtr dialog = IntPtr.Zero;
        try
        {
            Guid clsid = ClsidFileOpenDialog;
            Guid iid = IidFileOpenDialog;
            int hr = CoCreateInstance(ref clsid, IntPtr.Zero, CLSCTX_INPROC_SERVER, ref iid, out dialog);
            if (hr != 0) return $"CoCreateInstance hr=0x{hr:X8}";
            if (dialog == IntPtr.Zero) return "无法创建对话框";

            IntPtr* vt = *(IntPtr**)dialog;
            var getOptions = (delegate* unmanaged<IntPtr, uint*, int>)vt[SlotGetOptions];
            var setOptions = (delegate* unmanaged<IntPtr, uint, int>)vt[SlotSetOptions];

            uint before;
            getOptions(dialog, &before);
            setOptions(dialog, before | FOS_PICKFOLDERS);
            uint after;
            getOptions(dialog, &after);

            return $"OK before=0x{before:X} after=0x{after:X}";
        }
        catch (Exception ex)
        {
            return "EX: " + ex;
        }
        finally
        {
            if (dialog != IntPtr.Zero) Release(dialog);
        }
    }

    /// <summary>解析 IFileOpenDialog 用的 COM 初始化。</summary>
    public static void EnsureCom()
    {
        CoInitializeEx(IntPtr.Zero, 2 /* COINIT_APARTMENTTHREADED */);
    }
}
