using System.Windows.Media;
using BetterIdentityV.Core.Geometry;

namespace BetterIdentityV.View.DebugOverlay.Layers;

/// <summary>
/// 区域几何的显示，样式和坐标映射不进入业务算法。
/// </summary>
public sealed class RegionLayer : DebugVisualLayer
{
    private static readonly Pen Outline = CreateOutline();

    public void Render(CircleGeometry circle, ImageSize sourceSize, OverlayViewport viewport)
    {
        using var context = RenderOpen();
        if (circle.Radius <= 0)
            return;

        var mapper = new CoordinateMapper(sourceSize, viewport);
        context.PushClip(new RectangleGeometry(viewport.Bounds));
        context.DrawEllipse(null, Outline, mapper.Map(circle.Center),
            circle.Radius * mapper.ScaleX, circle.Radius * mapper.ScaleY);
        context.Pop();
    }

    public void Render(IReadOnlyList<(Geometry Shape, Pen Outline)> regions,
        CoordinateMapper mapper, OverlayViewport viewport)
    {
        using var context = RenderOpen();
        context.PushClip(new RectangleGeometry(viewport.Bounds));
        foreach (var region in regions)
            context.DrawGeometry(null, region.Outline, mapper.Map(region.Shape));
        context.Pop();
    }

    private static Pen CreateOutline()
    {
        var pen = new Pen(Brushes.Cyan, 1.5);
        pen.Freeze();
        return pen;
    }
}
