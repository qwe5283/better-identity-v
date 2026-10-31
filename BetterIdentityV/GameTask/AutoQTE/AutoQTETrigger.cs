using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using BetterIdentityV.Core.Diagnostics;
using BetterIdentityV.Core.Geometry;
using BetterIdentityV.Core.Simulator;
using BetterIdentityV.GameCapture;
using BetterIdentityV.GameTask.AutoQTE.Core;
using BetterIdentityV.GameTask.Model;
using BetterIdentityV.Helpers;
using Microsoft.Extensions.Logging;
using OpenCvSharp;

namespace BetterIdentityV.GameTask.AutoQTE;

public class AutoQTETrigger : ITaskTrigger, IDisposable, INotifyPropertyChanged
{
    private sealed record Settings(double DelayCompSec, bool BackgroundOperation);

    private readonly ILogger<AutoQTETrigger> _logger;
    private readonly DebugHub _debugHub;
    private readonly object _loopLock = new();
    private readonly QTEAssets _assets = new();
    
    private volatile bool _isEnabled;
    private Thread? _workerThread;
    private CancellationTokenSource? _workerCts;
    private Settings _settings = new(0, false);
    private volatile bool _isHealthy = true;
    private DebugSession? _debugSession;
    private QteGeometry? _geometry;
    private long _debugEpoch;
    private long _frameId;

