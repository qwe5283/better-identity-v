using System.Windows;
using System.Windows.Media;
using BetterIdentityV.Core.Geometry;

namespace BetterIdentityV.View.DebugOverlay;

/// <summary>
/// 承载独立保留的绘图层，某一图层更新不会重新构造其他图层的绘图内容。
/// </summary>
public sealed class DebugOverlayControl : FrameworkElement
{
    private readonly VisualCollection _layers;

    public DebugOverlayControl()
    {
        _layers = new VisualCollection(this);
        IsHitTestVisible = false;
        ClipToBounds = true;
    }

    public ImageSize CaptureSize { get; set; }

    protected override int VisualChildrenCount => _layers.Count;
    protected override Visual GetVisualChild(int index) => _layers[index];

    internal void AddLayer(DrawingVisual layer) => _layers.Add(layer);
    internal void RemoveLayer(DrawingVisual layer) => _layers.Remove(layer);

    internal OverlayViewport GetViewport() => new(CaptureSize, ActualWidth, ActualHeight,
        VisualTreeHelper.GetDpi(this).PixelsPerDip);
}
