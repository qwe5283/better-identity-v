using System.Windows;
using BetterIdentityV.Core.Config;
using BetterIdentityV.Helpers.Ui;
using BetterIdentityV.Model;
using BetterIdentityV.Service.Interface;
using BetterIdentityV.View.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Wpf.Ui;

namespace BetterIdentityV.ViewModel.Pages;

public partial class CommonSettingsPageViewModel : ViewModel
{
    private readonly INavigationService _navigationService;

    [ObservableProperty] private bool _useAcrylicBackdropProxy;
    
    public AllConfig Config { get; set; }
    
    public CommonSettingsPageViewModel(IConfigService configService, INavigationService navigationService)
    {
        Config = configService.Get();
        UseAcrylicBackdropProxy = Config.CommonConfig.UseAcrylicBackdrop;
        _navigationService = navigationService;
        // 初始化OCR模型选择
        // SelectedPaddleOcrModelConfig = Config.OtherConfig.OcrConfig.PaddleOcrModelConfig;
    }
    
    [RelayCommand]
    public void OnRefreshMaskSettings()
    {
        WeakReferenceMessenger.Default.Send(
            new PropertyChangedMessage<object>(this, "RefreshSettings", new object(), "重新计算控件位置"));
    }
    
    [RelayCommand]
    private void OpenAboutWindow()
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.Owner = Application.Current.MainWindow;
        aboutWindow.ShowDialog();
    }
    
    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        await App.GetService<IUpdateService>()!.CheckUpdateAsync(new UpdateOption
        {
            Trigger = UpdateTrigger.Manual,
            Channel = UpdateChannel.Stable
        });
    }

    partial void OnUseAcrylicBackdropProxyChanged(bool value)
    {
        Config.CommonConfig.UseAcrylicBackdrop = value;
        
        if (value)
        {
            if (Config.CommonConfig.CurrentThemeType.Equals(ThemeType.LightMica))
            {
                Config.CommonConfig.CurrentThemeType = ThemeType.LightAcrylic;
            }
            else if (Config.CommonConfig.CurrentThemeType.Equals(ThemeType.DarkMica))
            {
                Config.CommonConfig.CurrentThemeType = ThemeType.DarkAcrylic;
            }
        }
        else
        {
            if (Config.CommonConfig.CurrentThemeType.Equals(ThemeType.LightAcrylic))
            {
                Config.CommonConfig.CurrentThemeType = ThemeType.LightMica;
            }
            else if (Config.CommonConfig.CurrentThemeType.Equals(ThemeType.DarkAcrylic))
            {
                Config.CommonConfig.CurrentThemeType = ThemeType.DarkMica;
            }
        }
        
        if (Application.Current.MainWindow != null)
        {
            WindowHelper.ApplyThemeToWindow(Application.Current.MainWindow, Config.CommonConfig.CurrentThemeType);
        }
    }

}