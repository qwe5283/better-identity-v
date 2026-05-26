using System.Diagnostics;
using Vanara.PInvoke;

namespace BetterIdentityV.GameTask;

public class SystemControl
{
    public static nint FindGameHandle()
    {
        return FindHandleByProcessName("dwrg", "MuMuNxDevice");
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
        var windowRect = GetWindowRect(hWnd);
        var gameScreenRect = GetGameScreenRect(hWnd);
        var left = windowRect.Left;
        var top = windowRect.Top + windowRect.Height - gameScreenRect.Height;
        var right = left + gameScreenRect.Width;
        var bottom = top + gameScreenRect.Height;
        return new RECT(left, top, right, bottom);
    }
    
    public static void ActivateWindow(nint hWnd)
    {
        if (User32.IsIconic(hWnd)) //只有当窗口最小化时才恢复，不影响处于最大化的窗口
        {
            User32.ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
        }
        User32.SetForegroundWindow(hWnd);
    }
    
}