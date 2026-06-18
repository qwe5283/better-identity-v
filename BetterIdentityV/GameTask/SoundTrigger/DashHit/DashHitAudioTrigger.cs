using System.IO;
using BetterIdentityV.Core.Audio;
using BetterIdentityV.Core.Config;
using BetterIdentityV.GameTask.Common;
using Microsoft.Extensions.Logging;

namespace BetterIdentityV.GameTask.SoundTrigger.DashHit;

public sealed class DashHitAudioTrigger : AudioTaskTriggerBase
{
    private readonly ILogger<DashHitAudioTrigger> _logger = App.GetLogger<DashHitAudioTrigger>();
    private readonly SoundTriggerConfig _config;
    private readonly DashHitAudioTriggerAssets _assets;
    private readonly CooldownService _cooldownService;

    public DashHitAudioTrigger()
    {
        _config = TaskContext.Instance().Config.SoundTriggerConfig;
        _assets = new DashHitAudioTriggerAssets();
        _cooldownService = CooldownService.Instance;
    }

    public override string Name => "刹那生灭触发器";

    public override int Priority => 30;

    public override void OnAudioMatched(AudioMatchResult result)
    {
        _logger.LogInformation("音频匹配命中: {Pattern}, Score={Score:F5}", result.PatternName, result.Score);
        _cooldownService.LastTriggerAbilityTime_DashHit = DateTime.UtcNow;
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

    protected override bool LoadEnabledFromConfig() => _config.DashHitEnabled;
    
    private string ResolveSamplePath()
    {
        if (Path.IsPathRooted(_assets.SampleFileName))
        {
            return _assets.SampleFileName;
        }

        return Global.Absolute(Path.Combine(@"GameTask\SoundTrigger\Assets", _assets.SampleFileName));
    }
}