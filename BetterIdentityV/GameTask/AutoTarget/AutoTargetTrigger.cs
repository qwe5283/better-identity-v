using System.Collections.Immutable;
using System.IO;
using System.Windows;
using BetterIdentityV.Core.Diagnostics;
using BetterIdentityV.Core.Geometry;
using BetterIdentityV.Core.Recognition.ONNX;
using BetterIdentityV.Core.Simulator;
using BetterIdentityV.GameCapture;
using BetterIdentityV.GameTask.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using Compunet.YoloSharp;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BetterIdentityV.GameTask.AutoTarget;

/// <summary>
/// 使用独立线程截图方式，在游戏前台运行时检测并跟随目标。
/// </summary>
public class AutoTargetTrigger : ObservableObject, ITaskTrigger, IDisposable
{
    private sealed record Settings(string ModelPath, double DeadzoneRadius, int MouseMoveDistance,
        InferenceDeviceType InferenceDevice, int GpuDevice);

    private readonly ILogger<AutoTargetTrigger> _logger;
    private readonly BivOnnxFactory _onnxFactory;
    private readonly DebugHub _debugHub;
    private readonly object _loopLock = new();
    private Settings _settings = new("", 30d, 20, InferenceDeviceType.GpuDirectMl, 1);
    private Thread? _workerThread;
    private CancellationTokenSource? _workerCts;
    private volatile bool _isEnabled;
    private volatile bool _isHealthy = true;
    private DebugSession? _debugSession;
    private AutoTargetGeometry? _geometry;
    private long _debugEpoch;
    private long _frameId;

    public AutoTargetTrigger(BivOnnxFactory onnxFactory, DebugHub debugHub, ILogger<AutoTargetTrigger> logger)
    {
        _onnxFactory = onnxFactory;
        _debugHub = debugHub;
        _logger = logger;
    }

