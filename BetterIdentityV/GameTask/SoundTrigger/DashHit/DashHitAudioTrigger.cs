using System.IO;
using BetterIdentityV.Core.Audio;
using BetterIdentityV.Core.Config;
using Microsoft.Extensions.Logging;

namespace BetterIdentityV.GameTask.SoundTrigger.DashHit;

public sealed class DashHitAudioTrigger : AudioTaskTriggerBase
{
    private readonly ILogger<DashHitAudioTrigger> _logger = App.GetLogger<DashHitAudioTrigger>();
    private readonly DashHitAudioTriggerAssets _assets;

    public DashHitAudioTrigger()
    {
        _assets = new DashHitAudioTriggerAssets();
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
            Name = "刹那生灭",
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

        return Global.Absolute(Path.Combine(@"GameTask\SoundTrigger\Assets", _assets.SampleFileName));
    }
}