using System.Diagnostics;
using System.Drawing;
using BetterIdentityV.GameCapture;
using Vanara.PInvoke;

namespace BetterIdentityV.GameTask;

public class SystemControl
{
    public static nint FindGameHandle()
    {
        return FindHandleByProcessName("dwrg", "MuMuNxDevice");
    }

    /// <summary>
    /// 获取捕获窗口句柄，当传入MuMu模拟器的顶层窗口时返回MuMu渲染窗口，否则返回传入值
    /// </summary>
    /// <param name="hWnd">顶层窗口句柄</param>
    /// <returns>要捕获窗口的句柄</returns>
    public static nint FindCaptureAreaHandle(nint hWnd)
    {
        if (hWnd == 0)
            return 0;

        if (IsMuMulatorWindow(hWnd))
        {
            var renderHandle = FindMuMuRenderHandle(hWnd);
            if (renderHandle != 0)
                return renderHandle;
        }

        return hWnd;
    }
    
    public static nint FindMuMuRenderHandle(nint muMuTopWindowHwnd)
    {
        if (muMuTopWindowHwnd == 0)
            return 0;

        return (nint)User32.FindWindowEx(muMuTopWindowHwnd, IntPtr.Zero, "Qt5156QWindowIcon", "MuMuNxDevice");
    }
    
    public static bool IsMuMulatorWindow(nint hWnd)
    {
        var process = GetProcessByHandle(hWnd);
        return process?.ProcessName == "MuMuNxDevice";
    }
    
    public static bool IsGameActive()
    {
        var hWnd = User32.GetForegroundWindow();
        return hWnd == TaskContext.Instance().GameHandle;
    }
    
    public static nint FindHandleByProcessName(params string[] names)
    {
        foreach (var name in names)
        {
            var pros = Process.GetProcessesByName(name);
            if (pros.Length is not 0)
            {
                return pros[0].MainWindowHandle;
            }
        }

        return 0;
    }
    
    public static string? GetActiveProcessName()
    {
        try
        {
            var hWnd = User32.GetForegroundWindow();
            _ = User32.GetWindowThreadProcessId(hWnd, out var pid);
            var p = Process.GetProcessById((int)pid);
            return p.ProcessName;
        }
        catch
        {
            return null;
        }
    }
    
    public static Process? GetProcessByHandle(nint hWnd)
    {
        try
        {
            _ = User32.GetWindowThreadProcessId(hWnd, out var pid);
            var p = Process.GetProcessById((int)pid);
            return p;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }
    
    /// <summary>
    /// 获取窗口位置
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns>包括边框、标题栏在内的完整屏幕区域</returns>
    public static RECT GetWindowRect(nint hWnd)
    {
        DwmApi.DwmGetWindowAttribute<RECT>(hWnd, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out var windowRect);
        return windowRect;
    }

    /// <summary>
    /// 游戏本身分辨率获取
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns>不包括边框、标题栏在内的窗口客户区域</returns>
    public static RECT GetGameScreenRect(nint hWnd)
    {
        User32.GetClientRect(hWnd, out var clientRect);
        return clientRect;
    }

    /// <summary>
    /// GetWindowRect or GetGameScreenRect
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns>裁剪掉标题栏的窗口区域</returns>
    public static RECT GetCaptureRect(nint hWnd)
    {
        if (IsChildWindow(hWnd))
        {
            User32.GetClientRect(hWnd, out var childClientRect);
            POINT point = default;
            User32.ClientToScreen(hWnd, ref point);
            return new RECT(point.X, point.Y, point.X + childClientRect.Width, point.Y + childClientRect.Height);
        }
        
        var windowRect = GetWindowRect(hWnd);
        var gameScreenRect = GetGameScreenRect(hWnd);
        var left = windowRect.Left;
        var top = windowRect.Top + windowRect.Height - gameScreenRect.Height;
        var right = left + gameScreenRect.Width;
        var bottom = top + gameScreenRect.Height;
        return new RECT(left, top, right, bottom);
    }
    
    public static bool IsChildWindow(nint hWnd)
    {
        var style = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_STYLE);
        return (style & (int)User32.WindowStyles.WS_CHILD) != 0;
    }
    
    public static void ActivateWindow(nint hWnd)
    {
        if (User32.IsIconic(hWnd)) //只有当窗口最小化时才恢复，不影响处于最大化的窗口
        {
            User32.ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
        }
        User32.SetForegroundWindow(hWnd);
    }

    /// <summary>
    /// 确保最大化窗口还原回窗口化，返回客户区大小
    /// </summary>
    /// <param name="hWnd">窗口</param>
    /// <returns>还原后客户区大小</returns>
    public static Size RestoreWindowGetSize(nint hWnd)
    {
        if (User32.IsZoomed(hWnd))
        {
            User32.ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
            Thread.Sleep(500);
        }
        User32.GetClientRect(hWnd, out var clientRect);
        return new Size(clientRect.Width, clientRect.Height);
    }
    
    public static void ResizeWindowClientRect(nint hWnd, int clientWidth, int clientHeight)
    {
        if (hWnd == 0 || !User32.IsWindow(hWnd)) return;
        
        User32.GetWindowRect(hWnd, out var windowRect);
        User32.GetClientRect(hWnd, out var clientRect);
        // 计算边框和标题栏的尺寸
        int borderWidth = windowRect.Width - clientRect.Width;
        int borderHeight = windowRect.Height - clientRect.Height;
        // 计算新的窗口尺寸（包含边框和标题栏）
        int newWindowWidth = clientWidth + borderWidth;
        int newWindowHeight = clientHeight + borderHeight;
        // 调整窗口大小，保持位置不变
        User32.SetWindowPos(hWnd, IntPtr.Zero, windowRect.Left, windowRect.Top,
            newWindowWidth, newWindowHeight, User32.SetWindowPosFlags.SWP_NOZORDER);
    }
    
    public static CaptureModes GetAutoCaptureMode(IntPtr hWnd, CaptureModes mode)
    {
        if (CaptureModes.Auto.Equals(mode))
        {
            string name = GetProcessByHandle(hWnd)!.ProcessName;
            if (name == "MuMuNxDevice")
                mode = CaptureModes.WindowsGraphicsCapture;
            else
                mode = CaptureModes.BitBlt;
        }
        Console.WriteLine("使用截图模式" + mode);
        return mode;
    }
    
}