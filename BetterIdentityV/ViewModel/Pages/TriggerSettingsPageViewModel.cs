using BetterIdentityV.Core.Config;
using BetterIdentityV.Service.Interface;
using Wpf.Ui;

namespace BetterIdentityV.ViewModel.Pages;

public class TriggerSettingsPageViewModel : ViewModel
{
    private bool test;
    public bool Test
    {
        get
        {
            Console.WriteLine(test);
            return test;
        }
        set
        {
            test = value;
            Console.WriteLine(value);
        }
    }

    public AllConfig Config { get; set; }
    
    private readonly INavigationService _navigationService;

    public TriggerSettingsPageViewModel(IConfigService configService, INavigationService navigationService)
    {
        Config = configService.Get();
        _navigationService = navigationService;
    }
}