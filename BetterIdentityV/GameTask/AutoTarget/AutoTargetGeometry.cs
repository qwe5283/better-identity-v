using BetterIdentityV.Core.Geometry;

namespace BetterIdentityV.GameTask.AutoTarget;

/// <summary>
/// 控制判断和调试显示共用的死区定义，坐标属于原始整帧。
/// </summary>
public sealed record AutoTargetGeometry(ImageSize ImageSize, CircleGeometry Deadzone)
{
    public static AutoTargetGeometry Calculate(ImageSize imageSize, double deadzoneRadius)
    {
        if (!double.IsFinite(deadzoneRadius) || deadzoneRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(deadzoneRadius));

        // 居中裁出最大 16:9 区域，将 1280x720 预览中的 (640,317) 映回整帧。
        var viewport = AspectRatioViewport.Calculate(imageSize, new ImageSize(16, 9),
            HorizontalViewportAlignment.Center, VerticalViewportAlignment.Center);
        var center = new ImageTransform(new ImageSize(1280, 720), viewport.Bounds)
            .Map(new PointD(640, 317));
        return new AutoTargetGeometry(imageSize, new CircleGeometry(center, deadzoneRadius));
    }
}
