using System.Windows;
using System.Windows.Media;
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
        : this(new ImageTransform(sourceSize,
            new RectD(0, 0, viewport.CaptureSize.Width, viewport.CaptureSize.Height)), viewport)
    {
    }

    public CoordinateMapper(ImageTransform transform, OverlayViewport viewport)
    {
        if (!transform.SourceSize.IsValid || !viewport.IsValid ||
            transform.DestinationBounds.Width <= 0 || transform.DestinationBounds.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(transform));
        var captureScaleX = viewport.Width / viewport.CaptureSize.Width;
        var captureScaleY = viewport.Height / viewport.CaptureSize.Height;
        ScaleX = transform.ScaleX * captureScaleX;
        ScaleY = transform.ScaleY * captureScaleY;
        OffsetX = transform.DestinationBounds.X * captureScaleX;
        OffsetY = transform.DestinationBounds.Y * captureScaleY;
    }

    public double ScaleX { get; }
    public double ScaleY { get; }
    public double OffsetX { get; }
    public double OffsetY { get; }

    public Point Map(PointD point) => new(OffsetX + point.X * ScaleX, OffsetY + point.Y * ScaleY);

    public Rect Map(RectD rect) => new(OffsetX + rect.X * ScaleX, OffsetY + rect.Y * ScaleY,
        rect.Width * ScaleX, rect.Height * ScaleY);

    public Geometry Map(Geometry shape)
    {
        var mapped = shape.Clone();
        mapped.Transform = new MatrixTransform(ScaleX, 0, 0, ScaleY, OffsetX, OffsetY);
        mapped.Freeze();
        return mapped;
    }
}
