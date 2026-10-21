using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace C2D;

internal static class Native
{
    public const int WS_OVERLAPPEDWINDOW = 0x00CF0000;
    public const int WS_VISIBLE = 0x10000000;
    public const int WS_CHILD = 0x40000000;
    public const int WS_TABSTOP = 0x00010000;
    public const int WS_EX_CLIENTEDGE = 0x00000200;
    public const int CS_HREDRAW = 0x0002;
    public const int CS_VREDRAW = 0x0001;
    public const int COLOR_WINDOW = 5;
    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;
    public const int BS_PUSHBUTTON = 0x00000000;
    public const int BS_DEFPUSHBUTTON = 0x00000001;
    public const int ES_AUTOHSCROLL = 0x0080;
    public const int PBS_SMOOTH = 0x01;
    public const int DEFAULT_GUI_FONT = 17;
    public const int IDC_ARROW = 32512;
    public const int ICC_LISTVIEW_CLASSES = 0x01;
    public const int ICC_PROGRESS_CLASS = 0x20;

    public const uint WM_CREATE = 0x0001;
    public const uint WM_DESTROY = 0x0002;
    public const uint WM_SIZE = 0x0005;
    public const uint WM_TIMER = 0x0113;
    public const uint WM_COMMAND = 0x0111;
    public const uint WM_SETICON = 0x0080;
    public const uint WM_PAINT = 0x000F;
    public const uint WM_ERASEBKGND = 0x0014;
    public const uint WM_CTLCOLORSTATIC = 0x0138;
    public const uint WM_SETFONT = 0x0030;
    public const uint WM_APP_DONE = 0x8001;

    public const int LVS_REPORT = 0x0001;
    public const int LVS_SINGLESEL = 0x0004;
    public const int LVS_SHOWSELALWAYS = 0x0008;
    public const int LVS_EX_FULLROWSELECT = 0x00000020;
    public const int LVS_EX_DOUBLEBUFFER = 0x00010000;
    public const uint LVM_SETEXTENDEDLISTVIEWSTYLE = 0x1036;
    public const uint LVM_INSERTCOLUMNW = 0x1061;
    public const uint LVM_INSERTITEMW = 0x104D; // LVM_FIRST + 77（A 版是 0x1007，别用错）
    public const uint LVM_SETITEMW = 0x104C;    // LVM_FIRST + 76

    public const uint PBM_SETPOS = 0x0402;
    public const uint PBM_SETRANGE32 = 0x0406;

    public const uint LVCF_FMT = 0x0001, LVCF_WIDTH = 0x0002, LVCF_TEXT = 0x0004, LVCF_SUBITEM = 0x0008;
    public const uint LVIF_TEXT = 0x0001;
    public const int LVCFMT_LEFT = 0x0000;

