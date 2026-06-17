namespace BetterIdentityV.GameTask.CooldownSoundTrigger;

public class SoundTriggerAssets
{
    public readonly string SampleFileName = "刹那生灭波形.wav";
    public readonly double Threshold = 0.15;
    public readonly double Ratio = 1.0;
    public readonly bool AllowSuccessiveTrigger = false;
    public readonly int CooldownMilliseconds = 300;
    public readonly int SampleRate = 32000;
    public readonly int SampleWindowMilliseconds = 200;
    public readonly float HighPassCutoffHz = 1000;
}