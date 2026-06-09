using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterIdentityV.GameTask.AutoPurchase;

public partial class AutoPickConfig : ObservableObject
{
    /// <summary>
    /// 触发器是否启用
    /// </summary>
    [ObservableProperty] private bool _enabled = true;
    
}