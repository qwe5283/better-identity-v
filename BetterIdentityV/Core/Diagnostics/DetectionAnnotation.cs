using BetterIdentityV.Core.Geometry;

namespace BetterIdentityV.Core.Diagnostics;

/// <summary>
/// 完整检测标注的数据，不包含绘图样式和窗口坐标。
/// </summary>
public sealed record DetectionAnnotation(
    RectD Bounds,
    int ClassId,
    string Label,
    double Confidence,
    bool IsSelected);
