using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterIdentityV.GameTask.AutoTarget;

[Serializable]
public partial class AutoTargetConfig : ObservableObject
{
    /// <summary>
    /// 触发器是否启用，提供模型后再手动开启。
    /// </summary>
    [ObservableProperty] private bool _enabled;

    /// <summary>
    /// ONNX 模型路径，相对路径以程序所在目录为基准，也支持绝对路径。
    /// </summary>
    [ObservableProperty] private string _modelPath = @"GameTask\AutoTarget\Assets\target.onnx";

    /// <summary>
    /// 画面中心圆形死区的半径，单位为原始截图像素。
    /// </summary>
    [ObservableProperty] private double _deadzoneRadius = 30d;

    /// <summary>
    /// 每轮向目标方向发送的相对鼠标移动距离。
    /// </summary>
    [ObservableProperty] private int _mouseMoveDistance = 2;
}
