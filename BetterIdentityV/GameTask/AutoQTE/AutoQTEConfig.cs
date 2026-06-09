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
    /// 敌人靠近自动松手，防触发恐惧震慑
    /// </summary>
    [ObservableProperty] private bool _preventTerrorShock = false;
    
    /// <summary>
    /// 后台运行
    /// </summary>
    [ObservableProperty] private bool _runBackgroundEnabled = false;
    
    /// <summary>
    /// 击打延迟(毫秒)
    /// </summary>
    [ObservableProperty] private double _systemDelayMs = 25.0;

}