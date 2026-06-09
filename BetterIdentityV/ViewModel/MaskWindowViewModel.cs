using System.Collections.ObjectModel;
using System.Windows;
using BetterIdentityV.Core.Config;
using BetterIdentityV.GameTask;
using BetterIdentityV.Helpers;
using BetterIdentityV.Model;
using BetterIdentityV.Service.Interface;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterIdentityV.ViewModel;

public partial class MaskWindowViewModel : ObservableRecipient
{
    [ObservableProperty] private Rect _windowRect;
    
    [ObservableProperty] private ObservableCollection<StatusItem> _statusList = [];
    
    public AllConfig? Config { get; set; }
    
    public MaskWindowViewModel()
    {
        //订阅配置更改消息，重新应用配置
        WeakReferenceMessenger.Default.Register<PropertyChangedMessage<object>>(this, (sender, msg) =>
        {
            if (msg.PropertyName == "RefreshSettings")
            {
                UIDispatcherHelper.Invoke(RefreshSettings);
            }
            else if (msg.PropertyName == "RefreshStatusList")
            {
                UIDispatcherHelper.Invoke(RefreshStatusList);
            }
        });
    }
    
    private void InitializeStatusList()
    {
        InitConfig();
        if (Config != null)
        {
            ClearStatusList();
            var triggers = GameTaskManager.TriggerDictionary;
            StatusList.Add(new StatusItem("\u26a1 校准", Config.AutoQTEConfig, "Enabled", triggers?.GetValueOrDefault("AutoQTE")));
            StatusList.Add(new StatusItem("\uf256 拾取", Config.AutoPickConfig, "Enabled", triggers?.GetValueOrDefault("AutoPick")));
        }
    }

    private void ClearStatusList()
    {
        foreach (var item in StatusList)
        {
            item.Dispose();
        }

        StatusList.Clear();
    }
    
    [RelayCommand]
    private void OnLoaded()
    {
        RefreshSettings();
        RefreshStatusList();
    }
    
    private void RefreshSettings()
    {
        InitConfig();
        if (Config != null)
        {
            OnPropertyChanged(nameof(Config));
        }
    }

    private void RefreshStatusList()
    {
        RefreshSettings();
        InitializeStatusList();
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
