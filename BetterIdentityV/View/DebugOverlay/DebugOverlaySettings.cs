using System.Collections.Concurrent;

namespace BetterIdentityV.View.DebugOverlay;

/// <summary>
/// 按任务来源保存本次应用运行的显示选择，不进入配置文件。
/// </summary>
public sealed class DebugOverlaySettings
{
    private readonly ConcurrentDictionary<string, bool> _visibility = new();

    public bool IsVisible(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        return !_visibility.TryGetValue(source, out var visible) || visible;
    }

    public void SetVisible(string source, bool visible)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        _visibility[source] = visible;
    }
}
