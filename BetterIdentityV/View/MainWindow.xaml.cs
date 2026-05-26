using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BetterIdentityV.ViewModel;
using Microsoft.Extensions.Logging;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;
using Wpf.Ui.Tray.Controls;

namespace BetterIdentityV;

public partial class MainWindow : INavigationWindow
{
    private readonly ILogger<MainWindow> _logger = App.GetLogger<MainWindow>();
    
    public MainWindowViewModel ViewModel { get; }
    
    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService)
    {
        DataContext = ViewModel = viewModel;
        
        InitializeComponent();
        
        navigationService.SetNavigationControl(RootNavigation);
        
        Application.Current.MainWindow = this;
        
        Loaded += (s, e) => Activate();
    }
    
    protected override void OnClosed(EventArgs e)
    {
        _logger.LogDebug("主窗体退出");
        base.OnClosed(e);
        App.GetService<NotifyIconViewModel>()?.Exit();
    }
    
    private void OnNotifyIconLeftDoubleClick(NotifyIcon sender, RoutedEventArgs e)
    {
        App.GetService<NotifyIconViewModel>()?.ShowOrHide(); // 双击托盘图标显示或隐藏主窗体
    }

    public INavigationView GetNavigation()
    {
        return RootNavigation;
    }

    public bool Navigate(Type pageType)
    {
        return RootNavigation.Navigate(pageType);
    }

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }

    public void SetPageService(INavigationViewPageProvider navigationViewPageProvider)
    {
        RootNavigation.SetPageProviderService(navigationViewPageProvider);
    }

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();
}