    public AutoQTETrigger(DebugHub debugHub, ILogger<AutoQTETrigger> logger)
    {
        _debugHub = debugHub;
        _logger = logger;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public string Name => "自动QTE校准";

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
            {
                if (_isEnabled)
                    StartLoop();
                return;
            }

            _isEnabled = value;
            if (_isEnabled)
                StartLoop();
            else
                StopLoop();
        }
    }
    public int Priority => 30;
    // 自定义后台截图循环实现，不需要托管调度器后台截图，独占和后台运行均保留false，自己实现
    public bool IsExclusive => false;
    public bool IsBackgroundRunning => false;

    public void Init()
    {
        var config = TaskContext.Instance().Config.AutoQTEConfig;
        var settings = new Settings(Math.Max(0d, config.SystemDelayMs) / 1000d, config.RunBackgroundEnabled);
        lock (_loopLock)
        {
            if (_settings != settings)
                ResetDebugSession();
            Volatile.Write(ref _settings, settings);
        }
        // 任意有效分辨率均可裁剪成标准画布，不再以宽高比判断健康状态。
        IsHealthy = true;
        IsEnabled = config.Enabled;
    }
    
    public bool IsHealthy
    {
        get => _isHealthy;
        set
        {
            if (_isHealthy == value) return;
            _isHealthy = value;
            // 状态通知异步投递，避免 UI 等待工作线程退出时相互阻塞。
            if (Application.Current is { } application)
                _ = application.Dispatcher.InvokeAsync(() =>
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHealthy))));
            else
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHealthy)));
        }
    }

    public void OnCapture(CaptureContent content)
    {
        // 不使用TaskTriggerDispatcher的截图回调，自己实现独立线程高频截图
    }

    public void OnResolutionChanged(SystemInfo oldInfo, SystemInfo newInfo)
    {
        // 旧检测、预测和待击打时间随会话一起失效，下一帧按新尺寸重建跟踪状态。
        ResetDebugSession();
    }

    private void StartLoop()
    {
        lock (_loopLock)
        {
            if (!_isEnabled || _workerThread?.IsAlive == true)
                return;

            var capture = TaskTriggerDispatcher.Instance().GameCapture;
            var cts = new CancellationTokenSource();
            _workerCts = cts;
            _workerThread = new Thread(() => WorkerLoop(cts, capture))
            {
                IsBackground = true,
                Name = "AutoQTEWorker"
            };
            _workerThread.Start();
            _logger.LogDebug("启动QTE线程");
        }
    }
    
    private void StopLoop()
    {
        Thread? threadToJoin;
        lock (_loopLock)
        {
            ResetDebugSession();
            _workerCts?.Cancel();
            threadToJoin = _workerThread;
        }

        if (threadToJoin is not null && threadToJoin.IsAlive && threadToJoin != Thread.CurrentThread)
        {
            if (!threadToJoin.Join(TimeSpan.FromSeconds(1)))
                _logger.LogWarning("QTE线程未能在超时时间内结束，将等待其响应取消信号");
        }
    }

    public void Dispose()
    {
        IsEnabled = false;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 处理主循环
    /// </summary>
    private void WorkerLoop(CancellationTokenSource cts, IGameCapture? capture)
    {
        var token = cts.Token;
        using var detector = new QTEDetector(_assets, QteRegionGeometry.Default);
        var tracker = new QTETracker(_assets);
        var trackerEpoch = -1L;
        var errorLogged = false;
        try
        {
            while (!token.IsCancellationRequested)
            {
                var speedTimer = new SpeedTimer("QTE线程");
                try
                {
                    Settings settings;
                    long epoch;
                    lock (_loopLock)
                    {
                        settings = _settings;
                        epoch = _debugEpoch;
                    }
                    if (capture is not { IsCapturing: true } ||
                        (!settings.BackgroundOperation && !SystemControl.IsGameActive()))
                    {
                        ResetDebugSession();
                        token.WaitHandle.WaitOne(50);
                        continue;
                    }

                    var capturedAt = DateTimeOffset.UtcNow;
                    using var rawFrame = capture.Capture();
                    if (rawFrame is null || rawFrame.Empty())
                    {
                        ResetDebugSession();
                        token.WaitHandle.WaitOne(10);
                        continue;
                    }

                    var currentTimeSec = GetTimestampSec();
                    speedTimer.Record("捕获");
                    QteGeometry geometry;
                    DebugSession session;
                    lock (_loopLock)
                    {
                        if (token.IsCancellationRequested || !_isEnabled || epoch != _debugEpoch)
                            continue;
                        var size = new ImageSize(rawFrame.Width, rawFrame.Height);
                        if (_geometry is not null && _geometry.ImageSize != size)
                        {
                            // 截图线程可能先于分辨率通知发现尺寸变化。
                            ResetDebugSession();
                            epoch = _debugEpoch;
                        }
                        geometry = _geometry ??= QteGeometry.Calculate(size);
                        session = _debugSession ??= _debugHub.OpenSession(AutoQteDebugState.Source);
                        session.Set(AutoQteDebugState.Geometry, geometry);
                    }

                    using var frame1080P = QteFrameNormalizer.Normalize(rawFrame, geometry);
                    speedTimer.Record("裁剪并转1080P");
                    var detection = detector.Process(frame1080P);
                    speedTimer.Record("提取指针角度和黄色范围");
                    QTETrackResult trackResult;
                    lock (_loopLock)
                    {
                        if (token.IsCancellationRequested || !_isEnabled || epoch != _debugEpoch ||
                            !ReferenceEquals(session, _debugSession))
                            continue;
                        if (!settings.BackgroundOperation && !SystemControl.IsGameActive())
                        {
                            ResetDebugSession();
                            continue;
                        }
                        if (trackerEpoch != epoch)
                        {
                            tracker = new QTETracker(_assets);
                            trackerEpoch = epoch;
                        }
                        trackResult = tracker.Update(detection.RedAngle, detection.YellowSpan,
                            currentTimeSec, settings.DelayCompSec);
                        session.Set(AutoQteDebugState.Frame, new QteFrameSnapshot(
                            Interlocked.Increment(ref _frameId), capturedAt, geometry, detection, trackResult));
                    }

                    IsHealthy = true;
                    errorLogged = false;
                    if (trackResult.Status == QTETrackStatus.EmergencyHit)
                        _logger.LogWarning("预测拟合校准时间模型失败");
                    if (trackResult is { ShouldHit: true, HitTimeSec: not null })
                        ExecuteHitAt(trackResult.HitTimeSec.Value, token, capture, epoch, settings);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ResetDebugSession();
                    IsHealthy = false;
                    if (!errorLogged)
                    {
                        _logger.LogError(ex, "自动QTE处理循环异常");
                        errorLogged = true;
                    }
                    token.WaitHandle.WaitOne(100);
                }
                speedTimer.DebugPrint();
            }
        }
        finally
        {
            lock (_loopLock)
            {
                ResetDebugSession();
                _workerThread = null;
                _workerCts = null;
                cts.Dispose();
            }
            // 快速停用再启用时，等旧线程退出后才建立新的一次运行。
            if (_isEnabled)
                StartLoop();
        }
    }
    
    private void ExecuteHitAt(double hitTimeSec, CancellationToken token, IGameCapture capture,
        long epoch, Settings settings)
    {
        Random random = new Random();
        
        var delayMs = (int)Math.Round((hitTimeSec - GetTimestampSec()) * 1000d);
        if (delayMs > 1)
        {
            token.WaitHandle.WaitOne(delayMs);
        }

        if (token.IsCancellationRequested || !_isEnabled || epoch != Interlocked.Read(ref _debugEpoch))
        {
            return;
        }

        if (settings.BackgroundOperation)
        {
            TaskContext.Instance().PostMessageSimulator?.LongKeyPress(_assets.VkHitQTE, random.Next(500, 1000));
        }
        else
        {
            if (!SystemControl.IsGameActive())
                return;
            Simulation.SendInput.Keyboard.KeyPress(_assets.VkHitQTE);
        }
#if DEBUG
        using var debugFrame = capture.Capture();
        if (debugFrame is not null && !debugFrame.Empty())
            Cv2.ImWrite("debug.png", debugFrame);
#endif
        _logger.LogInformation("自动校准触发按键{Key}", _assets.VkHitQTE);
    }

    private void ResetDebugSession()
    {
        lock (_loopLock)
        {
            _debugSession?.Dispose();
            _debugSession = null;
            _geometry = null;
            Interlocked.Increment(ref _debugEpoch);
        }
    }

    /// <summary>
    /// 获取高精度时间戳（秒）
    /// </summary>
    /// <returns></returns>
    private static double GetTimestampSec()
    {
        return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
    }
}
