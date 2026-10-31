namespace BetterIdentityV.Core.Geometry;

public enum HorizontalViewportAlignment
{
    Left,
    Center,
    Right
}

public enum VerticalViewportAlignment
{
    Top,
    Center,
    Bottom
}

/// <summary>
/// 指定宽高比的最大内接视口。Bounds 保留精确几何，PixelBounds 用于实际截图裁剪。
/// </summary>
public sealed record AspectRatioViewport(RectD Bounds, RectD PixelBounds)
{
    // 实际宽高比：1366 / 768 ≈ 1.778646
    // 目标宽高比：16 / 9     ≈ 1.777778
    // 容差：0.001
    // 差值：                ≈ 0.000868 < 0.001
    private const double PixelAspectTolerance = 1e-3;

    public static AspectRatioViewport Calculate(ImageSize imageSize, ImageSize aspectRatio,
        HorizontalViewportAlignment horizontal = HorizontalViewportAlignment.Center,
        VerticalViewportAlignment vertical = VerticalViewportAlignment.Center)
    {
        if (!imageSize.IsValid)
            throw new ArgumentOutOfRangeException(nameof(imageSize));
        if (!aspectRatio.IsValid)
            throw new ArgumentOutOfRangeException(nameof(aspectRatio));

        var horizontalAnchor = horizontal switch
        {
            HorizontalViewportAlignment.Left => 0d,
            HorizontalViewportAlignment.Center => 0.5,
            HorizontalViewportAlignment.Right => 1d,
            _ => throw new ArgumentOutOfRangeException(nameof(horizontal))
        };
        var verticalAnchor = vertical switch
        {
            VerticalViewportAlignment.Top => 0d,
            VerticalViewportAlignment.Center => 0.5,
            VerticalViewportAlignment.Bottom => 1d,
            _ => throw new ArgumentOutOfRangeException(nameof(vertical))
        };

        var width = Math.Min(imageSize.Width, (double)imageSize.Height * aspectRatio.Width / aspectRatio.Height);
        var height = Math.Min(imageSize.Height, (double)imageSize.Width * aspectRatio.Height / aspectRatio.Width);
        var bounds = new RectD((imageSize.Width - width) * horizontalAnchor,
            (imageSize.Height - height) * verticalAnchor, width, height);

        // 像素裁剪沿用近似比例容差和银行家舍入，避免对 1366x768 等尺寸做无意义裁剪。
        var difference = imageSize.Width / (double)imageSize.Height - aspectRatio.Width / (double)aspectRatio.Height;
        if (Math.Abs(difference) <= PixelAspectTolerance)
            return new AspectRatioViewport(bounds, new RectD(0, 0, imageSize.Width, imageSize.Height));

        var pixelWidth = Math.Clamp((int)Math.Round(width), 1, imageSize.Width);
        var pixelHeight = Math.Clamp((int)Math.Round(height), 1, imageSize.Height);
        // 居中时额外的单个像素留在右侧/底部，保证所有边界都是整数且位于截图内。
        var pixelBounds = new RectD(Math.Floor((imageSize.Width - pixelWidth) * horizontalAnchor),
            Math.Floor((imageSize.Height - pixelHeight) * verticalAnchor), pixelWidth, pixelHeight);
        return new AspectRatioViewport(bounds, pixelBounds);
    }
}
