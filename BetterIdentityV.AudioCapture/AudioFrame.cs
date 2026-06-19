namespace BetterIdentityV.AudioCapture;

public sealed class AudioFrame
{
    public AudioFrame(float[] samples, int sampleRate, int channels, DateTimeOffset timestamp)
    {
        Samples = samples;
        SampleRate = sampleRate;
        Channels = channels;
        Timestamp = timestamp;
    }

    public float[] Samples { get; }

    public int SampleRate { get; }

    public int Channels { get; }

    public DateTimeOffset Timestamp { get; }
}