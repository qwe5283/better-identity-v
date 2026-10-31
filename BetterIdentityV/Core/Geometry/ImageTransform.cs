namespace BetterIdentityV.Core.Geometry;

/// <summary>
/// 将参考图像映回原始截图中的指定区域，保留裁剪偏移和横纵缩放比例。
/// </summary>
public readonly record struct ImageTransform(ImageSize SourceSize, RectD DestinationBounds)
{
    public double ScaleX => DestinationBounds.Width / SourceSize.Width;
    public double ScaleY => DestinationBounds.Height / SourceSize.Height;

    public PointD Map(PointD point) => new(
        DestinationBounds.X + point.X * ScaleX, DestinationBounds.Y + point.Y * ScaleY);

    public RectD Map(RectD rect) => new(
        DestinationBounds.X + rect.X * ScaleX, DestinationBounds.Y + rect.Y * ScaleY,
        rect.Width * ScaleX, rect.Height * ScaleY);
}
