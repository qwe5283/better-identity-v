namespace BetterIdentityV.AudioCapture;

public class AudioStreamOptions
{
    public int SampleRate { get; set; } = 32000;

    public int Channels { get; set; } = 1;

    public TimeSpan FrameDuration { get; set; } = TimeSpan.FromMilliseconds(200);
}