using System.Windows;
using System.Windows.Media;
using BetterIdentityV.Core.Geometry;

namespace BetterIdentityV.View.DebugOverlay.Layers;

/// <summary>
/// 业务几何到 WPF 图形的适配，角度方向与 OpenCV 极坐标一致。
/// </summary>
internal static class RegionShapes
{
    public static PointD PointAt(PointD center, double radius, double angle)
    {
        var radians = angle * Math.PI / 180;
        return new PointD(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }

    public static StreamGeometry Sector(AnnularSectorGeometry sector)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            Point At(double radius, double angle)
            {
                var point = PointAt(sector.Center, radius, angle);
                return new Point(point.X, point.Y);
            }

            var largeArc = sector.EndAngle - sector.StartAngle > 180;
            context.BeginFigure(At(sector.OuterRadius, sector.StartAngle), true, true);
            context.ArcTo(At(sector.OuterRadius, sector.EndAngle),
                new Size(sector.OuterRadius, sector.OuterRadius), 0, largeArc,
                SweepDirection.Clockwise, true, false);
            context.LineTo(At(sector.InnerRadius, sector.EndAngle), true, false);
            context.ArcTo(At(sector.InnerRadius, sector.StartAngle),
                new Size(sector.InnerRadius, sector.InnerRadius), 0, largeArc,
                SweepDirection.Counterclockwise, true, false);
        }
        geometry.Freeze();
        return geometry;
    }
}
