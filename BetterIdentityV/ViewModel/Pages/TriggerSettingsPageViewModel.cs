using BetterIdentityV.Core.Config;
using BetterIdentityV.Service.Interface;
using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui;

namespace BetterIdentityV.ViewModel.Pages;

public partial class TriggerSettingsPageViewModel : ViewModel
{
    [ObservableProperty] private List<string> _primaryPickButtonNames;
    [ObservableProperty] private List<string> _secondaryPickButtonNames;

    public AllConfig Config { get; set; }
    
    private readonly INavigationService _navigationService;

    public TriggerSettingsPageViewModel(IConfigService configService, INavigationService navigationService)
    {
        Config = configService.Get();
        _navigationService = navigationService;

        _primaryPickButtonNames = new List<string> { "1", "F1" };
        if (!string.IsNullOrEmpty(Config.AutoPickConfig.PrimaryPickKey) &&
            !_primaryPickButtonNames.Contains(Config.AutoPickConfig.PrimaryPickKey.ToUpper()))
        {
            _primaryPickButtonNames.Insert(0, Config.AutoPickConfig.PrimaryPickKey.ToUpper());
        }
        _secondaryPickButtonNames = new List<string> { "2", "F2" };
        if (!string.IsNullOrEmpty(Config.AutoPickConfig.SecondaryPickKey) &&
            !_secondaryPickButtonNames.Contains(Config.AutoPickConfig.SecondaryPickKey.ToUpper()))
        {
            _secondaryPickButtonNames.Insert(0, Config.AutoPickConfig.SecondaryPickKey.ToUpper());
        }
    }
}