    public const uint BIF_RETURNONLYFSDIRS = 0x0001;
    public const uint BIF_NEWDIALOGSTYLE = 0x0040;
    public const uint BIF_EDITBOX = 0x0010;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct LVCOLUMN
    {
        public uint mask;
        public int fmt;
        public int cx;
        [MarshalAs(UnmanagedType.LPWStr)] public string pszText;
        public int cchTextMax;
        public int iSubItem;
        public int iImage;
        public int iOrder;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct LVITEM
    {
        public uint mask;
        public int iItem;
        public int iSubItem;
        public uint state;
        public uint stateMask;
        [MarshalAs(UnmanagedType.LPWStr)] public string pszText;
        public int cchTextMax;
        public int iImage;
        public IntPtr lParam;
        public int iIndent;
        public int iGroupId;
        public uint cColumns;
        public IntPtr puColumns;
        public IntPtr piColFmt;
        public int iGroup;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct BROWSEINFO
    {
        public IntPtr hwndOwner;
        public IntPtr pidlRoot;
        public IntPtr pszDisplayName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszTitle;
        public uint ulFlags;
        public IntPtr lpfn;
        public IntPtr lParam;
        public int iImage;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INITCOMMONCONTROLSEX
    {
        public int dwSize;
        public int dwICC;
    }

    public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern ushort RegisterClassExW(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateWindowExW(
        int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    public static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetMessageW(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    public static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr DispatchMessageW(ref MSG lpMsg);

    [DllImport("user32.dll")]
    public static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    public static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessageW(IntPtr hWnd, uint msg, IntPtr wParam, ref LVCOLUMN lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessageW(IntPtr hWnd, uint msg, IntPtr wParam, ref LVITEM lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadCursorW(IntPtr hInstance, IntPtr lpCursorName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool SetWindowTextW(IntPtr hWnd, string lpString);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    // ---------- 绘制 ----------

    [StructLayout(LayoutKind.Sequential)]
    public struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public int fErase;
        public RECT rcPaint;
        public int fRestore;
        public int fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public int bmiColors;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [DllImport("user32.dll")]
    public static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT ps);

    [DllImport("user32.dll")]
    public static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT ps);

    [DllImport("user32.dll")]
    public static extern int FillRect(IntPtr hdc, ref RECT rc, IntPtr brush);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateSolidBrush(int color);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr obj);

    [DllImport("gdi32.dll")]
    public static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [DllImport("gdi32.dll")]
    public static extern int SetTextColor(IntPtr hdc, int color);

    [DllImport("gdi32.dll")]
    public static extern int SetBkMode(IntPtr hdc, int mode);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateFontW(int cHeight, int cWidth, int cEscapement, int cOrientation,
        int cWeight, uint bItalic, uint bUnderline, uint cStrikeOut, uint iCharSet,
        uint iOutPrecision, uint iClipPrecision, uint iQuality, uint iPitchAndFamily, string pszFaceName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int DrawTextW(IntPtr hdc, string lpchText, int cchText, ref RECT lprc, uint dwDTFormat);

    [DllImport("msimg32.dll")]
    public static extern bool AlphaBlend(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
        IntPtr hdcSrc, int xSrc, int ySrc, int wSrc, int hSrc, BLENDFUNCTION blend);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint usage,
        out IntPtr bits, IntPtr hSection, uint offset);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll")]
    public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool PeekMessageW(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [DllImport("user32.dll")]
    public static extern bool UpdateWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);

    [DllImport("gdi32.dll")]
    public static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint lines,
        IntPtr bits, ref BITMAPINFO bi, uint usage);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint flags);

    [DllImport("user32.dll")]
    public static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    [DllImport("user32.dll")]
    public static extern IntPtr SetTimer(IntPtr hWnd, IntPtr nIDEvent, uint uElapse, IntPtr lpTimerFunc);

    [DllImport("user32.dll")]
    public static extern bool KillTimer(IntPtr hWnd, IntPtr uIDEvent);

    [DllImport("comctl32.dll")]
    public static extern bool InitCommonControlsEx(ref INITCOMMONCONTROLSEX icc);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SHBrowseForFolderW(ref BROWSEINFO lpbi);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern bool SHGetPathFromIDListW(IntPtr pidl, StringBuilder pszPath);

    [DllImport("ole32.dll")]
    public static extern void CoTaskMemFree(IntPtr pv);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadIconW(IntPtr hInstance, IntPtr lpIconName);

    /// <summary>加载 exe 内嵌的图标（.NET 把 ApplicationIcon 放在资源 ID 32512）。</summary>
    public static IntPtr LoadAppIcon(IntPtr hInstance)
    {
        IntPtr h = LoadIconW(hInstance, (IntPtr)32512);
        if (h == IntPtr.Zero) h = LoadIconW(hInstance, (IntPtr)1);
        return h;
    }

    /// <summary>旧式选择器，仅作为 COM 失败时的兜底。</summary>
    public static string? PickFolderLegacy(IntPtr owner, string title)
    {
        IntPtr display = Marshal.AllocHGlobal(520);
        IntPtr pidl = IntPtr.Zero;
        try
        {
            var bi = new BROWSEINFO
            {
                hwndOwner = owner,
                pszDisplayName = display,
                lpszTitle = title,
                ulFlags = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE,
            };

            pidl = SHBrowseForFolderW(ref bi);
            if (pidl == IntPtr.Zero) return null;

            var sb = new StringBuilder(1024);
            if (!SHGetPathFromIDListW(pidl, sb)) return null;
            return sb.ToString();
        }
        finally
        {
            if (pidl != IntPtr.Zero) CoTaskMemFree(pidl);
            Marshal.FreeHGlobal(display);
        }
    }

    [DllImport("gdi32.dll")]
    public static extern IntPtr GetStockObject(int i);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandleW(string? lpModuleName);
}

internal sealed class MainWindow
{
    private const int IdSrcBrowse = 1001;
    private const int IdDstBrowse = 1002;
    private const int IdStart = 1003;
    private const int IdCancel = 1004;
    private const int IdRollbackSel = 1005;
    private const int IdReleaseSel = 1006;
    private const int IdAuto = 1007;
    private const int TimerId = 1;
    private const string ClassName = "C2D.MainWindow";

    private readonly Native.WndProcDelegate _wndProc;
    private IntPtr _hwnd;

    // 首页
    private IntPtr _title, _summary, _list;
    private IntPtr _srcLabel, _srcEdit, _btnSrc, _arrow, _dstLabel, _dstEdit, _btnDst, _btnStart;

    // 任务页
    private IntPtr _taskTitle, _stageText, _bar, _pctText, _speedText, _fileText, _countText, _cancel;

    private ProgressState? _p;
    private Thread? _worker;
    private bool _busy;

    private long _lastBytes;
    private long _lastTicks;
    private double _speed;
    private string _src = "";
    private string _dst = "";
    private string _lastSrc = "";
    private string _lastDst = "";
    private string _lastBak = "";
    private string _lastId = "";
    private long _recBytes;
    private int _recFiles;

    private IntPtr _btnRollback, _btnRelease;
    private IntPtr _chkAuto, _preview;
    private readonly List<MigrationRecord> _rows = new();

    // 顶部横幅
    private const int HeaderH = 150;
    private IntPtr _bgDc, _fontTitle, _fontSub, _whiteBrush, _lineBrush;
    private int _bgW, _bgH;

    public MainWindow()
    {
        _wndProc = WndProc;
    }

    /// <summary>调试用：把窗口渲染成 BMP，方便在没有桌面的环境里检查界面。</summary>
    public string? ShotPath;

    /// <summary>调试用：截图前塞几行假数据，方便检查列表渲染。</summary>
    public bool ShotSeed;

    /// <summary>调试用：截图前切到任务页并塞入假进度。</summary>
    public bool ShotTask;

    public void Run()
    {
        IntPtr hInstance = Native.GetModuleHandleW(null);

        var icc = new Native.INITCOMMONCONTROLSEX
        {
            dwSize = Marshal.SizeOf<Native.INITCOMMONCONTROLSEX>(),
            dwICC = Native.ICC_LISTVIEW_CLASSES | Native.ICC_PROGRESS_CLASS,
        };
        Native.InitCommonControlsEx(ref icc);

        var wc = new Native.WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<Native.WNDCLASSEX>(),
            style = Native.CS_HREDRAW | Native.CS_VREDRAW,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = hInstance,
            hIcon = Native.LoadAppIcon(hInstance),
            hIconSm = Native.LoadAppIcon(hInstance),
            hCursor = Native.LoadCursorW(IntPtr.Zero, (IntPtr)Native.IDC_ARROW),
            hbrBackground = (IntPtr)(Native.COLOR_WINDOW + 1),
            lpszClassName = ClassName,
        };

        if (Native.RegisterClassExW(ref wc) == 0)
        {
            Native.MessageBoxW(IntPtr.Zero, "窗口类注册失败。", "伪迁移助手", 0x10);
            return;
        }

        string title = "伪迁移助手 — C 盘目录迁移";
        string il = Engine.IntegrityLevel();
        if (ShotPath is null && (il.StartsWith("低") || il.StartsWith("不受信任")))
            title += "　[受限模式]";

        _hwnd = Native.CreateWindowExW(0, ClassName, title,
            Native.WS_OVERLAPPEDWINDOW, 120, 80, 1000, 760,
            IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
        {
            Native.MessageBoxW(IntPtr.Zero, "窗口创建失败。", "伪迁移助手", 0x10);
            return;
        }

        Native.ShowWindow(_hwnd, Native.SW_SHOW);

        LoadBanner();
        _fontTitle = Native.CreateFontW(-34, 0, 0, 0, 600, 0, 0, 0, 1, 0, 0, 5, 0, "Microsoft YaHei UI");
        _fontSub = Native.CreateFontW(-17, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "Microsoft YaHei UI");
        _whiteBrush = Native.CreateSolidBrush(0x00FFFFFF);
        _lineBrush = Native.CreateSolidBrush(0x00E4E4E4);
        Native.InvalidateRect(_hwnd, IntPtr.Zero, false);

        // 任务栏/Alt-Tab 用窗口图标，类图标只覆盖标题栏
        IntPtr appIcon = Native.LoadAppIcon(hInstance);
        if (appIcon != IntPtr.Zero)
        {
            Native.SendMessageW(_hwnd, Native.WM_SETICON, IntPtr.Zero, appIcon);
            Native.SendMessageW(_hwnd, Native.WM_SETICON, (IntPtr)1, appIcon);
        }

        if (ShotPath is not null)
        {
            if (ShotSeed) SeedShot();
            if (ShotTask) SeedTask();
            PumpFor(600);
            Capture(ShotPath);
            return;
        }

        while (Native.GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            Native.TranslateMessage(ref msg);
            Native.DispatchMessageW(ref msg);
        }
    }

    private void PumpFor(int ms)
    {
        long until = Environment.TickCount64 + ms;
        while (Environment.TickCount64 < until)
        {
            while (Native.PeekMessageW(out var m, IntPtr.Zero, 0, 0, 1))
            {
                Native.TranslateMessage(ref m);
                Native.DispatchMessageW(ref m);
            }
            Thread.Sleep(15);
        }
        Native.UpdateWindow(_hwnd);
    }

    private void Capture(string path)
    {
        Native.GetWindowRect(_hwnd, out var wr);
        int w = wr.Right - wr.Left;
        int h = wr.Bottom - wr.Top;
        if (w <= 0 || h <= 0) return;

        IntPtr screen = Native.GetDC(_hwnd);
        IntPtr mem = Native.CreateCompatibleDC(screen);
        IntPtr bmp = Native.CreateCompatibleBitmap(screen, w, h);
        IntPtr old = Native.SelectObject(mem, bmp);

        Native.PrintWindow(_hwnd, mem, 2);

        var bi = new Native.BITMAPINFO
        {
            bmiHeader = new Native.BITMAPINFOHEADER
            {
                biSize = 40,
                biWidth = w,
                biHeight = h,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0,
                biSizeImage = w * h * 4,
            },
        };

        var bits = new byte[w * h * 4];
        IntPtr buf = Marshal.AllocHGlobal(bits.Length);
        try
        {
            Native.GetDIBits(mem, bmp, 0, (uint)h, buf, ref bi, 0);
            Marshal.Copy(buf, bits, 0, bits.Length);
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }

        Native.SelectObject(mem, old);
        Native.DeleteObject(bmp);
        Native.DeleteDC(mem);
        Native.ReleaseDC(_hwnd, screen);

        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);
        const int offset = 54;
        bw.Write((byte)'B');
        bw.Write((byte)'M');
        bw.Write(offset + bits.Length);
        bw.Write(0);
        bw.Write(offset);
        bw.Write(40);
        bw.Write(w);
        bw.Write(h);
        bw.Write((short)1);
        bw.Write((short)32);
        bw.Write(0);
        bw.Write(bits.Length);
        bw.Write(0);
        bw.Write(0);
        bw.Write(0);
        bw.Write(0);
        bw.Write(bits);
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case Native.WM_CREATE:
                CreateChildren(hWnd);
                ShowPage(hWnd, task: false);
                RefreshPending();
                return IntPtr.Zero;

            case Native.WM_SIZE:
                Layout(hWnd);
                return IntPtr.Zero;

            case Native.WM_PAINT:
                PaintWindow(hWnd);
                return IntPtr.Zero;

            case Native.WM_ERASEBKGND:
                return (IntPtr)1;

            case Native.WM_CTLCOLORSTATIC:
                // 让静态文字的背景和窗口一致，否则会出现灰底方块
                Native.SetBkMode(wParam, 1);
                Native.SetTextColor(wParam, 0x00303030);
                return _whiteBrush;

            case Native.WM_COMMAND:
                if ((((int)wParam >> 16) & 0xFFFF) == 0x0300) // EN_CHANGE
                {
                    UpdatePreview();
                    return IntPtr.Zero;
                }

                switch ((int)wParam & 0xFFFF)
                {
                    case IdSrcBrowse: OnBrowseSource(hWnd); break;
                    case IdDstBrowse: OnBrowseDest(hWnd); break;
                    case IdStart: OnStartClicked(hWnd); break;
                    case IdCancel: OnCancelClicked(); break;
                    case IdRollbackSel: OnRollbackSelected(hWnd); break;
                    case IdReleaseSel: OnReleaseSelected(hWnd); break;
                    case IdAuto: UpdatePreview(); break;
                }
                return IntPtr.Zero;

            case Native.WM_TIMER:
                OnTick();
                return IntPtr.Zero;

            case Native.WM_APP_DONE:
                OnWorkerDone(hWnd);
                return IntPtr.Zero;

            case Native.WM_DESTROY:
                if (_p is not null) _p.CancelRequested = true;
                Native.PostQuitMessage(0);
                return IntPtr.Zero;

            default:
                return Native.DefWindowProcW(hWnd, msg, wParam, lParam);
        }
    }

    // ---------------- 控件 ----------------

    private void LoadBanner()
    {
        try
        {
            using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("C2D.bg.bin");
            if (stream is null) return;

            var data = new byte[stream.Length];
            int read = 0;
            while (read < data.Length)
            {
                int n = stream.Read(data, read, data.Length - read);
                if (n <= 0) break;
                read += n;
            }
            if (read < data.Length) return;

            _bgW = 320;
            _bgH = 320;

            var bi = new Native.BITMAPINFO
            {
                bmiHeader = new Native.BITMAPINFOHEADER
                {
                    biSize = 40,
                    biWidth = _bgW,
                    biHeight = -_bgH, // 负数 = 自上而下
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0,
                },
            };

            IntPtr hbm = Native.CreateDIBSection(IntPtr.Zero, ref bi, 0, out IntPtr bits, IntPtr.Zero, 0);
            if (hbm == IntPtr.Zero) return;

            Marshal.Copy(data, 0, bits, data.Length);

            _bgDc = Native.CreateCompatibleDC(IntPtr.Zero);
            Native.SelectObject(_bgDc, hbm);
        }
        catch
        {
            // 没有背景图也照常能用
        }
    }

    private void PaintWindow(IntPtr hWnd)
    {
        Native.BeginPaint(hWnd, out var ps);
        try
        {
            Native.GetClientRect(hWnd, out var rc);
            Native.FillRect(ps.hdc, ref rc, _whiteBrush);

            var line = new Native.RECT { Left = 0, Top = HeaderH - 1, Right = rc.Right, Bottom = HeaderH };
            Native.FillRect(ps.hdc, ref line, _lineBrush);

            // 右上角插画
            if (_bgDc != IntPtr.Zero && _bgW > 0)
            {
                int size = 140;
                int x = rc.Right - 24 - size;
                int y = 5;

                var blend = new Native.BLENDFUNCTION
                {
                    BlendOp = 0,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = 1,
                };
                Native.AlphaBlend(ps.hdc, x, y, size, size, _bgDc, 0, 0, _bgW, _bgH, blend);
            }

            Native.SetBkMode(ps.hdc, 1); // TRANSPARENT

            if (_fontTitle != IntPtr.Zero)
            {
                Native.SelectObject(ps.hdc, _fontTitle);
                Native.SetTextColor(ps.hdc, 0x00442A1F); // #1F2A44
                var tr = new Native.RECT { Left = 24, Top = 36, Right = rc.Right - 180, Bottom = 84 };
                Native.DrawTextW(ps.hdc, "伪迁移助手", -1, ref tr, 0x00000020);
            }

            if (_fontSub != IntPtr.Zero)
            {
                Native.SelectObject(ps.hdc, _fontSub);
                Native.SetTextColor(ps.hdc, 0x00807A70); // #707A80
                var sr = new Native.RECT { Left = 26, Top = 88, Right = rc.Right - 180, Bottom = 116 };
                Native.DrawTextW(ps.hdc, "把占 C 盘的大目录搬到别的盘，原路径留一个链接", -1, ref sr, 0x00000020);
            }
        }
        finally
        {
            Native.EndPaint(hWnd, ref ps);
        }
    }

    private void CreateChildren(IntPtr hWnd)
    {
        IntPtr hInstance = Native.GetModuleHandleW(null);

        const int pad = 16;
        const int labelW = 118;
        const int btnW = 88;

        _srcLabel = MkStatic(hWnd, hInstance, "源文件夹", pad, 22, labelW, 20);
        _srcEdit = MkEdit(hWnd, hInstance, pad + labelW, 16, 600, 30);
        _btnSrc = MkButton(hWnd, hInstance, "浏览…", 800, 16, btnW, 30, IdSrcBrowse);

        _arrow = MkStatic(hWnd, hInstance, "↓", pad + 8, HeaderH + 50, 40, 24);

        _dstLabel = MkStatic(hWnd, hInstance, "目标文件夹", pad, 84, labelW, 20);
        _dstEdit = MkEdit(hWnd, hInstance, pad + labelW, 78, 600, 30);
        _btnDst = MkButton(hWnd, hInstance, "浏览…", 800, 78, btnW, 30, IdDstBrowse);

        _btnStart = MkButton(hWnd, hInstance, "开始迁移", pad, 122, 140, 34, IdStart);

        _chkAuto = Native.CreateWindowExW(0, "BUTTON", "自动在目标位置下创建同名文件夹",
            Native.WS_CHILD | Native.WS_VISIBLE | 0x0003 /* BS_AUTOCHECKBOX */,
            pad, 112, 300, 22, hWnd, (IntPtr)IdAuto, hInstance, IntPtr.Zero);
        Native.SendMessageW(_chkAuto, 0x00F1 /* BM_SETCHECK */, (IntPtr)1, IntPtr.Zero);

        _preview = MkStatic(hWnd, hInstance, "", pad + 310, 112, 600, 22);

        _btnRollback = MkButton(hWnd, hInstance, "回滚所选", 0, 172, 88, 26, IdRollbackSel);
        _btnRelease = MkButton(hWnd, hInstance, "释放所选", 0, 172, 88, 26, IdReleaseSel);

        _title = MkStatic(hWnd, hInstance, "待释放 0 项 · 共 0 B", pad, 176, 900, 24);
        _summary = MkStatic(hWnd, hInstance,
            "选好源文件夹会自动建议目标位置；两个地址都可以直接手填。", pad, 202, 900, 20);

        _list = Native.CreateWindowExW(
            Native.WS_EX_CLIENTEDGE, "SysListView32", "",
            Native.WS_CHILD | Native.WS_VISIBLE | Native.WS_TABSTOP | Native.LVS_REPORT |
            Native.LVS_SINGLESEL | Native.LVS_SHOWSELALWAYS,
            pad, 230, 900, 380, hWnd, IntPtr.Zero, hInstance, IntPtr.Zero);

        Native.SendMessageW(_list, Native.LVM_SETEXTENDEDLISTVIEWSTYLE,
            (IntPtr)(Native.LVS_EX_FULLROWSELECT | Native.LVS_EX_DOUBLEBUFFER),
            (IntPtr)(Native.LVS_EX_FULLROWSELECT | Native.LVS_EX_DOUBLEBUFFER));

        AddColumn(0, 462, "原路径（C 盘）");
        AddColumn(1, 86, "大小");
        AddColumn(2, 86, "已保留");
        AddColumn(3, 298, "实际位置（目标盘）");

        // --- 任务页 ---
        _taskTitle = MkStatic(hWnd, hInstance, "", 16, 14, 900, 26);
        _stageText = MkStatic(hWnd, hInstance, "", 16, 48, 900, 22);

        _bar = Native.CreateWindowExW(0, "msctls_progress32", "",
            Native.WS_CHILD | Native.PBS_SMOOTH,
            16, 86, 900, 18, hWnd, IntPtr.Zero, hInstance, IntPtr.Zero);
        Native.SendMessageW(_bar, Native.PBM_SETRANGE32, IntPtr.Zero, (IntPtr)1000);

        _pctText = MkStatic(hWnd, hInstance, "", 16, 112, 900, 22);
        _speedText = MkStatic(hWnd, hInstance, "", 16, 136, 900, 22);
        _fileText = MkStatic(hWnd, hInstance, "", 16, 168, 940, 22);
        _countText = MkStatic(hWnd, hInstance, "", 16, 192, 900, 22);

        _cancel = Native.CreateWindowExW(0, "BUTTON", "取消",
            Native.WS_CHILD | Native.BS_PUSHBUTTON,
            16, 560, 120, 34, hWnd, (IntPtr)IdCancel, hInstance, IntPtr.Zero);

        IntPtr font = Native.CreateFontW(-15, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "Microsoft YaHei UI");
        foreach (var h in new[] { _list, _title, _summary, _taskTitle, _stageText,
                                  _pctText, _speedText, _fileText, _countText, _cancel,
                                  _srcLabel, _srcEdit, _btnSrc, _arrow,
                                  _dstLabel, _dstEdit, _btnDst, _btnStart,
                                  _btnRollback, _btnRelease, _chkAuto, _preview })
        {
            Native.SendMessageW(h, Native.WM_SETFONT, font, (IntPtr)1);
        }

        UpdatePreview();
    }

    private static IntPtr MkEdit(IntPtr parent, IntPtr hInstance, int x, int y, int w, int h)
    {
        return Native.CreateWindowExW(Native.WS_EX_CLIENTEDGE, "EDIT", "",
            Native.WS_CHILD | Native.WS_VISIBLE | Native.ES_AUTOHSCROLL,
            x, y, w, h, parent, IntPtr.Zero, hInstance, IntPtr.Zero);
    }

    private static IntPtr MkButton(IntPtr parent, IntPtr hInstance, string text,
        int x, int y, int w, int h, int id)
    {
        return Native.CreateWindowExW(0, "BUTTON", text,
            Native.WS_CHILD | Native.WS_VISIBLE | Native.BS_PUSHBUTTON,
            x, y, w, h, parent, (IntPtr)id, hInstance, IntPtr.Zero);
    }

    private static IntPtr MkStatic(IntPtr parent, IntPtr hInstance, string text,
        int x, int y, int w, int h)
    {
        return Native.CreateWindowExW(0, "STATIC", text, Native.WS_CHILD | Native.WS_VISIBLE,
            x, y, w, h, parent, IntPtr.Zero, hInstance, IntPtr.Zero);
    }

    private void ShowPage(IntPtr hWnd, bool task)
    {
        Native.ShowWindow(_title, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_summary, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_list, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_btnStart, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_btnRollback, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_btnRelease, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_srcLabel, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_srcEdit, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_btnSrc, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_arrow, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_dstLabel, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_dstEdit, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_btnDst, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_chkAuto, task ? Native.SW_HIDE : Native.SW_SHOW);
        Native.ShowWindow(_preview, task ? Native.SW_HIDE : Native.SW_SHOW);

        Native.ShowWindow(_taskTitle, task ? Native.SW_SHOW : Native.SW_HIDE);
        Native.ShowWindow(_stageText, task ? Native.SW_SHOW : Native.SW_HIDE);
        Native.ShowWindow(_bar, task ? Native.SW_SHOW : Native.SW_HIDE);
        Native.ShowWindow(_pctText, task ? Native.SW_SHOW : Native.SW_HIDE);
        Native.ShowWindow(_speedText, task ? Native.SW_SHOW : Native.SW_HIDE);
        Native.ShowWindow(_fileText, task ? Native.SW_SHOW : Native.SW_HIDE);
        Native.ShowWindow(_countText, task ? Native.SW_SHOW : Native.SW_HIDE);
        Native.ShowWindow(_cancel, task ? Native.SW_SHOW : Native.SW_HIDE);
    }

    private void Layout(IntPtr hWnd)
    {
        if (_list == IntPtr.Zero) return;

        Native.GetClientRect(hWnd, out var rc);
        int w = rc.Right - rc.Left;
        int h = rc.Bottom - rc.Top;
        const int pad = 16;

        const int labelW = 84, btnW = 88, editH = 30;
        int right = w - pad;
        int editW = right - (pad + labelW) - btnW - 8;

        Native.MoveWindow(_srcLabel, pad, HeaderH + 24, labelW, 20, true);
        Native.MoveWindow(_srcEdit, pad + labelW, HeaderH + 16, editW, editH, true);
        Native.MoveWindow(_btnSrc, right - btnW, HeaderH + 16, btnW, editH, true);

        Native.MoveWindow(_arrow, pad + 8, HeaderH + 50, 40, 24, true);

        Native.MoveWindow(_dstLabel, pad, HeaderH + 84, labelW, 20, true);
        Native.MoveWindow(_dstEdit, pad + labelW, HeaderH + 78, editW, editH, true);
        Native.MoveWindow(_btnDst, right - btnW, HeaderH + 78, btnW, editH, true);

        Native.MoveWindow(_chkAuto, pad, HeaderH + 114, 300, 22, true);
        Native.MoveWindow(_preview, pad + 306, HeaderH + 114, w - pad * 2 - 306, 22, true);

        Native.MoveWindow(_btnStart, pad, HeaderH + 146, 140, 34, true);

        Native.MoveWindow(_title, pad, HeaderH + 196, w - pad * 2 - 200, 24, true);
        Native.MoveWindow(_btnRelease, w - pad - 88, HeaderH + 194, 88, 26, true);
        Native.MoveWindow(_btnRollback, w - pad - 88 - 92, HeaderH + 194, 88, 26, true);
        Native.MoveWindow(_summary, pad, HeaderH + 222, w - pad * 2, 20, true);
        Native.MoveWindow(_list, pad, HeaderH + 250, w - pad * 2, h - (HeaderH + 250) - pad, true);

        Native.MoveWindow(_taskTitle, pad, HeaderH + 14, w - pad * 2, 26, true);
        Native.MoveWindow(_stageText, pad, HeaderH + 48, w - pad * 2, 22, true);
        Native.MoveWindow(_bar, pad, HeaderH + 86, w - pad * 2, 18, true);
        Native.MoveWindow(_pctText, pad, HeaderH + 112, w - pad * 2, 22, true);
        Native.MoveWindow(_speedText, pad, HeaderH + 136, w - pad * 2, 22, true);
        Native.MoveWindow(_fileText, pad, HeaderH + 168, w - pad * 2, 22, true);
        Native.MoveWindow(_countText, pad, HeaderH + 192, w - pad * 2, 22, true);
        Native.MoveWindow(_cancel, pad, h - 42, 120, 34, true);
    }

    private void AddColumn(int index, int width, string text)
    {
        var col = new Native.LVCOLUMN
        {
            mask = Native.LVCF_FMT | Native.LVCF_WIDTH | Native.LVCF_TEXT | Native.LVCF_SUBITEM,
            fmt = Native.LVCFMT_LEFT,
            cx = width,
            pszText = text,
            iSubItem = index,
        };
        Native.SendMessageW(_list, Native.LVM_INSERTCOLUMNW, (IntPtr)index, ref col);
    }

    private void AddRow(int index, string name, string size, string age, string path)
    {
        var item = new Native.LVITEM
        {
            mask = Native.LVIF_TEXT,
            iItem = index,
            iSubItem = 0,
            pszText = name,
        };
        Native.SendMessageW(_list, Native.LVM_INSERTITEMW, IntPtr.Zero, ref item);

        SetSubItem(index, 1, size);
        SetSubItem(index, 2, age);
        SetSubItem(index, 3, path);
    }

    private void SetSubItem(int row, int col, string text)
    {
        var item = new Native.LVITEM
        {
            mask = Native.LVIF_TEXT,
            iItem = row,
            iSubItem = col,
            pszText = text,
        };
        Native.SendMessageW(_list, Native.LVM_SETITEMW, IntPtr.Zero, ref item);
    }

    // ---------------- 交互 ----------------

    private void OnBrowseSource(IntPtr hWnd)
    {
        if (_busy) return;
        string? p = PickFolder(hWnd, "选择要迁移的文件夹");
        if (p is null) return;

        Native.SetWindowTextW(_srcEdit, p);
        string suggest = SuggestDest(p);
        if (suggest.Length > 0) Native.SetWindowTextW(_dstEdit, suggest);
    }

    private void OnBrowseDest(IntPtr hWnd)
    {
        if (_busy) return;
        string? p = PickFolder(hWnd, "选择目标文件夹");
        if (p is null) return;
        Native.SetWindowTextW(_dstEdit, p);
    }

    private void OnStartClicked(IntPtr hWnd)
    {
        if (_busy) return;

        string src = GetText(_srcEdit).Trim().TrimEnd('\\');
        string dst = GetText(_dstEdit).Trim().TrimEnd('\\');

        if (src.Length == 0 || dst.Length == 0)
        {
            Native.MessageBoxW(hWnd, "源文件夹和目标文件夹都要填。", "伪迁移助手", 0x30);
            return;
        }

        // 目标框可能少写了最后一层，这里统一算成"实际落点"
        string dstFinal = EffectiveDest(src, dst, out _);
        if (!Directory.Exists(src))
        {
            Native.MessageBoxW(hWnd, "源文件夹不存在：\n\n" + src, "伪迁移助手", 0x30);
            return;
        }

        var info = new DirectoryInfo(src);
        if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            Native.MessageBoxW(hWnd, "这个文件夹本身已经是一个链接，不能重复迁移。", "伪迁移助手", 0x30);
            return;
        }
        if (string.Equals(src, dstFinal, StringComparison.OrdinalIgnoreCase))
        {
            Native.MessageBoxW(hWnd, "源和目标不能是同一个文件夹。", "伪迁移助手", 0x30);
            return;
        }
        if (dstFinal.StartsWith(src + "\\", StringComparison.OrdinalIgnoreCase))
        {
            Native.MessageBoxW(hWnd, "目标文件夹不能放在源文件夹里面。", "伪迁移助手", 0x30);
            return;
        }

        StartMigration(hWnd, src, dstFinal);
    }

    private static void QueueCleanup(string path)
    {
        // 后台删，别让"取消"或"失败"卡住界面
        var t = new Thread(() => Engine.DeleteTree(path, out _)) { IsBackground = true };
        t.Start();
    }

    private static void TryDeleteDir(string path)
    {
        Engine.DeleteTree(path, out _);
    }

    private void RefreshPending()
    {
        _rows.Clear();

        foreach (var r in State.Load())
        {
            if (r.Status != "linked") continue;

            if (r.Bak.Length == 0 || !Directory.Exists(PathUtil.Long(r.Bak)))
            {
                r.Status = "released";
                State.Append(r);
                continue;
            }

            _rows.Add(r);
        }

        RenderRows();
    }

    private void RenderRows()
    {
        const uint LVM_DELETEALLITEMS = 0x1009;
        Native.SendMessageW(_list, LVM_DELETEALLITEMS, IntPtr.Zero, IntPtr.Zero);

        long total = 0;
        for (int i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];
            total += r.Bytes;
            AddRow(i, r.Src, Engine.Human(r.Bytes), AgeText(r.At), r.Dst);
        }

        Native.SetWindowTextW(_title, _rows.Count == 0
            ? "待释放 0 项 · 共 0 B"
            : $"待释放 {_rows.Count} 项 · 共 {Engine.Human(total)}");

        Native.SetWindowTextW(_summary, _rows.Count == 0
            ? "暂无待释放项。迁移完成后，原目录的副本会出现在这里。"
            : "选中一行后，可以回滚到迁移前，或者只删副本释放空间。");
    }

