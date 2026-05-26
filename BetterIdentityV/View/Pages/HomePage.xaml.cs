using System.Windows.Controls;
using BetterIdentityV.ViewModel.Pages;

namespace BetterIdentityV.View.Pages;

public partial class HomePage : Page
{
    public HomePageViewModel ViewModel { get; }
    
    public HomePage(HomePageViewModel viewModel)
    {
        DataContext = ViewModel = viewModel;
        InitializeComponent();
    }
}