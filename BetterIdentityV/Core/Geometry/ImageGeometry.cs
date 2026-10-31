namespace BetterIdentityV.Core.Geometry;

/// <summary>
/// 图像空间的几何数据，单位为图像像素，不包含 DPI 和绘图样式。
/// </summary>
public readonly record struct ImageSize(int Width, int Height)
{
    public bool IsValid => Width > 0 && Height > 0;
}

public readonly record struct PointD(double X, double Y);

public readonly record struct RectD(double X, double Y, double Width, double Height);

public readonly record struct CircleGeometry(PointD Center, double Radius)
{
    public bool Contains(PointD point)
    {
        var dx = point.X - Center.X;
        var dy = point.Y - Center.Y;
        return dx * dx + dy * dy <= Radius * Radius;
    }
}

/// <summary>
/// 图像坐标中的环形扇区，角度从向右方向开始，随图像 Y 轴向下顺时针增加。
/// </summary>
public readonly record struct AnnularSectorGeometry(
    PointD Center, double InnerRadius, double OuterRadius, double StartAngle, double EndAngle);
