using System.Diagnostics;
using BetterIdentityV.GameCapture;
using BetterIdentityV.View;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;

namespace BetterIdentityV.GameTask;

/// <summary>
/// 连接游戏画面捕获、触发器逻辑执行和游戏状态监控的核心枢纽，通过统一的调度机制协调各类游戏任务触发器（如自动操作、UI 识别等）的运行，实现对游戏的自动化辅助功能。
/// </summary>
public class TaskTriggerDispatcher : IDisposable
{
    private readonly ILogger<TaskTriggerDispatcher> _logger = App.GetLogger<TaskTriggerDispatcher>();
    
    private static TaskTriggerDispatcher? _instance; //单例
    
    private readonly System.Timers.Timer _timer = new();
    private List<ITaskTrigger>? _triggers;
    
    public IGameCapture? GameCapture { get; private set; }
    
    private int _frameIndex = 0;
    private static readonly object _locker = new();
    
    private RECT _gameRect = RECT.Empty;
    /// <summary>窗口位于前台焦点</summary>
    private bool _prevGameActive;
    
    private DateTime _prevManualGc = DateTime.MinValue;
    
    private static readonly object _triggerListLocker = new();
    
    public event EventHandler? UiTaskStopTickEvent;

    public event EventHandler? UiTaskStartTickEvent;
    
    public TaskTriggerDispatcher()
    {
        _instance = this;
        _timer.Elapsed += Tick;
    }
    
    public static TaskTriggerDispatcher Instance()
    {
        if (_instance == null)
        {
            throw new Exception("请先在启动页启动BetterGI，如果已经启动请重启");
        }

        return _instance;
    }
    
    public static IGameCapture GlobalGameCapture
    {
        get
        {
            _instance = Instance();

            if (_instance.GameCapture == null)
            {
                throw new Exception("截图器未初始化!");
            }

            return _instance.GameCapture;
        }
    }

    public void Start(IntPtr hWnd, CaptureModes mode, int interval = 50)
    {
        // 初始化截图器
        GameCapture = GameCaptureFactory.Create(mode);
        // 激活窗口 保证后面能够正常获取窗口信息
        SystemControl.ActivateWindow(hWnd);
        
        // 初始化任务上下文(一定要在初始化触发器前完成)
        var captureAreaHandle = SystemControl.FindCaptureAreaHandle(hWnd);
        TaskContext.Instance().Init(hWnd, captureAreaHandle);
        
        // 初始化触发器(一定要在任务上下文初始化完毕后使用)
        _triggers = GameTaskManager.LoadInitialTriggers();
        
        // 启动截图
        GameCapture.Start(hWnd);
        
        // 启动定时器
        _timer.Interval = interval;
        if (!_timer.Enabled)
        {
            _timer.Start();
        }
    }

    public void Stop()
    {
        _timer.Stop();
        GameCapture?.Stop();
        _gameRect = RECT.Empty;
        _prevGameActive = false;
    }
    
    public void Dispose()
    {
        Stop();
    }

