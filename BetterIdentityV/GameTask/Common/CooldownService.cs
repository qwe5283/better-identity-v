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

    public DateTime LastTriggerAbilityTime_AquaMirror;
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
        // 水镜持续12秒，冷却15秒
        if (_config.AquaMirrorEnabled && LastTriggerAbilityTime_AquaMirror != default)
        {
            span = LastTriggerAbilityTime_AquaMirror + TimeSpan.FromSeconds(27) - DateTime.UtcNow;
            if (span.TotalSeconds > 15)
            {
                span -= TimeSpan.FromSeconds(15);
                string sec = span.TotalSeconds > 10 ? span.TotalSeconds.ToString("F0") : span.TotalSeconds.ToString("F1");
                text += $"水镜持续中: {sec}\n";
            }
            else if (span.TotalSeconds > 0)
            {
                string sec = span.TotalSeconds > 10 ? span.TotalSeconds.ToString("F0") : span.TotalSeconds.ToString("F1");
                text += $"水镜CD: {sec}\n";
            }
        }
        // 闪现冷却150秒
        if (_config.BlinkEnabled && LastTriggerAbilityTime_Blink != default)
        {
            span = LastTriggerAbilityTime_Blink + TimeSpan.FromSeconds(150) - DateTime.UtcNow;
            string sec = span.TotalSeconds > 10 ? span.TotalSeconds.ToString("F0") : span.TotalSeconds.ToString("F1");
            text += span.TotalSeconds > 0 ? $"闪现CD: {sec}\n" : String.Empty;
        }
        // 刹那生灭冷却9秒
        if (_config.DashHitEnabled && LastTriggerAbilityTime_DashHit != default)
        {
            span = LastTriggerAbilityTime_DashHit + TimeSpan.FromSeconds(9) - DateTime.UtcNow;
            text += span.TotalSeconds > 0 ? $"飞蝴蝶CD: {span.TotalSeconds:F1}\n" : String.Empty;
        }
        
        return text.Trim();
    }
}