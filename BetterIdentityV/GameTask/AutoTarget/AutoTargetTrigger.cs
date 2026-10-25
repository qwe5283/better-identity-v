using System.IO;
using System.Windows;
using BetterIdentityV.Core.Recognition.ONNX;
using BetterIdentityV.Core.Simulator;
using BetterIdentityV.GameCapture;
using BetterIdentityV.GameTask.Model;
using BetterIdentityV.GameTask.Model.Area;
using BetterIdentityV.View.Drawable;
using CommunityToolkit.Mvvm.ComponentModel;
using Compunet.YoloSharp;
using Microsoft.Extensions.DependencyInjection;
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
    private const string DetectionRectKey = "AutoTarget.Detections";
    private const string DeadzoneCircleKey = "AutoTarget.Deadzone";

    private sealed record Settings(string ModelPath, double DeadzoneRadius, int MouseMoveDistance,
        InferenceDeviceType InferenceDevice, int GpuDevice);

    private readonly ILogger<AutoTargetTrigger> _logger = App.GetLogger<AutoTargetTrigger>();
    private readonly BivOnnxFactory _onnxFactory = App.ServiceProvider.GetRequiredService<BivOnnxFactory>();
    private readonly object _loopLock = new();
    private Settings _settings = new("", 30d, 20, InferenceDeviceType.GpuDirectMl, 1);
    private Thread? _workerThread;
    private CancellationTokenSource? _workerCts;
    private volatile bool _isEnabled;
    private volatile bool _isHealthy = true;

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
        Volatile.Write(ref _settings, new Settings(config.ModelPath?.Trim() ?? "", deadzone,
            Math.Max(1, config.MouseMoveDistance), config.InferenceDevice, Math.Max(0, config.GpuDevice)));
        IsEnabled = config.Enabled;
    }

    public void OnCapture(CaptureContent content)
    {
        // 不使用调度器的截图回调，由独立线程持续获取整个游戏画面。
    }

    public void OnResolutionChanged(SystemInfo oldInfo, SystemInfo newInfo)
    {
        // 每轮直接以原始截图的宽高计算中心，不缓存与分辨率有关的坐标。
        ClearDrawings();
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
            ClearDrawings();
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
                        ClearDrawings();
                        token.WaitHandle.WaitOne(50);
                        continue;
                    }

                    var settings = Volatile.Read(ref _settings);
                    if (predictor is null || loadedModelSettings?.ModelPath != settings.ModelPath ||
                        loadedModelSettings?.InferenceDevice != settings.InferenceDevice ||
                        loadedModelSettings?.GpuDevice != settings.GpuDevice)
                    {
                        ClearDrawings();
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

                    using var frame = capture.Capture();
                    if (frame is null || frame.Empty())
                    {
                        ClearDrawings();
                        token.WaitHandle.WaitOne(10);
                        continue;
                    }

                    ProcessFrame(frame, predictor, settings, token);
                    SetHealthy(true);
                    errorLogged = false;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ClearDrawings();
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
                    ClearDrawings();
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
        CancellationToken token)
    {
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
        // _logger.LogInformation("预测完成，结果数" + result.Count);

        // 绘制、死区判定和移动方向共用自动计算的中心，坐标单位为原始截图像素。
        var deadzoneCenter = CalculateDeadzoneCenter(frame.Width, frame.Height);
        var dpiScale = TaskContext.Instance().DpiScale;
        var deadzoneCircle = new CircleDrawable(
            new System.Windows.Point(deadzoneCenter.X / dpiScale, deadzoneCenter.Y / dpiScale),
            settings.DeadzoneRadius / dpiScale, System.Drawing.Pens.Cyan);

        // 检测框属于原始整帧像素坐标，使用现有转换处理遮罩窗口的 DPI 缩放。
        var captureRegion = new GameCaptureRegion(frame, 0, 0);
        var rectangles = result.Select((box, index) => captureRegion.ConvertToRectDrawable(
            box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height,
            index == 0 ? System.Drawing.Pens.LimeGreen : System.Drawing.Pens.Red,
            box.Name.Name)).ToList();
        lock (_loopLock)
        {
            // 推理结束后可能已经停用或失焦，避免重新显示刚清除的旧框。
            if (!token.IsCancellationRequested && _isEnabled && SystemControl.IsGameActive())
            {
                var drawContent = VisionContext.Instance().DrawContent;
                // 无目标时仍显示死区边界，便于观察自动定位并调整半径。
                drawContent.PutOrRemoveCircle(DeadzoneCircleKey, deadzoneCircle);
                drawContent.PutOrRemoveRectList(DetectionRectKey, rectangles);
            }
            else
            {
                ClearDrawings();
            }
        }

        if (!result.Any())
            return;

        // 保留 BetterGI 的原始检测结果顺序，每帧直接取第一个对象，不额外排序。
        var target = result[0];
        var dx = target.Bounds.X + target.Bounds.Width / 2d - deadzoneCenter.X;
        var dy = target.Bounds.Y + target.Bounds.Height / 2d - deadzoneCenter.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance <= settings.DeadzoneRadius)
            return;

        // 相对死区中心计算方向，屏幕坐标向右为正、向下为正。
        var moveX = (int)Math.Round(dx / distance * settings.MouseMoveDistance);
        var moveY = (int)Math.Round(dy / distance * settings.MouseMoveDistance);
        if (token.IsCancellationRequested || !_isEnabled || !SystemControl.IsGameActive())
            return;

        Simulation.SendInput.Mouse.MoveMouseBy(moveX, moveY);
        // _logger.LogInformation("发现目标，移动鼠标{MoveX},{MoveY}", moveX, moveY);
        // Thread.Sleep(20); // 缓解CPU压力
    }

    private static System.Windows.Point CalculateDeadzoneCenter(int frameWidth, int frameHeight)
    {
        // 居中裁出最大 16:9 区域，再将 1280x720 预览中的 (640, 317) 映回整帧。
        // 居中裁剪不改变区域中点；向上的偏移量使用裁剪区域的高度，而非窗口总高度。
        var cropHeight = Math.Min(frameHeight, frameWidth * 9d / 16d);
        return new System.Windows.Point(frameWidth / 2d, frameHeight / 2d - cropHeight * 43d / 720d);
    }

    private void ClearDrawings()
    {
        lock (_loopLock)
        {
            var drawContent = VisionContext.Instance().DrawContent;
            drawContent.PutOrRemoveRectList(DetectionRectKey, null);
            drawContent.PutOrRemoveCircle(DeadzoneCircleKey, null);
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