    private void SeedTask()
    {
        ShowPage(_hwnd, task: true);
        Native.SetWindowTextW(_taskTitle, "正在迁移  C:\\Users\\me\\AppData\\Local\\SomeApp");
        Native.SetWindowTextW(_stageText, "② 复制中");
        Native.SetWindowTextW(_pctText, "68.4%   129.9 MB / 185.7 MB");
        Native.SetWindowTextW(_speedText, "42.3 MB/s   剩余约 1 秒");
        Native.SetWindowTextW(_fileText, "正在复制  D:\\Cache\\data_00042.bin");
        Native.SetWindowTextW(_countText, "116 / 204 个文件");
        Native.SendMessageW(_bar, Native.PBM_SETPOS, (IntPtr)684, IntPtr.Zero);
    }

    private void SeedShot()
    {
        Native.SetWindowTextW(_srcEdit, @"C:\Users\me\AppData\Roaming\Tencent");
        Native.SetWindowTextW(_dstEdit, @"D:\Apps");
        UpdatePreview();

        _rows.Add(new MigrationRecord
        {
            Src = @"C:\Users\me\AppData\Local\Google\Chrome\User Data",
            Dst = @"D:\Migrated\C\Users\me\AppData\Local\Google\Chrome\User Data",
            Bytes = 13314398617,
            At = DateTime.Now.AddDays(-3).ToString("O"),
        });
        _rows.Add(new MigrationRecord
        {
            Src = @"C:\Users\me\.gradle\caches",
            Dst = @"D:\Migrated\C\Users\me\.gradle\caches",
            Bytes = 8234567891,
            At = DateTime.Now.AddHours(-5).ToString("O"),
        });
        RenderRows();
    }

