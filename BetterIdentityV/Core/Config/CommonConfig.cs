using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterIdentityV.Core.Config;

public enum ThemeType
{
    DarkNone,
    DarkMica,
    DarkAcrylic,
    LightNone,
    LightMica,
    LightAcrylic,
}

[Serializable]
public partial class CommonConfig : ObservableObject
{
    /// <summary>
    /// 退出时最小化至托盘
    /// </summary>
    [ObservableProperty]
    private bool _exitToTray;
    
    /// <summary>
    /// 使用透明亚克力材质作背景
    /// </summary>
    [ObservableProperty] private bool _useAcrylicBackdrop = true;
    
    /// <summary>
    /// 当前色彩主题
    /// </summary>
    [ObservableProperty] private ThemeType _currentThemeType = ThemeType.DarkAcrylic;
}