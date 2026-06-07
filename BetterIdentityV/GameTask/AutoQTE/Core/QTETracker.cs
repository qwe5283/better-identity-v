using System.Diagnostics;

namespace BetterIdentityV.GameTask.AutoQTE.Core;

/// <summary>
/// 状态追踪器：QTE状态机，负责运动趋势分析与击打预判
/// </summary>
public class QTETracker
{
    private readonly QTEAssets _assets;

    public QTETracker(QTEAssets assets)
    {
        _assets = assets;
    }
    
}