namespace BetterIdentityV.Core.Audio;

public class AudioMatchPattern
{
    public string Name { get; set; } = string.Empty;

    public string SamplePath { get; set; } = string.Empty;

    public double Threshold { get; set; } = 0.1;

    public double Ratio { get; set; } = 1.0;

    public bool AllowSuccessiveTrigger { get; set; }

    public TimeSpan Cooldown { get; set; } = TimeSpan.FromMilliseconds(300);

    public int SampleRate { get; set; } = 32000;

    public TimeSpan SampleWindow { get; set; } = TimeSpan.FromMilliseconds(200);

    public float HighPassCutoffHz { get; set; } = 1000;
}