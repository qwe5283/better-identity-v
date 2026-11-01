using System.Windows;
using System.Windows.Threading;
using BetterIdentityV.Core.Diagnostics;
using BetterIdentityV.GameTask.AutoQTE;
using BetterIdentityV.GameTask.AutoTarget;
using BetterIdentityV.View.DebugOverlay.Presenters;

namespace BetterIdentityV.View.DebugOverlay;

/// <summary>
/// 在 UI 线程按显示频率读取最新快照，生产线程无须通知或等待窗口。
/// </summary>
public sealed class DebugOverlayController : IDisposable
{
    private sealed class Presentation(IDebugPresenter presenter)
    {
        public IDebugPresenter Presenter { get; } = presenter;
        public long Version { get; set; } = -1;
        public OverlayViewport Viewport { get; set; }
    }

    private readonly DebugHub _hub;
    private readonly DebugOverlayControl _control;
    private readonly DebugOverlaySettings _settings;
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<Guid, Presentation> _presentations = new();
    private bool _isDisposed;

    public DebugOverlayController(DebugHub hub, DebugOverlayControl control, DebugOverlaySettings settings)
    {
        _hub = hub;
        _control = control;
        _settings = settings;
        _timer = new DispatcherTimer(DispatcherPriority.Render, control.Dispatcher)
        {
            // 以 30 FPS 频率渲染调试图形
            Interval = TimeSpan.FromMilliseconds(1000d / 30)
        };
        _timer.Tick += OnTick;
        control.Loaded += OnLoaded;
        control.Unloaded += OnUnloaded;
        control.IsVisibleChanged += OnIsVisibleChanged;
        UpdateTimerState();
    }

    /// <summary>
    /// 显示侧主动刷新，也用于不启动游戏的离屏渲染验证。
    /// </summary>
    public void Refresh()
    {
        _control.Dispatcher.VerifyAccess();
        if (_isDisposed)
            return;

        var sessions = _hub.GetSessions();
        var active = sessions.Select(session => session.Id).ToHashSet();
        // 清理已经不存在的会话及其图层
        foreach (var id in _presentations.Keys.Where(id => !active.Contains(id)).ToArray())
        {
            RemoveLayers(_presentations[id]);
            _presentations.Remove(id);
        }

        var viewport = _control.GetViewport();
        foreach (var session in sessions)
        {
            if (!_presentations.TryGetValue(session.Id, out var presentation))
            {
                // 新增调试场景时只需在显示侧注册对应的 Presenter。
                IDebugPresenter? presenter = session.Source switch
                {
                    AutoTargetDebugState.Source => new AutoTargetDebugPresenter(),
                    AutoQteDebugState.Source => new AutoQteDebugPresenter(),
                    _ => null
                };
                if (presenter is null)
                    continue;
                presentation = new Presentation(presenter);
                _presentations.Add(session.Id, presentation);
                foreach (var layer in presenter.Layers)
                    _control.AddLayer(layer);
            }

            // 任务级显示开关独立于快照版本；隐藏期间仍更新最新诊断绘图。
            var opacity = _settings.IsVisible(session.Source) ? 1 : 0;
            foreach (var layer in presentation.Presenter.Layers)
                layer.Opacity = opacity;

            if (presentation.Version == session.Version && presentation.Viewport == viewport)
                continue;

            var snapshot = session.Capture();
            presentation.Presenter.Update(snapshot, viewport);
            presentation.Version = snapshot.Version;
            presentation.Viewport = viewport;
        }
    }

    private void OnTick(object? sender, EventArgs e) => Refresh();
    private void OnLoaded(object sender, RoutedEventArgs e) => UpdateTimerState();
    private void OnUnloaded(object sender, RoutedEventArgs e) => _timer.Stop();
    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdateTimerState();

    private void UpdateTimerState()
    {
        if (!_isDisposed && _control.IsLoaded && _control.IsVisible)
        {
            Refresh();
            _timer.Start();
        }
        else
        {
            _timer.Stop();
        }
    }

    private void RemoveLayers(Presentation presentation)
    {
        foreach (var layer in presentation.Presenter.Layers)
            _control.RemoveLayer(layer);
    }

    public void Dispose()
    {
        _control.Dispatcher.VerifyAccess();
        if (_isDisposed)
            return;
        _isDisposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _control.Loaded -= OnLoaded;
        _control.Unloaded -= OnUnloaded;
        _control.IsVisibleChanged -= OnIsVisibleChanged;
        foreach (var presentation in _presentations.Values)
            RemoveLayers(presentation);
        _presentations.Clear();
    }
}
