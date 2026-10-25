using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;

namespace BetterIdentityV.Core.Recognition.ONNX;

public class BivOnnxFactory
{
    private readonly ILogger<BivOnnxFactory> _logger;

    public BivOnnxFactory(ILogger<BivOnnxFactory> logger)
    {
        _logger = logger;
    }

    public BivYoloPredictor CreateYoloPredictor(BivOnnxModel model, InferenceDeviceType inferenceDevice, int gpuDevice)
    {
        return new BivYoloPredictor(model, model.ModalPath, CreateSessionOptions(inferenceDevice, gpuDevice));
    }

    private ProviderType[] GetProviderType(InferenceDeviceType inferenceDeviceType)
    {
        switch (inferenceDeviceType)
        {
            case InferenceDeviceType.Cpu:
                return [ProviderType.Cpu];
            case InferenceDeviceType.GpuDirectMl:
                // 只用dml不加cpu的话在很多场景下性能很差。
                return [ProviderType.Dml, ProviderType.Cpu];
            default:
                throw new InvalidEnumArgumentException("无效的推理设备");
        }
    }

    private SessionOptions CreateSessionOptions(InferenceDeviceType inferenceDevice, int gpuDevice)
    {
        // 沿用 BetterGI 的执行提供程序顺序和会话参数，仅保留 DML/CPU 分支。
        var providerTypes = GetProviderType(inferenceDevice);
        var sessionOptions = new SessionOptions();
        foreach (var type in providerTypes)
            try
            {
                switch (type)
                {
                    case ProviderType.Dml:
                        // DirectML 不支持内存模式优化和并行执行，必须关闭并采用顺序执行。
                        sessionOptions.AppendExecutionProvider_DML(gpuDevice);
                        sessionOptions.EnableMemoryPattern = false;
                        sessionOptions.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
                        _logger.LogInformation("[ONNX]已注册 DirectML，GPU设备编号: {Device}", gpuDevice);
                        break;
                    case ProviderType.Cpu:
                        sessionOptions.AppendExecutionProvider_CPU();
                        break;
                    default:
                        throw new InvalidEnumArgumentException("无效的推理设备");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("无法加载指定的 ONNX provider {Provider}，跳过。请检查推理设备配置是否正确。({Err})",
                    Enum.GetName(type), e.Message);
            }

        return sessionOptions;
    }
}
