namespace BetterIdentityV.Core.Audio;

/// <summary>
/// 音频匹配命中时返回的结果。
/// </summary>
public sealed class AudioMatchResult
{
    /// <summary>
    /// 初始化匹配结果。
    /// </summary>
    /// <param name="patternName">命中的模板名称。</param>
    /// <param name="score">NCC匹配得分。</param>
    /// <param name="threshold">本次匹配使用的阈值。</param>
    /// <param name="timestamp">命中时刻的时间戳。</param>
    public AudioMatchResult(string patternName, double score, double threshold, DateTimeOffset timestamp)
    {
        PatternName = patternName;
        Score = score;
        Threshold = threshold;
        Timestamp = timestamp;
    }

    /// <summary>
    /// 命中的模板名称。
    /// </summary>
    public string PatternName { get; }

    /// <summary>
    /// NCC匹配得分。
    /// </summary>
    public double Score { get; }

    /// <summary>
    /// 匹配使用的阈值。
    /// </summary>
    public double Threshold { get; }

    /// <summary>
    /// 命中时刻的时间戳。
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
