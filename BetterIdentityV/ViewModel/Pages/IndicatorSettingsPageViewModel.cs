using BetterIdentityV.Core.Config;
using BetterIdentityV.Service.Interface;
using Wpf.Ui;

namespace BetterIdentityV.ViewModel.Pages;

public class IndicatorSettingsPageViewModel : ViewModel
{
    public AllConfig Config { get; set; }
    
    private readonly INavigationService _navigationService;
    
    public IndicatorSettingsPageViewModel(IConfigService configService, INavigationService navigationService)
    {
        Config = configService.Get();
        _navigationService = navigationService;
    }
}