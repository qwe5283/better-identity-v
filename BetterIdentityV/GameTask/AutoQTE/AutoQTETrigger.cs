using System.Diagnostics;
using BetterIdentityV.Core.Simulator;
using BetterIdentityV.GameTask.AutoQTE.Core;
using Microsoft.Extensions.Logging;
using OpenCvSharp;

namespace BetterIdentityV.GameTask.AutoQTE;

public class AutoQTETrigger : ITaskTrigger
{
    private readonly ILogger<AutoQTETrigger> _logger = App.GetLogger<AutoQTETrigger>();
    private readonly object _loopLock = new();
    
    private bool _isEnabled;
    private QTEAssets _assets = new();
    private QTEDetector? _detector;
    private QTETracker? _tracker;
    private Thread? _workerThread;
    private CancellationTokenSource? _workerCts;
    private double _delayCompSec;
    
    public string Name => "自动QTE校准";

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            // 停止后台线程
            if (!_isEnabled)
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
        _delayCompSec = Math.Max(0d, config.SystemDelayMs) / 1000d;
        IsEnabled = config.Enabled;
        
        // 启动后台线程
        if (IsEnabled)
            StartLoop();
    }

    public void OnCapture(CaptureContent content)
    {
        // 不使用TaskTriggerDispatcher的截图回调，自己实现独立线程高频截图
    }

    private void StartLoop()
    {
        lock (_loopLock)
        {
            if (_workerThread?.IsAlive == true)
                return;

            _detector?.Dispose();
            _detector = new QTEDetector(_assets);
            _tracker = new QTETracker(_assets);
            _workerCts = new CancellationTokenSource();
            _workerThread = new Thread(() => WorkerLoop(_workerCts.Token))
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
        _logger.LogDebug("结束QTE线程");
        Thread? threadToJoin;
        lock (_loopLock)
        {
            _workerCts?.Cancel();
            threadToJoin = _workerThread;
        }

        if (threadToJoin is not null && threadToJoin.IsAlive && threadToJoin != Thread.CurrentThread)
        {
            threadToJoin.Join(TimeSpan.FromSeconds(1));
        }

        lock (_loopLock)
        {
            _workerCts?.Dispose();
            _workerCts = null;
            _workerThread = null;
            _detector?.Dispose();
            _detector = null;
            _tracker = null;
        }
    }

    /// <summary>
    /// 处理主循环
    /// </summary>
    private void WorkerLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Loop");
                var dispatcher = TaskTriggerDispatcher.Instance();
                var capture = dispatcher.GameCapture;
                if (capture is not { IsCapturing: true })
                {
                    token.WaitHandle.WaitOne(50);
                    continue;
                }

                using var rawFrame = capture.Capture();
                if (rawFrame is null || rawFrame.Empty())
                {
                    token.WaitHandle.WaitOne(10);
                    continue;
                }

                using var frame1080p = NormalizeTo1080p(rawFrame);
                var detection = _detector?.Process(frame1080p) ?? default;
                var currentTimeSec = GetTimestampSec();
                var trackResult = _tracker?.Update(
                    detection.RedAngle,
                    detection.YellowSpan,
                    currentTimeSec,
                    _delayCompSec) ?? default;

                if (trackResult is { ShouldHit: true, HitTimeSec: not null })
                {
                    ExecuteHitAt(trackResult.HitTimeSec.Value, token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动QTE处理循环异常");
                token.WaitHandle.WaitOne(100);
            }
        }
    }
    
    private void ExecuteHitAt(double hitTimeSec, CancellationToken token)
    {
        var delayMs = (int)Math.Round((hitTimeSec - GetTimestampSec()) * 1000d);
        if (delayMs > 1)
        {
            token.WaitHandle.WaitOne(delayMs);
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        Simulation.SendInput.Keyboard.KeyPress(_assets.VkHitQTE);
        _logger.LogInformation("自动校准触发按键{Key}", _assets.VkHitQTE);
    }

    private static Mat NormalizeTo1080p(Mat frame)
    {
        if (frame.Width == 1920 && frame.Height == 1080)
        {
            return frame.Clone();
        }

        var targetHeight = (int)Math.Round(frame.Height * (1920d / frame.Width));
        var resized = new Mat();
        Cv2.Resize(frame, resized, new Size(1920, targetHeight));
        return resized;
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