    public string Name => "弓箭准心吸附";
    public int Priority => 30;
    public bool IsExclusive => false;
    public bool IsBackgroundRunning => false;
    public bool IsHealthy => _isHealthy;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            if (value)
                StartLoop();
            else
                StopLoop();
        }
    }

    public void Init()
    {
        var config = TaskContext.Instance().Config.AutoTargetConfig;
        var deadzone = double.IsFinite(config.DeadzoneRadius) ? Math.Max(0d, config.DeadzoneRadius) : 30d;
        var settings = new Settings(config.ModelPath?.Trim() ?? "", deadzone,
            Math.Max(1, config.MouseMoveDistance), config.InferenceDevice, Math.Max(0, config.GpuDevice));
        lock (_loopLock)
        {
            if (_settings != settings)
                ResetDebugSession();
            Volatile.Write(ref _settings, settings);
        }
        IsEnabled = config.Enabled;
    }

    public void OnCapture(CaptureContent content)
    {
        // 不使用调度器的截图回调，由独立线程持续获取整个游戏画面。
    }

    public void OnResolutionChanged(SystemInfo oldInfo, SystemInfo newInfo)
    {
        // 关闭旧会话，使尺寸变化前已经开始的推理不能重新提交旧结果。
        ResetDebugSession();
    }

    private void StartLoop()
    {
        lock (_loopLock)
        {
            if (!_isEnabled || _workerThread?.IsAlive == true)
                return;

            var capture = TaskTriggerDispatcher.Instance().GameCapture;
            var cts = new CancellationTokenSource();
            _workerCts = cts;
            _workerThread = new Thread(() => WorkerLoop(cts, capture))
            {
                IsBackground = true,
                Name = "AutoTargetWorker"
            };
            _workerThread.Start();
        }
    }

    private void StopLoop()
    {
        Thread? thread;
        lock (_loopLock)
        {
            _workerCts?.Cancel();
            ResetDebugSession();
            thread = _workerThread;
        }

        if (thread is not null && thread.IsAlive && thread != Thread.CurrentThread &&
            !thread.Join(TimeSpan.FromSeconds(1)))
        {
            // 推理不能在中途释放，工作线程退出后自行释放预测器和取消令牌。
            _logger.LogWarning("目标跟随线程正在等待当前推理完成后退出");
        }
    }

    private void WorkerLoop(CancellationTokenSource cts, IGameCapture? capture)
    {
        var token = cts.Token;
        BivYoloPredictor? predictor = null;
        Settings? loadedModelSettings = null;
        var errorLogged = false;

        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (capture is not { IsCapturing: true } || !SystemControl.IsGameActive())
                    {
                        ResetDebugSession();
                        token.WaitHandle.WaitOne(50);
                        continue;
                    }

                    var settings = Volatile.Read(ref _settings);
                    if (predictor is null || loadedModelSettings?.ModelPath != settings.ModelPath ||
                        loadedModelSettings?.InferenceDevice != settings.InferenceDevice ||
                        loadedModelSettings?.GpuDevice != settings.GpuDevice)
                    {
                        ResetDebugSession();
                        predictor?.Dispose();
                        predictor = null;
                        var model = new BivOnnxModel("AutoTarget", settings.ModelPath);
                        if (string.IsNullOrWhiteSpace(settings.ModelPath) || !File.Exists(model.ModalPath))
                            throw new FileNotFoundException("目标跟随 ONNX 模型文件不存在", model.ModalPath);

                        predictor = _onnxFactory.CreateYoloPredictor(model, settings.InferenceDevice, settings.GpuDevice);
                        // 在工作线程完成延迟加载，避免模型加载阻塞设置页。
                        _ = predictor.Predictor;
                        loadedModelSettings = settings;
                        _logger.LogInformation("目标跟随模型已加载: {Path}", model.ModalPath);
                    }

                    long epoch;
                    lock (_loopLock)
                    {
                        if (settings != _settings)
                            continue;
                        epoch = _debugEpoch;
                    }
                    var capturedAt = DateTimeOffset.UtcNow;
                    using var frame = capture.Capture();
                    if (frame is null || frame.Empty())
                    {
                        ResetDebugSession();
                        token.WaitHandle.WaitOne(10);
                        continue;
                    }

                    ProcessFrame(frame, predictor, settings, token, epoch, capturedAt);
                    SetHealthy(true);
                    errorLogged = false;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ResetDebugSession();
                    SetHealthy(false);
                    if (!errorLogged)
                    {
                        _logger.LogError(ex, "目标跟随处理失败，请检查 ONNX 模型与配置");
                        errorLogged = true;
                    }

                    // 尚未提供权重时等待重试，避免持续报错；放入模型后可自动恢复。
                    token.WaitHandle.WaitOne(1000);
                }
            }
        }
        finally
        {
            try
            {
                predictor?.Dispose();
            }
            finally
            {
                lock (_loopLock)
                {
                    ResetDebugSession();
                    _workerThread = null;
                    _workerCts = null;
                    cts.Dispose();
                }

                // 快速关闭再开启时，等待旧线程退出后再启动，避免并发推理或重复移动。
                if (_isEnabled)
                    StartLoop();
            }
        }
    }

    private unsafe void ProcessFrame(Mat frame, BivYoloPredictor predictor, Settings settings,
        CancellationToken token, long epoch, DateTimeOffset capturedAt)
    {
        AutoTargetGeometry geometry;
        DebugSession session;
        lock (_loopLock)
        {
            if (token.IsCancellationRequested || !_isEnabled || epoch != _debugEpoch)
                return;

            var imageSize = new ImageSize(frame.Width, frame.Height);
            // 相同分辨率和半径复用同一几何对象，检测帧更新不会重建区域图层。
            if (_geometry is null || _geometry.ImageSize != imageSize ||
                _geometry.Deadzone.Radius != settings.DeadzoneRadius)
                _geometry = AutoTargetGeometry.Calculate(imageSize, settings.DeadzoneRadius);
            geometry = _geometry;
            session = _debugSession ??= _debugHub.OpenSession(AutoTargetDebugState.Source);
            session.Set(AutoTargetDebugState.Geometry, geometry);
        }

        var conversion = frame.Channels() switch
        {
            4 => ColorConversionCodes.BGRA2RGB,
            3 => ColorConversionCodes.BGR2RGB,
            1 => ColorConversionCodes.GRAY2RGB,
            _ => throw new InvalidOperationException("目标跟随不支持当前截图的颜色通道数")
        };

        using var rgb = frame.CvtColor(conversion);
        using var image = Image.WrapMemory<Rgb24>(rgb.DataPointer,
            checked((int)(rgb.Step() * rgb.Height)), rgb.Width, rgb.Height);
        var result = predictor.Predictor.Detect(image);
        var detections = result.Select((box, index) => new DetectionAnnotation(
            new RectD(box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height),
            box.Name.Id, box.Name.Name, box.Confidence, index == 0)).ToImmutableArray();
        lock (_loopLock)
        {
            // 推理结束后可能已经停用或失焦，避免重新显示刚清除的旧框。
            if (!token.IsCancellationRequested && _isEnabled && epoch == _debugEpoch &&
                ReferenceEquals(session, _debugSession) && SystemControl.IsGameActive())
            {
                // 无目标时仍保留区域定义，只将本帧检测列表替换为空。
                session.Set(AutoTargetDebugState.Frame, new AutoTargetFrameSnapshot(
                    Interlocked.Increment(ref _frameId), capturedAt, geometry, detections));
            }
            else
            {
                // 分辨率或配置变化可能已经建立新会话，旧推理不得清理新会话。
                if (ReferenceEquals(session, _debugSession))
                    ResetDebugSession();
                return;
            }
        }

        if (!result.Any())
            return;

        // 保留 BetterGI 的原始检测结果顺序，每帧直接取第一个对象，不额外排序。
        var target = result[0];
        var targetCenter = new PointD(target.Bounds.X + target.Bounds.Width / 2d,
            target.Bounds.Y + target.Bounds.Height / 2d);
        if (geometry.Deadzone.Contains(targetCenter))
            return;

        var dx = targetCenter.X - geometry.Deadzone.Center.X;
        var dy = targetCenter.Y - geometry.Deadzone.Center.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        // 相对死区中心计算方向，屏幕坐标向右为正、向下为正。
        var moveX = (int)Math.Round(dx / distance * settings.MouseMoveDistance);
        var moveY = (int)Math.Round(dy / distance * settings.MouseMoveDistance);
        if (token.IsCancellationRequested || !_isEnabled || epoch != Interlocked.Read(ref _debugEpoch) ||
            !SystemControl.IsGameActive())
            return;

        Simulation.SendInput.Mouse.MoveMouseBy(moveX, moveY);
    }

    private void ResetDebugSession()
    {
        lock (_loopLock)
        {
            _debugSession?.Dispose();
            _debugSession = null;
            _geometry = null;
            Interlocked.Increment(ref _debugEpoch);
        }
    }

    private void SetHealthy(bool value)
    {
        if (_isHealthy == value)
            return;

        _isHealthy = value;
        // 状态通知异步交给 UI，避免 UI 停止线程并等待退出时发生相互等待。
        _ = Application.Current.Dispatcher.InvokeAsync(() => OnPropertyChanged(nameof(IsHealthy)));
    }

    public void Dispose()
    {
        IsEnabled = false;
        GC.SuppressFinalize(this);
    }
}
