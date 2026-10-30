using System.Collections.Immutable;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BetterIdentityV.Core.Diagnostics;
using BetterIdentityV.Core.Geometry;

namespace BetterIdentityV.View.DebugOverlay.Layers;

/// <summary>
/// 将边框、标签底色和置信度作为一个完整标注绘制。
/// </summary>
public sealed class DetectionLayer : DebugVisualLayer
{
    private static readonly Typeface LabelTypeface = new("Microsoft Yahei UI");
    private static readonly Brush[] Palette =
    [
        CreateBrush(255, 56, 56), CreateBrush(255, 112, 31), CreateBrush(72, 249, 10),
        CreateBrush(0, 194, 255), CreateBrush(61, 219, 134), CreateBrush(82, 0, 133),
        CreateBrush(203, 56, 255), CreateBrush(255, 55, 199)
    ];

    public void Render(ImmutableArray<DetectionAnnotation> detections, ImageSize sourceSize,
        OverlayViewport viewport)
    {
        using var context = RenderOpen();
        if (detections.IsDefaultOrEmpty)
            return;
        var mapper = new CoordinateMapper(sourceSize, viewport);
        context.PushClip(new RectangleGeometry(viewport.Bounds));
        foreach (var detection in detections)
        {
            var bounds = detection.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0 ||
                !double.IsFinite(bounds.X) || !double.IsFinite(bounds.Y) ||
                !double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height))
                continue;

            var rect = mapper.Map(bounds);
            rect.Intersect(viewport.Bounds);
            if (rect.IsEmpty || rect.Width <= 0 || rect.Height <= 0)
                continue;

            var brush = Palette[(int)((uint)detection.ClassId % (uint)Palette.Length)];
            context.DrawRectangle(null, new Pen(brush, detection.IsSelected ? 3 : 2), rect);
            DrawLabel(context, detection, rect, brush, viewport);
        }
        context.Pop();
    }

    private static void DrawLabel(DrawingContext context, DetectionAnnotation detection, Rect box,
        Brush background, OverlayViewport viewport)
    {
        const double padding = 2;
        if (viewport.Width <= padding * 2)
            return;

        var color = ((SolidColorBrush)background).Color;
        var foreground = color.R * 0.299 + color.G * 0.587 + color.B * 0.114 > 160
            ? Brushes.Black : Brushes.White;
        var label = string.Create(CultureInfo.InvariantCulture,
            $"{(detection.IsSelected ? "> " : "")}{detection.Label} {detection.Confidence:0.00}");
        var text = new FormattedText(label, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            LabelTypeface, 12, foreground, viewport.PixelsPerDip)
        {
            MaxTextWidth = viewport.Width - padding * 2,
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis
        };
        var width = Math.Min(viewport.Width, text.Width + padding * 2);
        var height = text.Height + padding;
        var x = Math.Clamp(box.Left, 0, viewport.Width - width);
        // 顶部空间不足时将标签放在框内，保证标签不被遮罩边界截掉。
        var y = box.Top >= height ? box.Top - height : box.Top;
        y = Math.Clamp(y, 0, Math.Max(0, viewport.Height - height));
        context.DrawRectangle(background, null, new Rect(x, y, width, height));
        context.DrawText(text, new Point(x + padding, y + padding / 2));
    }

    private static Brush CreateBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
