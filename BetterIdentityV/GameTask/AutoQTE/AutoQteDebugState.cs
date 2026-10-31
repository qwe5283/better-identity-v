using BetterIdentityV.Core.Diagnostics;
using BetterIdentityV.GameTask.AutoQTE.Core;

namespace BetterIdentityV.GameTask.AutoQTE;

public static class AutoQteDebugState
{
    public const string Source = "AutoQTE";
    public static readonly StateKey<QteGeometry> Geometry = new("Geometry");
    public static readonly StateKey<QteFrameSnapshot> Frame = new("Frame");
}

public sealed record QteFrameSnapshot(
    long FrameId,
    DateTimeOffset Timestamp,
    QteGeometry Geometry,
    QTEDetectionResult Detection,
    QTETrackResult Tracking);
