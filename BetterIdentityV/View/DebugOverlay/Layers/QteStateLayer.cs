using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BetterIdentityV.Core.Geometry;
using BetterIdentityV.GameTask.AutoQTE;

namespace BetterIdentityV.View.DebugOverlay.Layers;

/// <summary>
/// 每帧的指针、黄色目标区和跟踪状态，与静态检测范围分层更新。
/// </summary>
internal sealed class QteStateLayer : DebugVisualLayer
{
    private static readonly Typeface Typeface = new("Microsoft Yahei UI");

    public void Render(QteFrameSnapshot? frame, CoordinateMapper mapper, OverlayViewport viewport)
    {
        using var context = RenderOpen();
        if (frame is null)
            return;

        context.PushClip(new RectangleGeometry(viewport.Bounds));
        var regions = frame.Geometry.Regions;
        if (frame.Detection.YellowSpan is { } span)
        {
            var sector = regions.YellowBand with { StartAngle = span.Start, EndAngle = span.End };
            context.DrawGeometry(new SolidColorBrush(Color.FromArgb(80, 255, 255, 0)),
                new Pen(Brushes.Yellow, 2), mapper.Map(RegionShapes.Sector(sector)));
        }
        if (frame.Detection.RedAngle is { } angle)
        {
            var band = regions.RedBand;
            context.DrawLine(new Pen(Brushes.Red, 3),
                mapper.Map(RegionShapes.PointAt(band.Center, band.InnerRadius, angle)),
                mapper.Map(RegionShapes.PointAt(band.Center, band.OuterRadius, angle)));
        }

        var label = new FormattedText($"QTE · {frame.Tracking.Status}", CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, Typeface, 12, Brushes.White, viewport.PixelsPerDip);
        var bounds = regions.DetectionBounds;
        var point = mapper.Map(new PointD(bounds.X, bounds.Y));
        var labelPoint = new Point(Math.Max(0, point.X), Math.Max(0, point.Y - label.Height - 6));
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(180, 20, 24, 32)), null,
            new Rect(labelPoint, new Size(label.Width + 8, label.Height + 4)));
        context.DrawText(label, new Point(labelPoint.X + 4, labelPoint.Y + 2));
        context.Pop();
    }
}
