using OpenCvSharp;

namespace BetterIdentityV.GameTask.AutoQTE.Core;

public static class QteFrameNormalizer
{
    /// <summary>
    /// 按共享几何先裁剪、再归一化，避免对整个非 16:9 画面直接拉伸。
    /// </summary>
    public static Mat Normalize(Mat frame, QteGeometry geometry)
    {
        if (frame.Empty() || frame.Width != geometry.ImageSize.Width || frame.Height != geometry.ImageSize.Height)
            throw new ArgumentException("原始截图尺寸与 QTE 几何不一致", nameof(frame));

        var bounds = geometry.CropBounds;
        using var crop = new Mat(frame, new Rect((int)bounds.X, (int)bounds.Y,
            (int)bounds.Width, (int)bounds.Height));
        var reference = geometry.Regions.ReferenceSize;
        if (crop.Width == reference.Width && crop.Height == reference.Height)
            return crop.Clone();

        var normalized = new Mat();
        try
        {
            Cv2.Resize(crop, normalized, new Size(reference.Width, reference.Height), 0, 0, InterpolationFlags.Linear);
            return normalized;
        }
        catch
        {
            normalized.Dispose();
            throw;
        }
    }
}
