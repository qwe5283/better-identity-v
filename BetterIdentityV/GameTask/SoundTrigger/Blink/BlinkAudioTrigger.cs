using System.IO;
using BetterIdentityV.Core.Audio;
using BetterIdentityV.Core.Config;
using Microsoft.Extensions.Logging;

namespace BetterIdentityV.GameTask.SoundTrigger.Blink;

public class BlinkAudioTrigger : AudioTaskTriggerBase
{
    private readonly ILogger<BlinkAudioTrigger> _logger = App.GetLogger<BlinkAudioTrigger>();
    private readonly SoundTriggerConfig _config;
    private readonly BlinkAudioTriggerAssets _assets;

    public BlinkAudioTrigger()
    {
        _config = TaskContext.Instance().Config.SoundTriggerConfig;
        _assets = new BlinkAudioTriggerAssets();
    }

    public override string Name => "闪现触发器";

    public override int Priority => 30;

    public override void OnAudioMatched(AudioMatchResult result)
    {
        _logger.LogInformation("音频匹配命中: {Pattern}, Score={Score:F5}", result.PatternName, result.Score);
    }
    
    protected override AudioMatchPattern CreatePattern()
    {
        return new AudioMatchPattern
        {
            Name = "闪现",
            SamplePath = ResolveSamplePath(),
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
    
    private string ResolveSamplePath()
    {
        if (Path.IsPathRooted(_assets.SampleFileName))
        {
            return _assets.SampleFileName;
        }

        return Global.Absolute(Path.Combine(@"GameTask\SoundTrigger\Assets", _assets.SampleFileName));
    }
}