using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterIdentityV.Core.Config;

/// <summary>
/// 遮罩窗口配置
/// </summary>
[Serializable]
public partial class MaskWindowConfig : ObservableObject
{
    /// <summary>
    /// 方位提示是否启用
    /// </summary>
    [ObservableProperty]
    private bool _directionsEnabled;

    /// <summary>
    /// 是否在遮罩窗口上显示识别结果
    /// </summary>
    [ObservableProperty]
    private bool _displayRecognitionResultsOnMask = false;
    
    /// <summary>
    /// 是否启用遮罩窗口
    /// </summary>
    [ObservableProperty]
    private bool _maskEnabled = true;
    
    /// <summary>
    /// 显示日志窗口
    /// </summary>
    [ObservableProperty]
    private bool _showLogBox = true;
    
    /// <summary>
    /// 显示状态指示
    /// </summary>
    [ObservableProperty]
    private bool _showStatus = true;

    /// <summary>
    /// 显示辅助瞄准线
    /// </summary>
    [ObservableProperty]
    private bool _showAimLine = false;
}