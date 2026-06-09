using System.ComponentModel;
using System.Windows;
using BetterIdentityV.Core.Config;
using BetterIdentityV.GameCapture.BitBlt;
using BetterIdentityV.GameTask;
using BetterIdentityV.Helpers.Ui;
using BetterIdentityV.Helpers.Win32;
using BetterIdentityV.Service.Interface;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Markup;

namespace BetterIdentityV.ViewModel;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    public string Title => $"BetterIDV · 更好的第五人格 · {Global.Version}";
    
    [ObservableProperty] private bool _isVisible = true;
    
    [ObservableProperty] private WindowBackdropType _currentBackdropType = WindowBackdropType.Auto;
    
    [ObservableProperty] private SymbolRegular _colorThemeButtonIconSymbol = SymbolRegular.WeatherMoon24;

    public AllConfig Config { get; set; }
    
    public MainWindowViewModel(INavigationService navigationService, IConfigService configService)
    {
        _configService = configService;
        Config = _configService.Get();
    }

    [RelayCommand]
    private void OnSwitchColorTheme()
    {
        Config.CommonConfig.CurrentColorTheme = Config.CommonConfig.CurrentColorTheme switch
        {
            ThemeType.DarkNone => ThemeType.LightNone,
            ThemeType.LightNone => ThemeType.DarkNone,
            _ => ThemeType.DarkNone
        };
        
        ApplyTheme(Config.CommonConfig.CurrentColorTheme);
    }

    
    private void ApplyTheme(ThemeType themeType)
    {
        switch (themeType)
        {
            case ThemeType.DarkNone:
                Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);
                CurrentBackdropType = WindowBackdropType.None;
                ColorThemeButtonIconSymbol = SymbolRegular.WeatherSunny24;
                break;
            case ThemeType.LightNone:
                Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Light);
                CurrentBackdropType = WindowBackdropType.None;
                ColorThemeButtonIconSymbol = SymbolRegular.WeatherMoon24;
                break;
        }

        // 立即应用主题到当前窗口
        if (Application.Current.MainWindow != null)
        {
            WindowHelper.ApplyThemeToWindow(Application.Current.MainWindow, themeType);
        }
    }
    
    [RelayCommand]
    private void OnClosing(CancelEventArgs e)
    {
        if (Config.CommonConfig.ExitToTray)
        {
            // 拦截窗口关闭，最小化到后台运行
            e.Cancel = true;
            OnHide();
        }
    }

    [RelayCommand]
    private async Task OnLoaded()
    {
        // 应用上次保存的主题
        ApplyTheme(Config.CommonConfig.CurrentColorTheme);
        
        //  Win11下 BitBlt截图方式不可用，需要关闭窗口优化功能
        if (OsVersionHelper.IsWindows11_OrGreater && TaskContext.Instance().Config.AutoFixWin11BitBlt)
        {
            BitBltRegistryHelper.SetDirectXUserGlobalSettings();
        }
    }
    
    [RelayCommand]
    private void OnHide()
    {
        IsVisible = false;
    }
}