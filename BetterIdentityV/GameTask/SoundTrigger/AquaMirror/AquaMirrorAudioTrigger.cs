using BetterIdentityV.Core.Audio;
using BetterIdentityV.GameTask.Common;
using BetterIdentityV.GameTask.SoundTrigger.Blink;
using Microsoft.Extensions.Logging;

namespace BetterIdentityV.GameTask.SoundTrigger.AquaMirror;

public class AquaMirrorAudioTrigger : AudioTaskTriggerBase
{
    private readonly SoundTriggerConfig _config;
    private readonly AquaMirrorAudioTriggerAssets _assets;
    private readonly CooldownService _cooldownService;

    public AquaMirrorAudioTrigger()
    {
        _config = TaskContext.Instance().Config.SoundTriggerConfig;
        _assets = new AquaMirrorAudioTriggerAssets();
        _cooldownService = CooldownService.Instance;
    }

    public override string Name => "水镜触发器";

    public override int Priority => 30;

    public override void OnAudioMatched(AudioMatchResult result)
    {
        Logger.LogInformation("音频匹配命中: {Pattern}, Score={Score:F5}", result.PatternName, result.Score);
        _cooldownService.LastTriggerAbilityTime_AquaMirror = DateTime.UtcNow;
    }
    
    protected override AudioMatchPattern CreatePattern()
    {
        return new AudioMatchPattern
        {
            Name = "水镜",
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

    protected override bool LoadEnabledFromConfig() => _config.AquaMirrorEnabled;
}