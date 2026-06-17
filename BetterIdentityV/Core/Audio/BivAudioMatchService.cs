using BetterIdentityV.AudioCapture;
using Microsoft.Extensions.Logging;
using System.IO;

namespace BetterIdentityV.Core.Audio;

public sealed class BivAudioMatchService : IAudioMatchListener, IDisposable
{
    private sealed class MatcherEntry
    {
        public Guid Id { get; init; }

        public required AudioMatchPattern Pattern { get; init; }

        public required Action<AudioMatchResult> Callback { get; init; }

        public required NormalizedCrossCorrelationMatcher Matcher { get; init; }

        public required AudioPreprocessor StreamPreprocessor { get; init; }

        public float[] LastFrame { get; set; } = [];

        public bool PreviousFrameMatched { get; set; }

        public DateTimeOffset LastMatchedAt { get; set; } = DateTimeOffset.MinValue;
    }

    private readonly ILogger<BivAudioMatchService> _logger = App.GetLogger<BivAudioMatchService>();
    private readonly object _locker = new();
    private readonly IAudioSource _audioSource;
    private readonly List<MatcherEntry> _entries = [];
    private AudioStreamOptions? _streamOptions;
    private bool _disposed;

    public BivAudioMatchService() : this(new WasapiLoopbackAudioSource())
    {
    }

    public BivAudioMatchService(IAudioSource audioSource)
    {
        _audioSource = audioSource;
        _audioSource.FrameCaptured += OnFrameCaptured;
    }

    public static BivAudioMatchService Instance { get; } = new();

    public AudioMatchSubscription Register(AudioMatchPattern pattern, Action<AudioMatchResult> onMatched)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(onMatched);

        if (string.IsNullOrWhiteSpace(pattern.Name))
        {
            throw new ArgumentException("音频匹配模板名称不能为空", nameof(pattern));
        }

        if (string.IsNullOrWhiteSpace(pattern.SamplePath) || !File.Exists(pattern.SamplePath))
        {
            throw new FileNotFoundException($"音频匹配模板文件不存在: {pattern.SamplePath}", pattern.SamplePath);
        }

        var sample = WavSampleLoader.LoadMono(pattern.SamplePath, pattern.SampleRate);
        var samplePreprocessor = new AudioPreprocessor(pattern.SampleRate, pattern.HighPassCutoffHz);
        var processedSample = samplePreprocessor.Process(sample);

        var entry = new MatcherEntry
        {
            Id = Guid.NewGuid(),
            Pattern = pattern,
            Callback = onMatched,
            Matcher = new NormalizedCrossCorrelationMatcher(processedSample),
            StreamPreprocessor = new AudioPreprocessor(pattern.SampleRate, pattern.HighPassCutoffHz),
        };

        lock (_locker)
        {
            ThrowIfDisposed();
            EnsureCompatibleOptions(pattern);
            _entries.Add(entry);
            EnsureStarted(pattern);
        }

        _logger.LogInformation("注册音频匹配模板: {Name}", pattern.Name);
        return new AudioMatchSubscription(() => Unregister(entry.Id));
    }

    public AudioMatchSubscription Register(AudioMatchPattern pattern, IAudioMatchHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Register(pattern, handler.OnAudioMatched);
    }

    public void Dispose()
    {
        lock (_locker)
        {
            if (_disposed)
            {
                return;
            }

            _entries.Clear();
            _audioSource.Stop();
            _audioSource.FrameCaptured -= OnFrameCaptured;
            _audioSource.Dispose();
            _disposed = true;
        }
    }

    private void Unregister(Guid id)
    {
        lock (_locker)
        {
            var index = _entries.FindIndex(entry => entry.Id == id);
            if (index < 0)
            {
                return;
            }

            var name = _entries[index].Pattern.Name;
            _entries.RemoveAt(index);
            _logger.LogInformation("取消音频匹配模板: {Name}", name);

            if (_entries.Count == 0)
            {
                _audioSource.Stop();
                _streamOptions = null;
            }
        }
    }

    private void EnsureStarted(AudioMatchPattern pattern)
    {
        if (_audioSource.IsCapturing)
        {
            return;
        }

        _streamOptions = new AudioStreamOptions
        {
            SampleRate = pattern.SampleRate,
            Channels = 1,
            FrameDuration = pattern.SampleWindow,
        };
        _audioSource.Start(_streamOptions);
    }

    private void EnsureCompatibleOptions(AudioMatchPattern pattern)
    {
        if (_streamOptions == null)
        {
            return;
        }

        if (_streamOptions.SampleRate != pattern.SampleRate || _streamOptions.FrameDuration != pattern.SampleWindow)
        {
            throw new InvalidOperationException("当前音频监听器已启动，暂不支持混用不同采样率或采样窗口的音频模板");
        }
    }

    private void OnFrameCaptured(object? sender, AudioFrame frame)
    {
        MatcherEntry[] entries;
        lock (_locker)
        {
            entries = _entries.ToArray();
        }

        foreach (var entry in entries)
        {
            try
            {
                MatchEntry(entry, frame);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "音频匹配模板 {Name} 处理失败", entry.Pattern.Name);
            }
        }
    }

    private static void MatchEntry(MatcherEntry entry, AudioFrame frame)
    {
        var currentFrame = entry.StreamPreprocessor.Process(frame.Samples);
        var combined = new float[entry.LastFrame.Length + currentFrame.Length];
        Array.Copy(entry.LastFrame, 0, combined, 0, entry.LastFrame.Length);
        Array.Copy(currentFrame, 0, combined, entry.LastFrame.Length, currentFrame.Length);

        var score = entry.Matcher.Match(combined) * entry.Pattern.Ratio;
        var matched = score >= entry.Pattern.Threshold;
        var cooldownElapsed = frame.Timestamp - entry.LastMatchedAt >= entry.Pattern.Cooldown;

        if (matched && cooldownElapsed && (entry.Pattern.AllowSuccessiveTrigger || !entry.PreviousFrameMatched))
        {
            entry.LastMatchedAt = frame.Timestamp;
            entry.Callback(new AudioMatchResult(entry.Pattern.Name, score, entry.Pattern.Threshold, frame.Timestamp));
        }

        entry.PreviousFrameMatched = matched;
        entry.LastFrame = currentFrame;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BivAudioMatchService));
        }
    }
}