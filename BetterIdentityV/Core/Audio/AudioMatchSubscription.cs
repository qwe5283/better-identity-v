namespace BetterIdentityV.Core.Audio;

public sealed class AudioMatchSubscription : IDisposable
{
    private readonly Action _dispose;
    private bool _disposed;

    internal AudioMatchSubscription(Action dispose)
    {
        _dispose = dispose;
    }

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