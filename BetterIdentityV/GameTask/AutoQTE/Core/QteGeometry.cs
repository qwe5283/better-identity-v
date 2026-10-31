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
        if (!imageSize.IsValid)
            throw new ArgumentOutOfRangeException(nameof(imageSize));

        var width = imageSize.Width;
        var height = imageSize.Height;
        var cropWidth = width;
        var cropHeight = height;
        // 近似 16:9 的取整分辨率不裁切，Python round 使用银行家舍入。
        var difference = width / (double)height - 16d / 9;
        if (difference > 1e-3)
            cropWidth = Math.Clamp((int)Math.Round(height * 16d / 9), 1, width);
        else if (difference < -1e-3)
            cropHeight = Math.Clamp((int)Math.Round(width * 9d / 16), 1, height);

        return new QteGeometry(imageSize,
            new RectD((width - cropWidth) / 2, height - cropHeight, cropWidth, cropHeight));
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
