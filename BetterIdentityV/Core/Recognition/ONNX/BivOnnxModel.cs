using BetterIdentityV.Core.Config;

namespace BetterIdentityV.Core.Recognition.ONNX;

public class BivOnnxModel
{
    public string Name { get; private init; }
    public string ModelRelativePath { get; private init; }
    public string ModalPath => Global.Absolute(ModelRelativePath);

    public BivOnnxModel(string name, string modelRelativePath)
    {
        Name = name;
        ModelRelativePath = modelRelativePath;
    }
}
