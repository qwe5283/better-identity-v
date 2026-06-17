namespace BetterIdentityV.Core.Audio;

/// <summary>
/// 音频匹配监听器接口，提供注册/取消注册音频匹配模板的能力。
/// </summary>
public interface IAudioMatchListener
{
    /// <summary>
    /// 注册一个音频匹配模板，命中时通过 <see cref="Action{AudioMatchResult}"/> 回调通知。
    /// </summary>
    /// <param name="pattern">匹配模板配置。</param>
    /// <param name="onMatched">命中时的回调。</param>
    /// <returns>用于取消注册的订阅句柄。</returns>
    AudioMatchSubscription Register(AudioMatchPattern pattern, Action<AudioMatchResult> onMatched);

    /// <summary>
    /// 注册一个音频匹配模板，命中时通过 <see cref="IAudioMatchHandler"/> 通知。
    /// </summary>
    /// <param name="pattern">匹配模板配置。</param>
    /// <param name="handler">命中时的处理器。</param>
    /// <returns>用于取消注册的订阅句柄。</returns>
    AudioMatchSubscription Register(AudioMatchPattern pattern, IAudioMatchHandler handler);
}
