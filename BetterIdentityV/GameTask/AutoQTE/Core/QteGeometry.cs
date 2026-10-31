using BetterIdentityV.Core.Geometry;

namespace BetterIdentityV.GameTask.AutoQTE.Core;

/// <summary>
/// 原始截图中用于 QTE 的 16:9 画面，横向居中、纵向底对齐。
/// </summary>
public sealed record QteGeometry(ImageSize ImageSize, RectD CropBounds)
{
    public QteRegionGeometry Regions => QteRegionGeometry.Default;
    public ImageTransform NormalizedToCapture => new(Regions.ReferenceSize, CropBounds);

    public static QteGeometry Calculate(ImageSize imageSize)
    {
        var viewport = AspectRatioViewport.Calculate(imageSize, new ImageSize(16, 9),
            HorizontalViewportAlignment.Center, VerticalViewportAlignment.Bottom);
        return new QteGeometry(imageSize, viewport.PixelBounds);
    }
}

/// <summary>
/// 标准 1920x1080 画布上的检测几何，算法与显示共同使用。
/// </summary>
public sealed class QteRegionGeometry
{
    public static QteRegionGeometry Default { get; } = new();

    public ImageSize ReferenceSize { get; } = new(1920, 1080);
    public RectD DetectionBounds { get; }
    public CircleGeometry PolarCircle { get; }
    public AnnularSectorGeometry RedBand { get; }
    public AnnularSectorGeometry YellowBand { get; }

    private QteRegionGeometry()
    {
        var center = new PointD(967, 900);
        const int radius = 338;
        const int thickness = 22;
        const int startAngle = 200;
        const int endAngle = 340;
        PolarCircle = new CircleGeometry(center, radius);
        RedBand = new AnnularSectorGeometry(center, Math.Max(0, radius - thickness * 4),
            radius, startAngle, endAngle);
        YellowBand = new AnnularSectorGeometry(center, Math.Max(0, radius - thickness - 5),
            radius, startAngle, endAngle);

        var half = radius + 10;
        var left = Math.Max(0, center.X - half);
        var top = Math.Max(0, center.Y - half);
        DetectionBounds = new RectD(left, top,
            Math.Min(ReferenceSize.Width, center.X + half) - left,
            Math.Min(ReferenceSize.Height, center.Y + half) - top);
    }
}
