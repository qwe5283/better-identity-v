using System.Windows.Controls;
using BetterIdentityV.ViewModel.Pages;

namespace BetterIdentityV.View.Pages;

public partial class IndicatorSettingsPage : Page
{
    public IndicatorSettingsPage(IndicatorSettingsPageViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}