    private static string AgeText(string iso)
    {
        if (DateTime.TryParse(iso, out var t))
        {
            var d = DateTime.Now - t;
            if (d.TotalDays >= 1) return (int)d.TotalDays + " 天";
            if (d.TotalHours >= 1) return (int)d.TotalHours + " 小时";
            return Math.Max(1, (int)d.TotalMinutes) + " 分钟";
        }
        return "";
    }

    private int SelectedIndex()
    {
        const uint LVM_GETNEXTITEM = 0x100A;
        const int LVNI_SELECTED = 0x0002;
        int i = (int)Native.SendMessageW(_list, LVM_GETNEXTITEM, (IntPtr)(-1), (IntPtr)LVNI_SELECTED);
        return i >= 0 && i < _rows.Count ? i : -1;
    }

    private void OnRollbackSelected(IntPtr hWnd)
    {
        int i = SelectedIndex();
        if (i < 0)
        {
            Native.MessageBoxW(hWnd, "请先在列表里选中一项。", "伪迁移助手", 0x40);
            return;
        }

        var r = _rows[i];
        string ask = "回滚到迁移前的状态？\n\n" +
                     "会删除链接、把备份改回原名、并删掉目标副本：\n" + r.Dst + "\n\n" +
                     "迁移之后在这个目录里产生的新改动会丢失。";

        if (Native.MessageBoxW(hWnd, ask, "伪迁移助手", 0x04u | 0x30u) != 6) return;

        if (Engine.Rollback(r.Src, r.Bak, r.Dst, out string err, out string warn))
        {
            r.Status = "rolledback";
            State.Append(r);
            RefreshPending();
            if (warn.Length > 0) Native.MessageBoxW(hWnd, warn, "伪迁移助手", 0x40);
        }
        else
        {
            Native.MessageBoxW(hWnd, "回滚失败：\n\n" + err, "伪迁移助手", 0x30);
        }
    }

