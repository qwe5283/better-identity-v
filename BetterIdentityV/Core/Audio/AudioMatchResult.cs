namespace BetterIdentityV.Core.Audio;

public sealed class AudioMatchResult
{
    public AudioMatchResult(string patternName, double score, double threshold, DateTimeOffset timestamp)
    {
        PatternName = patternName;
        Score = score;
        Threshold = threshold;
        Timestamp = timestamp;
    }

    public string PatternName { get; }

    public double Score { get; }

    public double Threshold { get; }

    public DateTimeOffset Timestamp { get; }
}