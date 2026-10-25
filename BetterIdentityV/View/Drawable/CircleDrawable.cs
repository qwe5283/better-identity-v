using System.Drawing;
using Point = System.Windows.Point;

namespace BetterIdentityV.View.Drawable;

/// <summary>
/// 遮罩窗口坐标中的圆形边界。
/// </summary>
[Serializable]
public sealed record CircleDrawable(Point Center, double Radius, Pen Pen)
{
    public bool IsEmpty => Radius <= 0;
}
