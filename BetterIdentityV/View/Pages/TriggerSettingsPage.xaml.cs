using System.Windows.Controls;
using BetterIdentityV.ViewModel.Pages;

namespace BetterIdentityV.View.Pages;

public partial class TriggerSettingsPage : Page
{
    public TriggerSettingsPage(TriggerSettingsPageViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}