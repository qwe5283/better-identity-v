using System.Windows.Interop;
using Vanara.PInvoke;

namespace BetterIdentityV.Helpers;

public class DpiHelper
{
    /// <summary>
    /// 获取主窗口所在显示器的 DPI 缩放，只能主线程调用
    /// </summary>
    public static float ScaleY => GetScaleY();

    private static float GetScaleY()
    {
        //TODO: 根据系统版本选择DPI获取方式
        HWND hWnd = new WindowInteropHelper(UIDispatcherHelper.MainWindow).Handle;
        HMONITOR hMonitor = User32.MonitorFromWindow(hWnd, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
        SHCore.GetDpiForMonitor(hMonitor, SHCore.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out _, out uint dpiY);
        return dpiY / 96f;
    }
}
