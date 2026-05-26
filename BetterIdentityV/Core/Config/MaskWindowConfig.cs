using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterIdentityV.Core.Config;

public partial class MaskWindowConfig : ObservableObject
{
    /// <summary>
    /// 显示日志窗口
    /// </summary>
    [ObservableProperty]
    private bool _showLogBox = true;
}