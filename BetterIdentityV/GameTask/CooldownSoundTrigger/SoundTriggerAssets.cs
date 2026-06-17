namespace BetterIdentityV.GameTask.CooldownSoundTrigger;

public class SoundTriggerAssets
{
    public string SampleFileName = "刹那生灭波形.wav";
    public double Threshold = 0.1;
    public double Ratio = 1.0;
    public bool AllowSuccessiveTrigger;
    public int CooldownMilliseconds = 300;
    public int SampleRate = 32000;
    public int SampleWindowMilliseconds = 200;
    public float HighPassCutoffHz = 1000;
}