using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterIdentityV.GameTask.AutoQTE;

[Serializable]
public partial class AutoQTEConfig : ObservableObject
{
    /// <summary>
    /// 触发器是否启用
    /// </summary>
    [ObservableProperty] private bool _enabled = true;
    
    /// <summary>
    /// 防恐惧震慑
    /// </summary>
    [ObservableProperty] private bool _preventTerrorShock = false;
    
    /// <summary>
    /// 后台运行
    /// </summary>
    [ObservableProperty] private bool _runBackgroundEnabled = false;

}