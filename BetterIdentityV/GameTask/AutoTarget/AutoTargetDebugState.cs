using System.Collections.Immutable;
using BetterIdentityV.Core.Diagnostics;

namespace BetterIdentityV.GameTask.AutoTarget;

public static class AutoTargetDebugState
{
    public const string Source = "AutoTarget";
    public static readonly StateKey<AutoTargetGeometry> Geometry = new("Geometry");
    public static readonly StateKey<AutoTargetFrameSnapshot> Frame = new("Frame");
}

/// <summary>
/// 一帧完整检测结果。几何定义随帧保存，避免混用不同尺寸或配置的状态。
/// </summary>
public sealed record AutoTargetFrameSnapshot(
    long FrameId,
    DateTimeOffset Timestamp,
    AutoTargetGeometry Geometry,
    ImmutableArray<DetectionAnnotation> Detections);
