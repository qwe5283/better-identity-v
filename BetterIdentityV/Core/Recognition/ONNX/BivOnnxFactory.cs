using Microsoft.ML.OnnxRuntime;

namespace BetterIdentityV.Core.Recognition.ONNX;

public class BivOnnxFactory
{
    public BivYoloPredictor CreateYoloPredictor(BivOnnxModel model)
    {
        return new BivYoloPredictor(model, model.ModalPath, CreateSessionOptions());
    }

    private SessionOptions CreateSessionOptions()
    {
        var sessionOptions = new SessionOptions();
        sessionOptions.AppendExecutionProvider_CPU();
        return sessionOptions;
    }
}
