using BetterIdentityV.Core.Diagnostics;
using BetterIdentityV.View.DebugOverlay.Layers;

namespace BetterIdentityV.View.DebugOverlay.Presenters;

internal interface IDebugPresenter
{
    IReadOnlyList<DebugVisualLayer> Layers { get; }
    void Update(DebugSnapshot snapshot, OverlayViewport viewport);
}
