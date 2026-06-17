namespace BetterIdentityV.Core.Audio;

public interface IAudioMatchListener
{
    AudioMatchSubscription Register(AudioMatchPattern pattern, Action<AudioMatchResult> onMatched);

    AudioMatchSubscription Register(AudioMatchPattern pattern, IAudioMatchHandler handler);
}