using System.ComponentModel;
using System.Windows;
using BetterIdentityV.Core.Config;
using BetterIdentityV.GameCapture.BitBlt;
using BetterIdentityV.GameTask;
using BetterIdentityV.Helpers;
using BetterIdentityV.Helpers.Ui;
using BetterIdentityV.Helpers.Win32;
using BetterIdentityV.Model;
using BetterIdentityV.Service.Interface;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Markup;

namespace BetterIdentityV.ViewModel;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IConfigService _configService;

    public string Title
    {
        get
        {
            if (RuntimeHelper.IsElevated)
                return $"BetterIDV · 更好的第五人格 · {Global.Version}";
            return $"BetterIDV · 更好的第五人格 · (标准用户) · {Global.Version}";
        }
    }

    [ObservableProperty] private bool _isVisible = true;
    
    [ObservableProperty] private WindowBackdropType _currentBackdropType = WindowBackdropType.Auto;
    
    [ObservableProperty] private SymbolRegular _colorThemeButtonIconSymbol = SymbolRegular.WeatherMoon24;

    public AllConfig Config { get; set; }
    
    public MainWindowViewModel(INavigationService navigationService, IConfigService configService)
    {
        _configService = configService;
        Config = _configService.Get();
        _logger = App.GetLogger<MainWindowViewModel>();
    }

    [RelayCommand]
    private void OnSwitchColorTheme()
    {
        if (!OsVersionHelper.IsWindows11_22523_OrGreater)
        {
            Config.CommonConfig.CurrentThemeType = Config.CommonConfig.CurrentThemeType switch
            {
                ThemeType.DarkNone => ThemeType.LightNone,
                ThemeType.LightNone => ThemeType.DarkNone,
                _ => ThemeType.DarkNone
            };
        }
        else if(Config.CommonConfig.UseAcrylicBackdrop)
        {
            Config.CommonConfig.CurrentThemeType = Config.CommonConfig.CurrentThemeType switch
            {
                ThemeType.DarkAcrylic => ThemeType.LightAcrylic,
                _ => ThemeType.DarkAcrylic
            };
        }
        else
        {
            Config.CommonConfig.CurrentThemeType = Config.CommonConfig.CurrentThemeType switch
            {
                ThemeType.DarkMica => ThemeType.LightMica,
                _ => ThemeType.DarkMica
            };
        }

        ApplyTheme(Config.CommonConfig.CurrentThemeType);
    }

    
    private void ApplyTheme(ThemeType themeType)
    {
        var originalThemeType = themeType;
        
        // 根据主题类型设置应用程序主题（深色/浅色）和背景效果类型（Mica/Acrylic/None）
        if (!OsVersionHelper.IsWindows11_22523_OrGreater)
        {
            // 22523以下版本只支持深浅色切换,修正背景材质为纯色
            if (themeType == ThemeType.DarkMica || themeType == ThemeType.DarkAcrylic)
            {
                themeType = ThemeType.DarkNone;
            }
            else if (themeType == ThemeType.LightMica || themeType == ThemeType.LightAcrylic)
            {
                themeType = ThemeType.LightNone;
            }
        }
        
        // 如果主题类型被修正，更新配置并保存
        if (themeType != originalThemeType)
        {
            Config.CommonConfig.CurrentThemeType = themeType;
            _configService.Save();
            _logger.LogInformation($"主题类型已从 {originalThemeType} 修正为 {themeType}，因为当前系统不支持该主题效果");
        }
        
        switch (themeType)
        {
            case ThemeType.DarkNone:
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                ColorThemeButtonIconSymbol = SymbolRegular.WeatherSunny24;
                CurrentBackdropType = WindowBackdropType.None;
                break;
            case ThemeType.DarkMica:
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                ColorThemeButtonIconSymbol = SymbolRegular.WeatherSunny24;
                CurrentBackdropType = WindowBackdropType.Mica;
                break;
            case ThemeType.DarkAcrylic:
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                ColorThemeButtonIconSymbol = SymbolRegular.WeatherSunny24;
                CurrentBackdropType = WindowBackdropType.Acrylic;
                break;
            case ThemeType.LightNone:
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                ColorThemeButtonIconSymbol = SymbolRegular.WeatherMoon24;
                CurrentBackdropType = WindowBackdropType.None;
                break;
            case ThemeType.LightMica:
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                ColorThemeButtonIconSymbol = SymbolRegular.WeatherMoon24;
                CurrentBackdropType = WindowBackdropType.Mica;
                break;
            case ThemeType.LightAcrylic:
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                ColorThemeButtonIconSymbol = SymbolRegular.WeatherMoon24;
                CurrentBackdropType = WindowBackdropType.Acrylic;
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
        ApplyTheme(Config.CommonConfig.CurrentThemeType);
        
        // 检查更新
        await App.GetService<IUpdateService>()!.CheckUpdateAsync(new UpdateOption());
        
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