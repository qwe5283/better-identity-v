namespace BetterIdentityV.Core.Audio;

public interface IAudioMatchHandler
{
    void OnAudioMatched(AudioMatchResult result);
}