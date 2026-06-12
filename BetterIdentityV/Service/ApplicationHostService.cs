using System.Windows;
using BetterIdentityV.View;
using BetterIdentityV.View.Pages;
using Microsoft.Extensions.Hosting;
using Wpf.Ui;

namespace BetterIdentityV.Service;

public class ApplicationHostService(IServiceProvider serviceProvider) : IHostedService
{
    private INavigationWindow? _navigationWindow;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await HandleActivationAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    private async Task HandleActivationAsync()
    {
        await Task.CompletedTask;

        if (!Application.Current.Windows.OfType<MainWindow>().Any())
        {
            // 从容器获取主窗口实例
            _navigationWindow = (serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow)!;
            _navigationWindow!.ShowWindow();
            // 跳转到主页
            _ = _navigationWindow.Navigate(typeof(HomePage));
        }

        await Task.CompletedTask;
    }
}