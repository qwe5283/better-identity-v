using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterIdentityV.GameTask.SoundTrigger;

public partial class SoundTriggerConfig : ObservableObject
{
    /// <summary>
    /// 显示红蝶-刹那生灭冷却时间
    /// </summary>
    [ObservableProperty] private bool _dashHitEnabled;

    /// <summary>
    /// 显示闪现冷却时间
    /// </summary>
    [ObservableProperty] private bool _blinkEnabled;
}