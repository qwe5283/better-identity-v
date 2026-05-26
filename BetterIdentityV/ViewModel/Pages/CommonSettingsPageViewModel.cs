using BetterIdentityV.Core.Config;
using BetterIdentityV.Service.Interface;
using Wpf.Ui;

namespace BetterIdentityV.ViewModel.Pages;

public class CommonSettingsPageViewModel : ViewModel
{
    private readonly INavigationService _navigationService;
    
    public AllConfig Config { get; set; }
    
    public CommonSettingsPageViewModel(IConfigService configService, INavigationService navigationService)
    {
        Config = configService.Get();
        _navigationService = navigationService;
        // 初始化OCR模型选择
        // SelectedPaddleOcrModelConfig = Config.OtherConfig.OcrConfig.PaddleOcrModelConfig;
    }
}