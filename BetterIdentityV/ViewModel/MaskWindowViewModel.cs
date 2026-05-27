using System.Collections.ObjectModel;
using System.Windows;
using BetterIdentityV.Core.Config;
using BetterIdentityV.Model;
using BetterIdentityV.Service.Interface;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetterIdentityV.ViewModel;

public partial class MaskWindowViewModel : ObservableRecipient
{
    [ObservableProperty] private Rect _windowRect;
    
    [ObservableProperty] private ObservableCollection<StatusItem> _statusList = [];
    
    public AllConfig? Config { get; set; }
    
    public MaskWindowViewModel()
    {
        //订阅配置更改消息，重新应用配置
        
    }
    
    private void InitializeStatusList()
    {
        if (Config != null)
        {
            StatusList.Add(new StatusItem("\u26a1 校准", Config.AutoQTEConfig));
        }
    }
    
    [RelayCommand]
    private void OnLoaded()
    {
        RefreshSettings();
        InitializeStatusList();
    }
    
    private void RefreshSettings()
    {
        InitConfig();
        if (Config != null)
        {
            OnPropertyChanged(nameof(Config));
        }
    }
    
    /// <summary>
    /// 这个窗口比较特殊，无法直接使用构造函数依赖注入
    /// </summary>
    private void InitConfig()
    {
        if (Config == null)
        {
            var configService = App.GetService<IConfigService>();
            if (configService != null)
            {
                Config = configService.Get();
            }
        }
    }
}