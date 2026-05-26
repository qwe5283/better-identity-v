using System.Diagnostics;
using BetterIdentityV.Core.Simulator;
using BetterIdentityV.GameTask.AutoQTE.Core;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;

namespace BetterIdentityV.GameTask.AutoQTE;

public class AutoQTETrigger : ITaskTrigger
{
    private readonly ILogger<AutoQTETrigger> _logger = App.GetLogger<AutoQTETrigger>();
    private bool _isEnabled;
    private Thread _workerThread;
    private CancellationTokenSource _cts;
    private QTEAssets _assets;
    private QTEDetector _detector;
    private QTETracker _tracker;
    
    public string Name => "自动QTE校准";

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            // 停止独立线程循环
            if (!_isEnabled)
                StopLoop();
        }
    }
    public int Priority => 30;
    public bool IsExclusive => false;
    public bool IsBackgroundRunning => false; // 自定义截图循环实现，不需要托管调度器后台截图

    public AutoQTETrigger()
    {
    }

    public void Init()
    {
        var config = TaskContext.Instance().Config.AutoQTEConfig;
        IsEnabled = config.Enabled;
        
        _assets = new QTEAssets();
        // 启动独立线程循环
        if (IsEnabled)
        {
            StartLoop();
        }
    }

    public void OnCapture(CaptureContent content)
    {
        // 不使用TaskTriggerDispatcher的截图回调，自己实现独立线程高频截图
    }

    private void StartLoop()
    {
        if (_workerThread != null && _workerThread.IsAlive) return;
        
        _cts = new CancellationTokenSource();
        _workerThread = new Thread(WorkerLoop) { IsBackground = true, Name = "AutoQTE_Worker" };
        _workerThread.Start();
        
        _logger.LogInformation("AutoQTE触发器启动后台循环");
    }
    
    private void StopLoop()
    {
        _cts?.Cancel();
        bool alreadyTerminated = _workerThread?.Join(500) ?? false;
        if (!alreadyTerminated)
            _logger.LogError("AutoQTE线程停止超时");
        _cts?.Dispose();
        _cts = null;
        _workerThread = null;
        
        _detector?.Dispose();
        _detector = null;
        _tracker = null;
        
        _logger.LogInformation("AutoQTE触发器停止后台循环");
    }

    /// <summary>
    /// 截图主循环
    /// </summary>
    private void WorkerLoop()
    {
        var dispatcher = TaskTriggerDispatcher.Instance();

        while (!_cts.Token.IsCancellationRequested)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            try
            {
                using var frame = dispatcher.GameCapture.Capture();
                if (frame == null || frame.IsDisposed || frame.Empty())
                {
                    Thread.Sleep(10);
                    continue;
                }

                // 动态适配分辨率变化
                if (_detector == null || _detector.Width != frame.Width || _detector.Height != frame.Height)
                {
                    _detector?.Dispose();
                    _detector = new QTEDetector(frame.Width, frame.Height, _assets);
                    _tracker = new QTETracker(_assets);
                }

                var (redAngle, yellowSpan) = _detector.ProcessFrame(frame);
                double delaySec = _assets.ClientDelayMs / 1000.0;

                bool isHit = _tracker.UpdateAndCheck(redAngle, yellowSpan, delaySec);

                if (isHit)
                {
                    Simulation.SendInput.Keyboard.KeyPress(User32.VK.VK_SPACE);
                    _logger.LogInformation("AutoQTE按键触发");
                }

                // 限制最高帧率避免过度占用 CPU (约 120~250 FPS)
                Thread.Sleep(3);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                // 忽略截图丢失或窗口句柄失效等瞬时异常
                Thread.Sleep(50);
            }
            
            sw.Stop();
            double elapsed = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"Elapsed: {elapsed}ms | Fps: {1000 / elapsed}");
        }
    }
}