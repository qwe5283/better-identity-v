using Compunet.YoloSharp;
using Microsoft.ML.OnnxRuntime;

namespace BetterIdentityV.Core.Recognition.ONNX;

/// <summary>
/// 移植自 BetterGI，保留延迟创建及 Predictor.Detect 调用方式。
/// YOLO11 的预处理、推理和后处理均由相同版本的 YoloSharp 执行。
/// </summary>
public class BivYoloPredictor : IDisposable
{
    private readonly Lazy<YoloPredictor> _lazyPredictor;
    private readonly SessionOptions _sessionOptions;
    private bool _disposed;

    /// <summary>
    /// 使用 BivOnnxFactory 创建这个类的实例。
    /// </summary>
    protected internal BivYoloPredictor(BivOnnxModel onnxModel, string modelPath, SessionOptions sessionOptions)
    {
        _sessionOptions = sessionOptions;
        _lazyPredictor = new Lazy<YoloPredictor>(() => new YoloPredictor(modelPath,
            new YoloPredictorOptions
            {
                SessionOptions = sessionOptions
            }));
    }

    public YoloPredictor Predictor => _lazyPredictor.Value;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_lazyPredictor.IsValueCreated)
        {
            Predictor.Dispose();
        }

        _sessionOptions.Dispose();
        GC.SuppressFinalize(this);
    }

    ~BivYoloPredictor()
    {
        Dispose();
    }
}
