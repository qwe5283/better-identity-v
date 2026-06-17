using BetterIdentityV.Core.Audio;

namespace BetterIdentityV.GameTask;

public abstract class AudioTaskTriggerBase : ITaskTrigger, IAudioMatchHandler, IDisposable
{
    private AudioMatchSubscription? _subscription;
    private bool _isEnabled;

    public abstract string Name { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
            {
                return;
            }

            _isEnabled = value;
            ApplySubscription();
        }
    }

    public abstract int Priority { get; }

    public virtual bool IsExclusive => false;

    public virtual bool IsBackgroundRunning => true;

    public void Init()
    {
        Dispose();
        _isEnabled = false;
        IsEnabled = LoadEnabledFromConfig();
    }

    public void OnCapture(CaptureContent content)
    {
    }

    public abstract void OnAudioMatched(AudioMatchResult result);

    public void Dispose()
    {
        _subscription?.Dispose();
        _subscription = null;
    }

    protected abstract AudioMatchPattern CreatePattern();

    protected abstract bool LoadEnabledFromConfig();

    private void ApplySubscription()
    {
        if (!IsEnabled)
        {
            Dispose();
            return;
        }

        _subscription ??= BivAudioMatchService.Instance.Register(CreatePattern(), this);
    }
}