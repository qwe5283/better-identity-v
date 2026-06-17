using BetterIdentityV.Core.Audio;
using Microsoft.Extensions.Logging;
using System.IO;
using BetterIdentityV.Core.Config;

namespace BetterIdentityV.GameTask.CooldownSoundTrigger;

public sealed class SoundTrigger : AudioTaskTriggerBase
{
    private readonly ILogger<SoundTrigger> _logger = App.GetLogger<SoundTrigger>();
    private readonly SoundTriggerAssets _assets;

    public SoundTrigger()
    {
        _assets = new SoundTriggerAssets();
    }

    public override string Name => "音频触发器";

    public override int Priority => 30;

    public override void OnAudioMatched(AudioMatchResult result)
    {
        _logger.LogInformation("音频匹配命中: {Pattern}, Score={Score:F5}", result.PatternName, result.Score);
    }
    
    protected override AudioMatchPattern CreatePattern()
    {
        return new AudioMatchPattern
        {
            Name = "SoundTrigger",
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

    protected override bool LoadEnabledFromConfig() => true; // _config.Enabled;
    
    private string ResolveSamplePath()
    {
        if (Path.IsPathRooted(_assets.SampleFileName))
        {
            return _assets.SampleFileName;
        }

        return Global.Absolute(Path.Combine(@"GameTask\CooldownSoundTrigger\Assets", _assets.SampleFileName));
    }
}