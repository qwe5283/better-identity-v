using Microsoft.Extensions.Logging;

namespace BetterIdentityV.GameTask.AutoPick;

public class AutoPickTrigger : ITaskTrigger
{
    private readonly ILogger<AutoPickTrigger> _logger = App.GetLogger<AutoPickTrigger>();
    
    public string Name => "自动拾取";
    public bool IsEnabled { get; set; }
    public int Priority => 30;
    public bool IsExclusive => false;
    
    public AutoPickTrigger()
    {
    }
    
    public void Init()
    {
        var config = TaskContext.Instance().Config.AutoPickConfig;
        IsEnabled = config.Enabled;
    }

    public void OnCapture(CaptureContent content)
    {
        
    }
}