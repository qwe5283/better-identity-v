using System.IO;
using BetterIdentityV.Core.Audio;
using BetterIdentityV.Core.Config;
using BetterIdentityV.GameTask.Common;
using Microsoft.Extensions.Logging;

namespace BetterIdentityV.GameTask.SoundTrigger.Blink;

public class BlinkAudioTrigger : AudioTaskTriggerBase
{
    private readonly SoundTriggerConfig _config;
    private readonly BlinkAudioTriggerAssets _assets;
    private readonly CooldownService _cooldownService;

    public BlinkAudioTrigger()
    {
        _config = TaskContext.Instance().Config.SoundTriggerConfig;
        _assets = new BlinkAudioTriggerAssets();
        _cooldownService = CooldownService.Instance;
    }

    public override string Name => "闪现触发器";

    public override int Priority => 30;

    public override void OnAudioMatched(AudioMatchResult result)
    {
        Logger.LogInformation("音频匹配命中: {Pattern}, Score={Score:F5}", result.PatternName, result.Score);
        _cooldownService.LastTriggerAbilityTime_Blink = DateTime.UtcNow;
    }
    
    protected override AudioMatchPattern CreatePattern()
    {
        return new AudioMatchPattern
        {
            Name = "闪现",
            SamplePath = ResolveSamplePath(_assets.SampleFileName),
            Threshold = _assets.Threshold,
            Ratio = _assets.Ratio,
            AllowSuccessiveTrigger = _assets.AllowSuccessiveTrigger,
            Cooldown = TimeSpan.FromMilliseconds(Math.Max(0, _assets.CooldownMilliseconds)),
            SampleRate = _assets.SampleRate,
            SampleWindow = TimeSpan.FromMilliseconds(Math.Max(50, _assets.SampleWindowMilliseconds)),
            HighPassCutoffHz = _assets.HighPassCutoffHz,
        };
    }

    protected override bool LoadEnabledFromConfig() => _config.BlinkEnabled;
    
}