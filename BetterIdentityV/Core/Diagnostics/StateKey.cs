namespace BetterIdentityV.Core.Diagnostics;

/// <summary>
/// 会话内的强类型状态标识，同一个标识始终对应同一种快照类型。
/// </summary>
public sealed class StateKey<T>(string name) where T : class
{
    public string Name { get; } = name;
}
