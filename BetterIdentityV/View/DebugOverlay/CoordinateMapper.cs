using System.Windows;
using BetterIdentityV.Core.Geometry;

namespace BetterIdentityV.View.DebugOverlay;

/// <summary>
/// 遮罩的局部布局信息。图像像素到 WPF 单位的转换只在显示侧进行。
/// </summary>
public readonly record struct OverlayViewport(
    ImageSize CaptureSize, double Width, double Height, double PixelsPerDip)
{
    public bool IsValid => CaptureSize.IsValid && Width > 0 && Height > 0 &&
                           double.IsFinite(Width) && double.IsFinite(Height) &&
                           double.IsFinite(PixelsPerDip) && PixelsPerDip > 0;
    public Rect Bounds => new(0, 0, Width, Height);
}

public sealed class CoordinateMapper
{
    public CoordinateMapper(ImageSize sourceSize, OverlayViewport viewport)
    {
        if (!sourceSize.IsValid || !viewport.IsValid)
            throw new ArgumentOutOfRangeException(nameof(sourceSize));
        ScaleX = viewport.Width / sourceSize.Width;
        ScaleY = viewport.Height / sourceSize.Height;
    }

    public double ScaleX { get; }
    public double ScaleY { get; }

    public Point Map(PointD point) => new(point.X * ScaleX, point.Y * ScaleY);

    public Rect Map(RectD rect) => new(rect.X * ScaleX, rect.Y * ScaleY,
        rect.Width * ScaleX, rect.Height * ScaleY);
}
