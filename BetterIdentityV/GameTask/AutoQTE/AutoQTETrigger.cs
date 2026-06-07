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
    private QTEAssets _assets = new QTEAssets();
    
    private QTEDetector _detector;
    private QTETracker _tracker;
    
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
    // 自定义后台截图循环实现，不需要托管调度器后台截图
    public bool IsExclusive => false;
    public bool IsBackgroundRunning => false;

    public AutoQTETrigger()
    {
        
    }

    public void Init()
    {
        var config = TaskContext.Instance().Config.AutoQTEConfig;
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
        
    }
    
    private void StopLoop()
    {
        
    }

    /// <summary>
    /// 处理主循环
    /// </summary>
    private void WorkerLoop()
    {
        var dispatcher = TaskTriggerDispatcher.Instance();
        using var frame = dispatcher.GameCapture?.Capture();

        
    }
}