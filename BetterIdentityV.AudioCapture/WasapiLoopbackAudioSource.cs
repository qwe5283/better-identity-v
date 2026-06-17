using NAudio.Wave;

namespace BetterIdentityV.AudioCapture;

public sealed class WasapiLoopbackAudioSource : IAudioSource
{
    private readonly object _locker = new();
    private WasapiLoopbackCapture? _capture;
    private BufferedWaveProvider? _buffer;
    private WaveFormat? _sourceFormat;
    private AudioStreamOptions _options = new();
    private CancellationTokenSource? _readCts;
    private Task? _readTask;

    public bool IsCapturing { get; private set; }

    public event EventHandler<AudioFrame>? FrameCaptured;

    public void Start(AudioStreamOptions options)
    {
        lock (_locker)
        {
            if (IsCapturing)
            {
                return;
            }

            _options = options;
            _capture = new WasapiLoopbackCapture();
            _sourceFormat = _capture.WaveFormat;
            _buffer = new BufferedWaveProvider(_sourceFormat)
            {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromSeconds(2),
            };
            _capture.DataAvailable += OnDataAvailable;
            _capture.RecordingStopped += OnRecordingStopped;

            _readCts = new CancellationTokenSource();
            _capture.StartRecording();
            IsCapturing = true;
            _readTask = Task.Run(() => ReadLoop(_readCts.Token));
        }
    }

    public void Stop()
    {
        lock (_locker)
        {
            if (!IsCapturing && _capture == null)
            {
                return;
            }

            IsCapturing = false;
            _readCts?.Cancel();
            _capture?.StopRecording();
            _capture?.Dispose();
            _capture = null;
            _buffer = null;
            _sourceFormat = null;
            _readCts?.Dispose();
            _readCts = null;
            _readTask = null;
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        _buffer?.AddSamples(e.Buffer, 0, e.BytesRecorded);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        IsCapturing = false;
    }

    private async Task ReadLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var sourceFormat = _sourceFormat;
            var buffer = _buffer;
            if (sourceFormat == null || buffer == null)
            {
                return;
            }

            var sourceBytesPerSecond = sourceFormat.AverageBytesPerSecond;
            var requestedBytes = Math.Max(sourceFormat.BlockAlign, (int)(sourceBytesPerSecond * _options.FrameDuration.TotalSeconds));
            requestedBytes -= requestedBytes % sourceFormat.BlockAlign;

            if (buffer.BufferedBytes < requestedBytes)
            {
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var bytes = new byte[requestedBytes];
            var read = buffer.Read(bytes, 0, bytes.Length);
            if (read <= 0)
            {
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var samples = AudioSampleConverter.ToMonoFloat(bytes, read, sourceFormat);
            if (sourceFormat.SampleRate != _options.SampleRate)
            {
                samples = AudioSampleConverter.ResampleLinear(samples, sourceFormat.SampleRate, _options.SampleRate);
            }

            FrameCaptured?.Invoke(this, new AudioFrame(samples, _options.SampleRate, 1, DateTimeOffset.Now));
        }
    }
}
