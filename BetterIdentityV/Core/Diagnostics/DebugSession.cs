namespace BetterIdentityV.Core.Diagnostics;

/// <summary>
/// 一次任务运行的最新诊断状态。关闭后拒绝写入，避免旧推理结果重新出现。
/// </summary>
public sealed class DebugSession : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<object, object> _states = new();
    private readonly Action<DebugSession> _onClosed;
    private long _version;
    private bool _isClosed;

    internal DebugSession(string source, Action<DebugSession> onClosed)
    {
        Source = source;
        _onClosed = onClosed;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Source { get; }
    public long Version => Interlocked.Read(ref _version);

    /// <summary>
    /// 发布不可变快照，只替换最新值，不排队、不等待 UI。
    /// </summary>
    public bool Set<T>(StateKey<T> key, T snapshot) where T : class
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_gate)
        {
            if (_isClosed)
                return false;
            if (_states.TryGetValue(key, out var previous) && ReferenceEquals(previous, snapshot))
                return false;

            _states[key] = snapshot;
            Interlocked.Increment(ref _version);
            return true;
        }
    }

    public DebugSnapshot Capture()
    {
        lock (_gate)
            return new DebugSnapshot(_version, _isClosed, new Dictionary<object, object>(_states));
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_isClosed)
                return;
            _isClosed = true;
            _states.Clear();
            Interlocked.Increment(ref _version);
        }

        _onClosed(this);
    }
}

/// <summary>
/// 一次一致读取，不向显示侧暴露可修改的状态集合。
/// </summary>
public sealed class DebugSnapshot
{
    private readonly Dictionary<object, object> _states;

    internal DebugSnapshot(long version, bool isClosed, Dictionary<object, object> states)
    {
        Version = version;
        IsClosed = isClosed;
        _states = states;
    }

    public long Version { get; }
    public bool IsClosed { get; }

    public T? Get<T>(StateKey<T> key) where T : class =>
        _states.TryGetValue(key, out var state) ? (T)state : null;
}
