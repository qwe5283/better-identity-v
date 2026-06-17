using BetterIdentityV.AudioCapture;
using Microsoft.Extensions.Logging;
using System.IO;

namespace BetterIdentityV.Core.Audio;

/// <summary>
/// 音频匹配服务，管理多个匹配模板的注册与生命周期，将系统音频帧分发给各模板进行NCC匹配。
/// </summary>
public sealed class BivAudioMatchService : IAudioMatchListener, IDisposable
{
    /// <summary>
    /// 单个匹配模板的内部条目，保存匹配所需的所有状态。
    /// </summary>
    private sealed class MatcherEntry
    {
        /// <summary>唯一标识符。</summary>
        public Guid Id { get; init; }

        /// <summary>匹配模板配置。</summary>
        public required AudioMatchPattern Pattern { get; init; }

        /// <summary>命中时的回调。</summary>
        public required Action<AudioMatchResult> Callback { get; init; }

        /// <summary>NCC匹配器实例。</summary>
        public required NormalizedCrossCorrelationMatcher Matcher { get; init; }

        /// <summary>上一帧的音频样本，用于跨帧拼接匹配。</summary>
        public float[] LastFrame { get; set; } = [];

        /// <summary>上一帧是否匹配成功，用于控制连续帧触发抑制。</summary>
        public bool PreviousFrameMatched { get; set; }

        /// <summary>上一次命中触发的时间戳。</summary>
        public DateTimeOffset LastMatchedAt { get; set; } = DateTimeOffset.MinValue;
    }

    private readonly ILogger<BivAudioMatchService> _logger = App.GetLogger<BivAudioMatchService>();
    private readonly object _locker = new();
    private readonly IAudioSource _audioSource;
    private readonly List<MatcherEntry> _entries = [];
    private AudioStreamOptions? _streamOptions;
    private bool _disposed;

    /// <summary>
    /// 使用默认的WASAPI环回音频源创建服务实例。
    /// </summary>
    public BivAudioMatchService() : this(new WasapiLoopbackAudioSource())
    {
    }

    /// <summary>
    /// 使用指定的音频源创建服务实例。
    /// </summary>
    /// <param name="audioSource">音频捕获源。</param>
    public BivAudioMatchService(IAudioSource audioSource)
    {
        _audioSource = audioSource;
        _audioSource.FrameCaptured += OnFrameCaptured;
    }

    /// <summary>
    /// 全局单例实例。
    /// </summary>
    public static BivAudioMatchService Instance { get; } = new();

    /// <summary>
    /// 注册一个音频匹配模板，命中时通过回调通知。
    /// </summary>
    /// <param name="pattern">匹配模板配置。</param>
    /// <param name="onMatched">命中时的回调。</param>
    /// <returns>用于取消注册的订阅句柄。</returns>
    /// <exception cref="ArgumentNullException">pattern 或 onMatched 为 null。</exception>
    /// <exception cref="ArgumentException">模板名称或样本路径无效。</exception>
    /// <exception cref="FileNotFoundException">样本文件不存在。</exception>
    /// <exception cref="InvalidOperationException">当前已有不同参数的模板在监听。</exception>
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

    /// <summary>
    /// 注册一个音频匹配模板，命中时通过 <see cref="IAudioMatchHandler"/> 通知。
    /// </summary>
    /// <param name="pattern">匹配模板配置。</param>
    /// <param name="handler">命中时的处理器。</param>
    /// <returns>用于取消注册的订阅句柄。</returns>
    public AudioMatchSubscription Register(AudioMatchPattern pattern, IAudioMatchHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Register(pattern, handler.OnAudioMatched);
    }

    /// <summary>
    /// 释放所有资源，停止音频捕获并清理全部注册模板。
    /// </summary>
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

    /// <summary>
    /// 取消指定模板的注册，最后一个模板被移除时自动停止音频捕获。
    /// </summary>
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

    /// <summary>
    /// 如果音频源未启动则启动，确保有音频帧流入。
    /// </summary>
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

    /// <summary>
    /// 检查新注册的模板参数是否与已启动的音频流兼容。
    /// </summary>
    /// <exception cref="InvalidOperationException">当前不支持混用不同采样率或窗口的模板。</exception>
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

    /// <summary>
    /// 音频帧到达时，分发给所有已注册的匹配条目。
    /// </summary>
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

    /// <summary>
    /// 对单个匹配条目执行NCC匹配，包含帧拼接、预处理、阈值判决和触发抑制。
    /// </summary>
    private static void MatchEntry(MatcherEntry entry, AudioFrame frame)
    {
        var currentFrame = frame.Samples.ToArray();
        var combined = new float[entry.LastFrame.Length + currentFrame.Length];
        Array.Copy(entry.LastFrame, 0, combined, 0, entry.LastFrame.Length);
        Array.Copy(currentFrame, 0, combined, entry.LastFrame.Length, currentFrame.Length);

        var streamPreprocessor = new AudioPreprocessor(entry.Pattern.SampleRate, entry.Pattern.HighPassCutoffHz);
        var processedCombined = streamPreprocessor.Process(combined);
        var score = entry.Matcher.Match(processedCombined) * entry.Pattern.Ratio;
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

    /// <summary>
    /// 检查服务是否已释放，已释放则抛出异常。
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BivAudioMatchService));
        }
    }
}
