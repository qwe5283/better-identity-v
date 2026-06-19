namespace BetterIdentityV.Core.Audio;

/// <summary>
/// 音频匹配命中处理器接口，实现此接口以响应音频匹配事件。
/// </summary>
public interface IAudioMatchHandler
{
    /// <summary>
    /// 当音频模板匹配命中时调用。
    /// </summary>
    /// <param name="result">匹配结果，包含模板名称、得分和阈值。</param>
    void OnAudioMatched(AudioMatchResult result);
}
