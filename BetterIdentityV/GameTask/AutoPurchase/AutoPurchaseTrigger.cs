namespace BetterIdentityV.GameTask.AutoPurchase;

public class AutoPurchaseTrigger : ITaskTrigger
{
    public string Name => "自动购买";
    public bool IsEnabled { get; set; }
    public int Priority => 30;
    public bool IsExclusive => false;
    
    public void Init()
    {
        
    }

    public void OnCapture(CaptureContent content)
    {
        
    }
}