    private void OnReleaseSelected(IntPtr hWnd)
    {
        int i = SelectedIndex();
        if (i < 0)
        {
            Native.MessageBoxW(hWnd, "请先在列表里选中一项。", "伪迁移助手", 0x40);
            return;
        }

        var r = _rows[i];
        string ask = "删除这份备份，释放 " + Engine.Human(r.Bytes) + "？\n\n" +
                     r.Bak + "\n\n" +
                     "删除后就不能再回滚到迁移前了（但链接和目标数据不受影响）。";

        if (Native.MessageBoxW(hWnd, ask, "伪迁移助手", 0x04u | 0x30u) != 6) return;

        if (!Engine.DeleteTree(r.Bak, out string delErr))
        {
            Native.MessageBoxW(hWnd, "删除失败：\n\n" + delErr, "伪迁移助手", 0x30);
            return;
        }

        r.Status = "released";
        State.Append(r);
        RefreshPending();
    }

    /// <summary>
    /// 目标框填的是"放到哪个目录下"。如果该目录本身不叫源文件夹的名字，
    /// 默认再补一层，避免把内容直接摊进目标目录。
    /// </summary>
    private string EffectiveDest(string src, string dst, out bool appended)
    {
        appended = false;
        if (src.Length == 0 || dst.Length == 0) return dst;

        bool auto = Native.SendMessageW(_chkAuto, 0x00F0 /* BM_GETCHECK */, IntPtr.Zero, IntPtr.Zero) == (IntPtr)1;
        if (!auto) return dst;

        string leaf = Path.GetFileName(src);
        if (leaf.Length == 0) return dst;
        if (string.Equals(Path.GetFileName(dst), leaf, StringComparison.OrdinalIgnoreCase)) return dst;

        appended = true;
        return PathUtil.Join(dst, leaf);
    }

