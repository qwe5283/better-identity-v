using SharpDX.Direct3D11;
using Vanara.PInvoke;

namespace BetterIdentityV.GameCapture;

public static class CaptureSettings
{
    public const string CaptureAreaHandleKey = "captureAreaHandle";
    
    public static nint GetCaptureAreaHandle(nint hWnd, Dictionary<string, object>? settings)
    {
        if (settings != null && settings.TryGetValue(CaptureAreaHandleKey, out var value) && value is nint handle && handle != 0)
        {
            return handle;
        }

        return hWnd;
    }
    
    public static bool UsesCaptureAreaHandle(nint hWnd, nint captureAreaHandle)
    {
        return hWnd != 0 && captureAreaHandle != 0 && hWnd != captureAreaHandle;
    }
    
    public static ResourceRegion? CreateRelativeRegion(nint sourceWindowHandle, nint captureAreaHandle)
    {
        if (!UsesCaptureAreaHandle(sourceWindowHandle, captureAreaHandle))
        {
            return null;
        }

        POINT sourcePoint = default;
        User32.ClientToScreen(sourceWindowHandle, ref sourcePoint);
        return CreateRelativeRegionFromScreenOrigin(captureAreaHandle, sourcePoint.X, sourcePoint.Y);
    }

    public static ResourceRegion? CreateRelativeRegionFromScreenOrigin(nint captureAreaHandle, int originX, int originY)
    {
        if (!User32.GetClientRect(captureAreaHandle, out var captureAreaClientRect))
        {
            return null;
        }

        POINT captureAreaPoint = default;
        User32.ClientToScreen(captureAreaHandle, ref captureAreaPoint);

        var left = Math.Max(0, captureAreaPoint.X - originX);
        var top = Math.Max(0, captureAreaPoint.Y - originY);

        return new ResourceRegion
        {
            Left = left,
            Top = top,
            Right = left + captureAreaClientRect.Width,
            Bottom = top + captureAreaClientRect.Height,
            Front = 0,
            Back = 1
        };
    }
}