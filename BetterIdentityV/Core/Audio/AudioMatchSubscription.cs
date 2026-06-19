namespace BetterIdentityV.Core.Audio;

/// <summary>
/// 音频匹配订阅句柄，调用 <see cref="Dispose"/> 可取消注册。
/// </summary>
public sealed class AudioMatchSubscription : IDisposable
{
    private readonly Action _dispose;
    private bool _disposed;

    /// <summary>
    /// 内部构造，由 <see cref="BivAudioMatchService"/> 创建。
    /// </summary>
    /// <param name="dispose">取消注册时执行的回调。</param>
    internal AudioMatchSubscription(Action dispose)
    {
        _dispose = dispose;
    }

    /// <summary>
    /// 取消该音频匹配订阅，停止监听对应模板。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _dispose();
    }
}