    public void Tick(object? sender, EventArgs e)
    {
        var hasLock = false;
        try
        {
            Monitor.TryEnter(_locker, ref hasLock);
            if (!hasLock)
            {
                // 避免多个定时器回调同时执行，丢帧处理，防止多线程重入问题
                return;
            }
            
            // 检查截图器是否初始化
            var maskWindow = MaskWindow.Instance();
            if (GameCapture == null || !GameCapture.IsCapturing)
            {
                if (!TaskContext.Instance().SystemInfo.GameProcess.HasExited)
                {
                    _logger.LogError($"截图器未初始化!");
                }
                else
                {
                    _logger.LogInformation("游戏已退出，BetterIDV 自动停止截图器");
                }

                UiTaskStopTickEvent?.Invoke(sender, e);
                maskWindow.Invoke(maskWindow.Hide);
                return;
            }

            // 检查游戏是否在前台
            bool hasBackgroundTriggerToRun = false;
            bool active = SystemControl.IsGameActive();
            if (!active)
            {
                // 检查游戏是否已结束
                if (TaskContext.Instance().SystemInfo.GameProcess.HasExited)
                {
                    _logger.LogInformation("游戏已退出，BetterIDV 自动停止截图器");
                    UiTaskStopTickEvent?.Invoke(sender, e);
                    return;
                }

                if (_prevGameActive)
                {
                    Debug.WriteLine("游戏窗口不在前台, 不再进行截屏");
                }
                
                var pName = SystemControl.GetActiveProcessName();
                if (pName != "dwrg" && pName != "MuMuNxDevice")
                {
                    maskWindow.Invoke(() => { maskWindow.Hide(); });
                }

                _prevGameActive = active;
                
                if (_triggers != null)
                {
                    lock (_triggerListLocker)
                    {
                        var exclusive = _triggers.FirstOrDefault(t => t is { IsEnabled: true, IsExclusive: true });
                        if (exclusive != null)
                        {
                            hasBackgroundTriggerToRun = exclusive.IsBackgroundRunning;
                        }
                        else
                        {
                            hasBackgroundTriggerToRun = _triggers.Any(t => t is { IsEnabled: true, IsBackgroundRunning: true });
                        }
                    }
                }

                if (!hasBackgroundTriggerToRun)
                {
                    // 没有后台运行的触发器，这次不再进行截图
                    return;
                }
            }
            else
            {
                // 重新显示遮罩窗口
                maskWindow.Invoke(() =>
                {
                    if (maskWindow.IsExist())
                    {
                        maskWindow.Show();
                    }
                });

                _prevGameActive = active;
                // 移动游戏窗口的时候同步遮罩窗口的位置,此时不进行捕获
                if (SyncMaskWindowPosition())
                {
                    return;
                }

            }

            if (_triggers == null || !_triggers.Exists(t => t.IsEnabled))
            {
                // 判断是否有有效且启用触发器
                return;
            }
            
            // 帧序号自增 1分钟后归零(MaxFrameIndexSecond)
            _frameIndex = (_frameIndex + 1) % (int)(CaptureContent.MaxFrameIndexSecond * 1000d / _timer.Interval);
            
            // 捕获游戏画面
            var bitmap = GameCapture.Capture();
            if (bitmap == null)
            {
                _logger.LogWarning("截图失败!");
                return;
            }
            
            // 循环执行所有触发器
            var content = new CaptureContent(bitmap, _frameIndex, _timer.Interval);

            lock (_triggerListLocker)
            {
                var runningTriggers = _triggers!.Where(t => t.IsEnabled);
                
                foreach (var trigger in runningTriggers)
                {
                    trigger.OnCapture(content);
                }
            }

            content.Dispose();
        }
        finally
        {
            if ((DateTime.Now - _prevManualGc).TotalSeconds > 2)
            {
                GC.Collect();
                _prevManualGc = DateTime.Now;
            }

            if (hasLock)
            {
                Monitor.Exit(_locker);
            }
        }
    }

    /// <summary>
    /// 移动窗口的时候同步遮罩窗口的位置
    /// </summary>
    /// <returns></returns>
    private bool SyncMaskWindowPosition()
    {
        var hWnd = TaskContext.Instance().CaptureAreaHandle;
        var currentRect = SystemControl.GetCaptureRect(hWnd);
        if(_gameRect == RECT.Empty)
        {
            _gameRect = new RECT(currentRect);
        }
        else if(_gameRect != currentRect)
        {
            if ((_gameRect.Width != currentRect.Width || _gameRect.Height != currentRect.Height)
                && !SizeIsZero(_gameRect) && !SizeIsZero(currentRect))
            {
                _logger.LogError("► 游戏窗口大小发生变化 {W}x{H}->{CW}x{CH}, 自动重启截图器中...", _gameRect.Width, _gameRect.Height, currentRect.Width, currentRect.Height);
                UiTaskStopTickEvent?.Invoke(null, EventArgs.Empty);
                UiTaskStartTickEvent?.Invoke(null, EventArgs.Empty);
                _logger.LogInformation("► 游戏窗口大小发生变化，截图器重启完成！");
            }
            
            _gameRect = new RECT(currentRect);
            TaskContext.Instance().SystemInfo.CaptureAreaRect = currentRect;
            MaskWindow.Instance().RefreshPosition();
            return true;
        }

        return false;
    }
    
    private bool SizeIsZero(RECT rect)
    {
        return rect.Width == 0 || rect.Height == 0;
    }
}