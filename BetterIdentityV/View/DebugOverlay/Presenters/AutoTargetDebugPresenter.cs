using System.Collections.Immutable;
using BetterIdentityV.Core.Diagnostics;
using BetterIdentityV.GameTask.AutoTarget;
using BetterIdentityV.View.DebugOverlay.Layers;

namespace BetterIdentityV.View.DebugOverlay.Presenters;

/// <summary>
/// 将 AutoTarget 的业务快照投影为独立的区域层和检测层。
/// </summary>
internal sealed class AutoTargetDebugPresenter : IDebugPresenter
{
    private readonly RegionLayer _region = new();
    private readonly DetectionLayer _detections = new();
    private AutoTargetGeometry? _previousGeometry;
    private AutoTargetFrameSnapshot? _previousFrame;
    private OverlayViewport _previousViewport;

    public IReadOnlyList<DebugVisualLayer> Layers { get; }

    public AutoTargetDebugPresenter()
    {
        Layers = [_region, _detections];
    }

    public void Update(DebugSnapshot snapshot, OverlayViewport viewport)
    {
        var geometry = snapshot.Get(AutoTargetDebugState.Geometry);
        if (snapshot.IsClosed || geometry is null || !viewport.IsValid ||
            geometry.ImageSize != viewport.CaptureSize)
        {
            // 尺寸切换期间隐藏旧帧，不把旧分辨率的结果拉伸到新画面。
            _region.Clear();
            _detections.Clear();
            _previousGeometry = null;
            _previousFrame = null;
            _previousViewport = default;
            return;
        }

        var viewportChanged = viewport != _previousViewport;
        var geometryChanged = !ReferenceEquals(geometry, _previousGeometry);
        if (geometryChanged || viewportChanged)
            _region.Render(geometry.Deadzone, geometry.ImageSize, viewport);

        var frame = snapshot.Get(AutoTargetDebugState.Frame);
        if (frame?.Geometry != geometry)
            frame = null;
        if (geometryChanged || viewportChanged || !ReferenceEquals(frame, _previousFrame))
            _detections.Render(frame?.Detections ?? ImmutableArray<DetectionAnnotation>.Empty,
                geometry.ImageSize, viewport);

        _previousGeometry = geometry;
        _previousFrame = frame;
        _previousViewport = viewport;
    }
}
