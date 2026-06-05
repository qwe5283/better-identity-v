using System.IO;
using System.Windows;
using System.Windows.Threading;
using BetterIdentityV.GameTask;
using BetterIdentityV.Service;
using BetterIdentityV.Service.Interface;
using BetterIdentityV.View.Pages;
using BetterIdentityV.ViewModel;
using BetterIdentityV.ViewModel.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.RichTextBox.Abstraction;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;
using Wpf.Ui.Violeta.Controls;

namespace BetterIdentityV;

public partial class App : Application
{
    private static readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureServices(services => // 配置依赖注入容器
            {
                // 提前初始化配置
                var configService = new ConfigService();
                services.AddSingleton<IConfigService>(sp => configService);
                var all = configService.Get();
                
                // 日志文件
                var logFolder = Path.Combine(AppContext.BaseDirectory, "log");
                Directory.CreateDirectory(logFolder);
                var logFile = Path.Combine(logFolder, "better-identity-v.log");
                
                // 配置Serilog
                var richTextBox = new RichTextBoxImpl(); // 用于桥接 Serilog 和遮罩窗口的 RichTextBox 控件
                services.AddSingleton<IRichTextBox>(richTextBox); // 注册 IRichTextBox 服务
                
                var loggerConfiguration = new LoggerConfiguration()
                    .WriteTo.File(logFile,
                        outputTemplate:
                        "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}",
                        rollingInterval: RollingInterval.Day)
                    .WriteTo.Console(outputTemplate: 
                        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning);
                loggerConfiguration.WriteTo.RichTextBox(richTextBox, LogEventLevel.Information,
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
                
                Log.Logger = loggerConfiguration.CreateLogger();
                services.AddLogging(c => c.AddSerilog());
                
                services.AddNavigationViewPageProvider();
                // App Host
                services.AddHostedService<ApplicationHostService>(); // 注册托管服务
                // Page resolver service
                services.AddSingleton<INavigationService, NavigationService>();
                
                // MainWindow
                services.AddSingleton<INavigationWindow, MainWindow>(); // 注册为单例的实现
                services.AddSingleton<MainWindowViewModel>(); // 注册服务
                services.AddSingleton<NotifyIconViewModel>(); // 托盘图标
                
                // Pages
                services.AddSingleton<HomePage>();
                services.AddSingleton<HomePageViewModel>();
                services.AddSingleton<CommonSettingsPage>();
                services.AddSingleton<CommonSettingsPageViewModel>();
                services.AddSingleton<TriggerSettingsPage>();
                services.AddSingleton<TriggerSettingsPageViewModel>();
                services.AddSingleton<IndicatorSettingsPage>();
                services.AddSingleton<IndicatorSettingsPageViewModel>();
                
                // My Services
                services.AddSingleton<TaskTriggerDispatcher>();
            }
        )
        .Build();
    
    public static IServiceProvider ServiceProvider => _host.Services;
    
    public static ILogger<T> GetLogger<T>()
    {
        return _host.Services.GetService<ILogger<T>>()!;
    }
    
    /// <summary>
    /// 获取注册的服务
    /// </summary>
    /// <typeparam name="T">获取服务的类型</typeparam>
    /// <returns>服务的实例或<see langword="null"/>.</returns>
    public static T? GetService<T>() where T : class
    {
        return _host.Services.GetService(typeof(T)) as T;
    }

    /// <summary>
    /// 获取注册的服务
    /// </summary>
    /// <returns>服务的实例或<see langword="null"/>.</returns>
    /// <returns></returns>
    public static object? GetService(Type type)
    {
        return _host.Services.GetService(type);
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        RegisterEvents();
        await _host.StartAsync(); //启动主机，触发托管服务启动流程
    }
    
    private void RegisterEvents()
    {
        //Task线程内未捕获异常处理事件
        TaskScheduler.UnobservedTaskException += TaskSchedulerUnobservedTaskException;
        //UI线程未捕获异常处理事件（UI主线程）
        this.DispatcherUnhandledException += AppDispatcherUnhandledException;
        //非UI线程未捕获异常处理事件(例如自己创建的一个子线程)
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
    }
    
    private static void TaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            HandleException(e.Exception);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        finally
        {
            e.SetObserved();
        }
    }
    
    private static void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            HandleException(e.Exception);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        finally
        {
            e.Handled = true;
        }
    }
    
    private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception exception)
            {
                HandleException(exception);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }
    
    private static void HandleException(Exception e)
    {
        if (e.InnerException != null)
        {
            e = e.InnerException;
        }
        
        ExceptionReport.Show(e);

        // log
        GetLogger<App>().LogDebug(e, "UnHandle Exception");
    }
}