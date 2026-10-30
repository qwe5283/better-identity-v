using BetterIdentityV.Core.Geometry;

namespace BetterIdentityV.GameTask.AutoTarget;

/// <summary>
/// 控制判断和调试显示共用的死区定义，坐标属于原始整帧。
/// </summary>
public sealed record AutoTargetGeometry(ImageSize ImageSize, CircleGeometry Deadzone)
{
    public static AutoTargetGeometry Calculate(ImageSize imageSize, double deadzoneRadius)
    {
        if (!imageSize.IsValid)
            throw new ArgumentOutOfRangeException(nameof(imageSize));
        if (!double.IsFinite(deadzoneRadius) || deadzoneRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(deadzoneRadius));

        // 居中裁出最大 16:9 区域，将 1280x720 预览中的 (640,317) 映回整帧。
        var cropHeight = Math.Min(imageSize.Height, imageSize.Width * 9d / 16d);
        var center = new PointD(imageSize.Width / 2d,
            imageSize.Height / 2d - cropHeight * 43d / 720d);
        return new AutoTargetGeometry(imageSize, new CircleGeometry(center, deadzoneRadius));
    }
}
