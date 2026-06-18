using BetterIdentityV.GameTask.SoundTrigger;
using Microsoft.Extensions.Logging;

namespace BetterIdentityV.GameTask.Common;

/// <summary>
/// 跟踪、记录、维护冷却计时，使用单例。
/// </summary>
public class CooldownService
{
    private readonly ILogger<CooldownService> _logger = App.GetLogger<CooldownService>();
    private readonly SoundTriggerConfig _config;
        
    public static CooldownService Instance { get; } = new();
    
    public DateTime LastTriggerAbilityTime_Blink;
    public DateTime LastTriggerAbilityTime_DashHit;

    private CooldownService()
    {
        _config = TaskContext.Instance().Config.SoundTriggerConfig;
    }

    public string GetCooldownInfoText()
    {
        String text = String.Empty;
        TimeSpan span;
        if (_config.BlinkEnabled && LastTriggerAbilityTime_Blink != default)
        {
            span = LastTriggerAbilityTime_Blink + TimeSpan.FromSeconds(150) - DateTime.UtcNow;
            text += span.TotalSeconds > 0 ? $"闪现CD: {span.TotalSeconds:F1}\n" : String.Empty;
        }
        if (_config.DashHitEnabled && LastTriggerAbilityTime_DashHit != default)
        {
            span = LastTriggerAbilityTime_DashHit + TimeSpan.FromSeconds(9) - DateTime.UtcNow;
            text += span.TotalSeconds > 0 ? $"飞蝴蝶CD: {span.TotalSeconds:F1}\n" : String.Empty;
        }
        
        return text.Trim();
    }
}