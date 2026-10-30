namespace BetterIdentityV.Core.Diagnostics;

/// <summary>
/// 管理各次任务运行的诊断会话，不依赖窗口或 UI 线程。
/// </summary>
public sealed class DebugHub
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, DebugSession> _sessions = new();

    public DebugSession OpenSession(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        var session = new DebugSession(source, RemoveSession);
        lock (_gate)
            _sessions.Add(session.Id, session);
        return session;
    }

    public IReadOnlyList<DebugSession> GetSessions()
    {
        lock (_gate)
            return _sessions.Values.ToArray();
    }

    private void RemoveSession(DebugSession session)
    {
        lock (_gate)
            _sessions.Remove(session.Id);
    }
}