    private void UpdatePreview()
    {
        if (_preview == IntPtr.Zero || _chkAuto == IntPtr.Zero) return;

        string src = GetText(_srcEdit).Trim().TrimEnd('\\');
        string dst = GetText(_dstEdit).Trim().TrimEnd('\\');

        if (src.Length == 0 || dst.Length == 0)
        {
            Native.SetWindowTextW(_preview, "");
            return;
        }

        string effective = EffectiveDest(src, dst, out bool appended);
        Native.SetWindowTextW(_preview, appended
            ? "实际落点：" + effective + "（已自动补 " + Path.GetFileName(src) + "）"
            : "实际落点：" + effective);
    }

    private static string GetText(IntPtr h)
    {
        var sb = new StringBuilder(1024);
        Native.GetWindowTextW(h, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string SuggestDest(string src)
    {
        if (src.Length < 3 || src[1] != ':') return "";

        string letter = src.Substring(0, 1).ToUpperInvariant();
        string rest = src.Substring(2).TrimStart('\\');

        string? target = null;
        foreach (var d in DriveInfo.GetDrives())
        {
            if (!d.IsReady || d.DriveType != DriveType.Fixed) continue;
            if (d.Name.StartsWith(letter, StringComparison.OrdinalIgnoreCase)) continue;
            target = d.Name.TrimEnd('\\');
            break;
        }

        if (target is null) return "";
        return target + "\\Migrated\\" + letter + "\\" + rest;
    }

    private void StartMigration(IntPtr hWnd, string src, string dst)
    {
        _src = src;
        _dst = dst;
        _busy = true;
        _p = new ProgressState { TotalBytes = 1 };
        _lastBytes = 0;
        _lastTicks = Environment.TickCount64;
        _speed = 0;

        Native.SetWindowTextW(_taskTitle, "正在迁移  " + src);
        Native.SetWindowTextW(_stageText, "① 检查中…");
        Native.SetWindowTextW(_pctText, "0%");
        Native.SetWindowTextW(_speedText, "");
        Native.SetWindowTextW(_fileText, "");
        Native.SetWindowTextW(_countText, "");
        Native.SendMessageW(_bar, Native.PBM_SETPOS, IntPtr.Zero, IntPtr.Zero);
        ShowPage(hWnd, task: true);
        Native.SetTimer(hWnd, (IntPtr)TimerId, 100, IntPtr.Zero);

        var p = _p;
        _worker = new Thread(() => RunFlow(p, src, dst)) { IsBackground = true };
        _worker.Start();
    }

    private void RunFlow(ProgressState p, string src, string dst)
    {
        string incoming = dst + ".__incoming__";
        try
        {
            if (Directory.Exists(dst) && !Engine.IsEmptyDir(dst))
            {
                p.Stage = (int)Stage.Failed;
                p.Detail = "目标文件夹已存在且不为空：\n\n" + dst + "\n\n请换一个位置，或先清空它。";
                return;
            }

            string? werr = Engine.CheckWritable(incoming);
            if (werr is not null)
            {
                p.Stage = (int)Stage.Failed;
                p.Detail = "目标位置不可写，已中止。\n\n" + dst + "\n\n" + werr +
                           "\n\n" + Engine.Diagnose(dst);
                return;
            }

            p.Stage = (int)Stage.Scanning;
            var scan = Engine.Scan(src, p);

            if (scan.DeniedPaths.Count > 0)
            {
                var deny = new StringBuilder();
                deny.AppendLine($"有 {scan.DeniedDirs} 个目录读不进去（权限不足）：");
                deny.AppendLine();
                for (int i = 0; i < scan.DeniedPaths.Count && i < 3; i++)
                    deny.AppendLine("· " + scan.DeniedPaths[i]);
                if (scan.DeniedDirs > 3) deny.AppendLine($"（还有 {scan.DeniedDirs - 3} 个）");
                deny.AppendLine();
                deny.AppendLine("这些目录里的文件不会被复制，迁移后会缺内容，所以先停下。");
                deny.AppendLine("可以用管理员身份运行本程序，或先调整这些目录的权限。");

                p.Stage = (int)Stage.Failed;
                p.Detail = deny.ToString();
                return;
            }

            // 只有"本来就超、而且被我们变得更长"才值得提醒。
            // 如果新路径更短，程序原来能读的，迁移后必然还能读。
            long newLongest = scan.LongestPath - src.Length + dst.Length;
            if (newLongest > 259 && newLongest > scan.LongestPath)
            {
                string warn = "迁移后最长的路径会达到 " + newLongest + " 个字符，超过 Windows 传统的 260 限制。\n\n" +
                              "复制本身没问题（程序内部已按长路径处理），但目标程序如果本身不支持长路径，\n" +
                              "这些文件迁移后可能会打不开。\n\n" +
                              "把目标位置换成更短的根目录（比如 D:\\M）可以缓解。\n\n" +
                              "要继续吗？";

                if (Native.MessageBoxW(_hwnd, warn, "伪迁移助手", 0x04u | 0x30u) != 6)
                {
                    p.Stage = (int)Stage.Failed;
                    p.CurrentFile = "已取消";
                    return;
                }
            }

            if (scan.SparseFiles.Count > 0)
            {
                var deny = new StringBuilder();
                deny.AppendLine($"这个文件夹里有 {scan.SparseFiles.Count} 个稀疏文件：");
                deny.AppendLine();
                for (int i = 0; i < scan.SparseFiles.Count && i < 3; i++)
                    deny.AppendLine("· " + scan.SparseFiles[i]);
                deny.AppendLine();
                deny.AppendLine("稀疏文件实际占用远小于文件大小，但复制后会被填成实打实的等大文件，");
                deny.AppendLine("可能直接把目标盘撑满，所以这里不做迁移。");

                p.Stage = (int)Stage.Failed;
                p.Detail = deny.ToString();
                return;
            }

            // 试改名：能改才值得复制，否则白等几十分钟
            if (!Engine.ProbeRename(src, out string probeErr))
            {
                p.Stage = (int)Stage.Failed;
                p.Detail = "这个文件夹现在改不了名，迁移的最后一步会失败，所以先停下。\n\n" +
                           probeErr + "\n\n" +
                           Locks.Report(src) + "\n" +
                           "把上面这些程序彻底关掉（含托盘图标）再试一次。";
                return;
            }

            p.TotalBytes = Math.Max(1, scan.TotalBytes);
            p.TotalFiles = scan.TotalFiles;
            _recBytes = scan.TotalBytes;
            _recFiles = scan.TotalFiles;
            p.DoneBytes = 0;
            p.DoneFiles = 0;
            p.Stage = (int)Stage.Copying;

            var copy = Engine.Copy(src, incoming, p, 8);
            p.LinksPreserved = copy.LinksPreserved;
            p.SkippedLinks = copy.LinksSkipped;
            if (p.CancelRequested)
            {
                QueueCleanup(incoming);
                p.Stage = (int)Stage.Failed;
                p.CurrentFile = "已取消，正在后台清理临时文件…";
                return;
            }

            p.Stage = (int)Stage.Verifying;
            p.DoneFiles = 0;
            var verify = Engine.Verify(src, incoming, p);

            bool ok = copy.Errors == 0 && verify.Missing == 0 &&
                      verify.SizeMismatch == 0 && verify.TimeMismatch == 0;

            if (!ok)
            {
                p.Stage = (int)Stage.Failed;
                p.CurrentFile = $"未完成：复制错误 {copy.Errors} 个，校验不一致 {verify.Missing + verify.SizeMismatch + verify.TimeMismatch} 个";

                var sb = new StringBuilder();
                sb.AppendLine("没有完成这次迁移。源目录未做任何修改。");
                sb.AppendLine();
                int shown = 0;
                foreach (var e in copy.ErrorMessages)
                {
                    if (shown++ >= 3) break;
                    sb.AppendLine("· " + e);
                }
                for (int i = 0; i < verify.Samples.Count && i < 3; i++)
                    sb.AppendLine("· " + verify.Samples[i]);
                if (copy.Errors > 3) sb.AppendLine($"（另有 {copy.Errors - 3} 个错误未显示）");

                bool locked = false;
                foreach (var e in copy.ErrorMessages)
                {
                    if (e.Contains("0x00000020", StringComparison.Ordinal)) { locked = true; break; }
                }
                if (locked)
                {
                    sb.AppendLine();
                    sb.AppendLine("提示：有文件正被其他程序占用，常见于微信、QQ、浏览器没有完全退出。");
                    sb.AppendLine("把它们退干净（包括托盘区图标）后再试一次。");
                }

                p.Detail = sb.ToString();
                QueueCleanup(incoming);
                return;
            }

            // 校验通过，开始换位：改名 → 建链接 → 探针
            var swap = Engine.Swap(src, dst, incoming, p);
            if (!swap.Ok)
            {
                QueueCleanup(incoming);
                p.Stage = (int)Stage.Failed;
                p.Detail = swap.Error;
                return;
            }

            _lastSrc = src;
            _lastDst = dst;
            _lastBak = swap.BakPath;
            _lastId = "m_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            p.Stage = (int)Stage.Done;
            p.CurrentFile = $"完成：{copy.FilesCopied} 个文件 / {Engine.Human(copy.BytesCopied)}";
        }
        catch (Exception ex)
        {
            p.Stage = (int)Stage.Failed;
            p.CurrentFile = "异常：" + ex.Message;
        }
        finally
        {
            Native.PostMessageW(_hwnd, Native.WM_APP_DONE, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private void OnCancelClicked()
    {
        if (_p is not null) _p.CancelRequested = true;
        Native.SetWindowTextW(_stageText, "正在取消…");
    }

    private void OnTick()
    {
        var p = _p;
        if (p is null) return;

        long now = Environment.TickCount64;
        long done = Interlocked.Read(ref p.DoneBytes);

        if (now > _lastTicks + 300)
        {
            double dt = (now - _lastTicks) / 1000.0;
            double inst = (done - _lastBytes) / 1024.0 / 1024.0 / dt;
            _speed = _speed <= 0 ? inst : _speed * 0.7 + inst * 0.3;
            _lastBytes = done;
            _lastTicks = now;
        }

        p.TotalBytes = Math.Max(1, Interlocked.Read(ref p.TotalBytes));
        long total = Interlocked.Read(ref p.TotalBytes);
        int permille = (int)(done * 1000 / total);
        if (permille > 1000) permille = 1000;

        Native.SendMessageW(_bar, Native.PBM_SETPOS, (IntPtr)permille, IntPtr.Zero);

        var stage = (Stage)p.Stage;
        string stageText = stage switch
        {
            Stage.Scanning => "① 检查中…",
            Stage.Copying => "② 复制中",
            Stage.Verifying => "③ 校验中",
            Stage.Done => "✓ 完成",
            Stage.Failed => "✗ 失败",
            _ => "",
        };
        Native.SetWindowTextW(_stageText, stageText);

        Native.SetWindowTextW(_pctText,
            $"{permille / 10.0:F1}%   {Engine.Human(done)} / {Engine.Human(total)}");

        if (stage == Stage.Copying && _speed > 0)
        {
            double left = total - done;
            double secs = left / 1024.0 / 1024.0 / _speed;
            string eta = secs < 60
                ? $"{secs:F0} 秒"
                : $"{secs / 60:F0} 分 {(int)(secs % 60):00} 秒";
            Native.SetWindowTextW(_speedText, $"{_speed:F1} MB/s   剩余约 {eta}");
        }
        else
        {
            Native.SetWindowTextW(_speedText, "");
        }

        string cur = p.CurrentFile;
        if (cur.Length > 110) cur = "…" + cur.Substring(cur.Length - 108);
        Native.SetWindowTextW(_fileText, stage == Stage.Copying ? "正在复制  " + cur : cur);

        Native.SetWindowTextW(_countText,
            $"{Interlocked.CompareExchange(ref p.DoneFiles, 0, 0)} / {p.TotalFiles} 个文件");
    }

    private void OnWorkerDone(IntPtr hWnd)
    {
        Native.KillTimer(hWnd, (IntPtr)TimerId);
        _busy = false;

        var p = _p;
        if (p is null) return;

        // 最后刷一次
        Native.SendMessageW(_bar, Native.PBM_SETPOS,
            (IntPtr)(p.Stage == (int)Stage.Done ? 1000 : 0), IntPtr.Zero);

        if (p.Stage == (int)Stage.Done)
        {
            State.Append(new MigrationRecord
            {
                Id = _lastId,
                Src = _lastSrc,
                Dst = _lastDst,
                Bak = _lastBak,
                Bytes = _recBytes,
                Files = _recFiles,
                At = DateTime.Now.ToString("O"),
                Status = "linked",
            });

            Native.SetWindowTextW(_stageText, "✓ 迁移完成");
            Native.SetWindowTextW(_fileText, "原路径已变成目录联接 → " + _lastDst);
            Native.SetWindowTextW(_countText, "副本保留在 " + _lastBak);

            string linkNote = "";
            if (p.LinksPreserved > 0)
                linkNote += $"其中 {p.LinksPreserved} 个链接/重解析点已按原样重建。\n\n";
            if (p.SkippedLinks > 0)
                linkNote += $"有 {p.SkippedLinks} 个链接无法重建，已跳过。\n\n";

            string done = "迁移完成，探针验证通过。\n\n" + linkNote +
                          "原路径现在是目录联接，指向：\n" + _lastDst + "\n\n" +
                          "原数据保留在：\n" + _lastBak + "\n\n" +
                          "已加入“待释放”列表。\n" +
                          "建议先正常用几天，确认没问题后再点“释放所选”删掉副本；\n" +
                          "如果发现异常，点“回滚所选”就能恢复原样。";

            Native.MessageBoxW(hWnd, done, "伪迁移助手", 0x40u);

            RefreshPending();
        }
        else
        {
            string msg = p.Detail.Length > 0 ? p.Detail : "未完成：" + p.CurrentFile;
            Native.MessageBoxW(hWnd, msg, "伪迁移助手", 0x30u);
        }

        Native.SetWindowTextW(_taskTitle, "");
        ShowPage(hWnd, task: false);
    }

    private static string? PickFolder(IntPtr owner, string title) => Shell.PickFolder(owner, title);
}

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // 把窗口渲染成 BMP（任务页）
        if (args.Length >= 2 && args[0].Equals("/shottask", StringComparison.OrdinalIgnoreCase))
        {
            new MainWindow { ShotPath = args[1], ShotTask = true }.Run();
            Environment.Exit(0);
            return;
        }

        // 把窗口渲染成 BMP（带假数据），用于检查列表渲染
        if (args.Length >= 2 && args[0].Equals("/shotlist", StringComparison.OrdinalIgnoreCase))
        {
            new MainWindow { ShotPath = args[1], ShotSeed = true }.Run();
            Environment.Exit(0);
            return;
        }

        // 把窗口渲染成 BMP，用于无桌面环境下的界面检查
        if (args.Length >= 2 && args[0].Equals("/shot", StringComparison.OrdinalIgnoreCase))
        {
            new MainWindow { ShotPath = args[1] }.Run();
            Environment.Exit(0);
            return;
        }

        // 查占用：谁是占用者
        if (args.Length >= 3 && args[0].Equals("/locktest", StringComparison.OrdinalIgnoreCase))
        {
            File.WriteAllText(args[2], Locks.Report(args[1]));
            Environment.Exit(0);
            return;
        }

        // 环境诊断：把各位置的写入能力写到文件
        if (args.Length >= 3 && args[0].Equals("/diag", StringComparison.OrdinalIgnoreCase))
        {
            File.WriteAllText(args[2], Engine.Diagnose(args[1]));
            Environment.Exit(0);
            return;
        }

        // 台账读写往返测试
        if (args.Length >= 2 && args[0].Equals("/statetest", StringComparison.OrdinalIgnoreCase))
        {
            Environment.Exit(SelfTest.RunStateTest(args[1]));
            return;
        }

        // 端到端自检：复制 → 校验 → 换位 → 建链 → 探针 → 回滚
        if (args.Length >= 4 && args[0].Equals("/swaptest", StringComparison.OrdinalIgnoreCase))
        {
            int code;
            try { code = SelfTest.RunSwap(args[1], args[2], args[3]); }
            catch (Exception ex)
            {
                try { File.WriteAllText(args[3] + ".crash", ex.ToString()); } catch { }
                code = 9;
            }
            Environment.Exit(code);
            return;
        }

        // 不弹 UI，只验证 AOT 下 COM 互操作是否可用
        if (args.Length >= 2 && args[0].Equals("/comtest", StringComparison.OrdinalIgnoreCase))
        {
            string r;
            try { r = Shell.ComTest(); }
            catch (Exception ex) { r = "EX: " + ex; }
            File.WriteAllText(args[1], r);
            Environment.Exit(0);
            return;
        }

        // 隐藏的自动化入口：/selftest <src> <dst> <report>
        if (args.Length >= 4 && args[0].Equals("/selftest", StringComparison.OrdinalIgnoreCase))
        {
            try { File.WriteAllText(args[3] + ".start", DateTime.Now.ToString("O")); } catch { }
            int code;
            try
            {
                code = SelfTest.Run(args[1], args[2], args[3]);
            }
            catch (Exception ex)
            {
                try { File.WriteAllText(args[3] + ".crash", ex.ToString()); } catch { }
                code = 9;
            }
            Environment.Exit(code);
            return;
        }

        Native.SetProcessDpiAwarenessContext((IntPtr)(-4));
        Shell.EnsureCom();
        new MainWindow().Run();
    }
}
