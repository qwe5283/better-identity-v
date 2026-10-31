using System.Windows;
using System.Windows.Media;
using BetterIdentityV.Core.Diagnostics;
using BetterIdentityV.GameTask.AutoQTE;
using BetterIdentityV.GameTask.AutoQTE.Core;
using BetterIdentityV.View.DebugOverlay.Layers;

namespace BetterIdentityV.View.DebugOverlay.Presenters;

internal sealed class AutoQteDebugPresenter : IDebugPresenter
{
    private readonly RegionLayer _region = new();
    private readonly QteStateLayer _state = new();
    private readonly (Geometry Shape, Pen Outline)[] _outlines;
    private QteGeometry? _previousGeometry;
    private QteFrameSnapshot? _previousFrame;
    private OverlayViewport _previousViewport;

    public IReadOnlyList<DebugVisualLayer> Layers { get; }

    public AutoQteDebugPresenter()
    {
        Layers = [_region, _state];
        var regions = QteRegionGeometry.Default;
        var roi = regions.DetectionBounds;
        var circle = regions.PolarCircle;
        _outlines =
        [
            (new RectangleGeometry(new Rect(roi.X, roi.Y, roi.Width, roi.Height)), new Pen(Brushes.Lime, 1)),
            (new EllipseGeometry(new Point(circle.Center.X, circle.Center.Y), circle.Radius, circle.Radius),
                new Pen(Brushes.Cyan, 1)),
            (RegionShapes.Sector(regions.RedBand), new Pen(Brushes.Red, 1)),
            (RegionShapes.Sector(regions.YellowBand), new Pen(Brushes.Yellow, 1))
        ];
    }

    public void Update(DebugSnapshot snapshot, OverlayViewport viewport)
    {
        var geometry = snapshot.Get(AutoQteDebugState.Geometry);
        if (snapshot.IsClosed || geometry is null || !viewport.IsValid || geometry.ImageSize != viewport.CaptureSize)
        {
            _region.Clear();
            _state.Clear();
            _previousGeometry = null;
            _previousFrame = null;
            _previousViewport = default;
            return;
        }

        var mapper = new CoordinateMapper(geometry.NormalizedToCapture, viewport);
        var geometryChanged = !ReferenceEquals(geometry, _previousGeometry);
        var viewportChanged = viewport != _previousViewport;
        if (geometryChanged || viewportChanged)
            _region.Render(_outlines, mapper, viewport);

        var frame = snapshot.Get(AutoQteDebugState.Frame);
        if (frame?.Geometry != geometry)
            frame = null;
        if (geometryChanged || viewportChanged || !ReferenceEquals(frame, _previousFrame))
            _state.Render(frame, mapper, viewport);

        _previousGeometry = geometry;
        _previousFrame = frame;
        _previousViewport = viewport;
    }
}
