namespace BetterIdentityV.AudioCapture;

public interface IAudioSource : IDisposable
{
    bool IsCapturing { get; }

    event EventHandler<AudioFrame>? FrameCaptured;

    void Start(AudioStreamOptions options);

    void Stop();
}
