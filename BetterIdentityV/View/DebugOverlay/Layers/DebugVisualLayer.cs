using System.Windows.Media;

namespace BetterIdentityV.View.DebugOverlay.Layers;

public abstract class DebugVisualLayer : DrawingVisual
{
    public void Clear()
    {
        using var context = RenderOpen();
    }
}
