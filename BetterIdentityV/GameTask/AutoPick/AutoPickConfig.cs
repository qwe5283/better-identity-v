using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterIdentityV.GameTask.AutoPick;

public partial class AutoPickConfig : ObservableObject
{
    /// <summary>
    /// 触发器是否启用
    /// </summary>
    [ObservableProperty] private bool _enabled = true;

    /// <summary>
    /// 自动拾取按键触发最小时间间隔，防止连续触发
    /// </summary>
    [ObservableProperty] private double _cooldownTime = 3.0;
    
    /// <summary>
    /// 拾取到主道具槽的按键
    /// </summary>
    [ObservableProperty] private string _primaryPickKey = "1";
    
    /// <summary>
    /// 拾取到副道具槽的按键
    /// </summary>
    [ObservableProperty] private string _secondaryPickKey = "2";
}