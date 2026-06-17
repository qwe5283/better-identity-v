using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using Windows.System;
using BetterIdentityV.Core.Config;
using BetterIdentityV.Helpers;
using BetterIdentityV.Model;
using BetterIdentityV.GameCapture;
using BetterIdentityV.GameTask;
using BetterIdentityV.GameTask.Common;
using BetterIdentityV.Service.Interface;
using BetterIdentityV.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MessageBox = Wpf.Ui.Violeta.Controls.MessageBox;

namespace BetterIdentityV.ViewModel.Pages;

public partial class HomePageViewModel : ViewModel
{
    [ObservableProperty]
    private IEnumerable<EnumItem<CaptureModes>> _modeNames = EnumExtensions.ToEnumItems<CaptureModes>();
    
    [ObservableProperty]
    private string? _selectedMode = CaptureModes.BitBlt.ToString();
    
    [ObservableProperty]
    private bool _taskDispatcherEnabled = false;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartTriggerCommand))]
    private bool _startButtonEnabled = true;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopTriggerCommand))]
    private bool _stopButtonEnabled = true;
    
    public AllConfig Config { get; set; }
    
    private MaskWindow? _maskWindow;
    
    private readonly ILogger<HomePageViewModel> _logger = App.GetLogger<HomePageViewModel>();
    private readonly TaskTriggerDispatcher _taskDispatcher;
    
    // 记录上次使用游戏的句柄
    private IntPtr _hWnd;

    public HomePageViewModel(IConfigService configService, TaskTriggerDispatcher taskTriggerDispatcher)
    {
        _taskDispatcher = taskTriggerDispatcher;
        Config = configService.Get();
    }
    
    private bool CanStartTrigger() => StartButtonEnabled;

    [RelayCommand(CanExecute = nameof(CanStartTrigger))]
    public async Task OnStartTriggerAsync()
    {
        var hWnd = SystemControl.FindGameHandle();
        if (hWnd == IntPtr.Zero)
        {
            await MessageBox.ErrorAsync("未找到第五人格或MuMu模拟器窗口，请先启动游戏！");
            return;
        }

        Start(hWnd);
        AskAdjustGameWindowSize(hWnd);
    }

    private void Start(IntPtr hWnd)
    {
        Debug.WriteLine($"游戏启动句柄{hWnd}");
        lock (this)
        {
            try
            {
                if (Config.TriggerInterval <= 0)
                {
                    MessageBox.Error("触发器触发频率必须大于0");
                    return;
                }

                if (!TaskDispatcherEnabled)
                {
                    _hWnd = hWnd;
                    _taskDispatcher.Start(hWnd, GetCaptureMode(hWnd), Config.TriggerInterval);
                    _taskDispatcher.UiTaskStopTickEvent -= OnUiTaskStopTick;
                    _taskDispatcher.UiTaskStartTickEvent -= OnUiTaskStartTick;
                    _taskDispatcher.UiTaskStopTickEvent += OnUiTaskStopTick;
                    _taskDispatcher.UiTaskStartTickEvent += OnUiTaskStartTick;
                    _maskWindow ??= new MaskWindow(); // 延迟初始化
                    _maskWindow.Show();
                    _maskWindow.Topmost = true;
                    MaskWindow.Instance().RefreshPosition();
                    TaskDispatcherEnabled = true;
                }
            }
            catch (ArgumentException e)
            {
                // 捕获分辨率不得小于800x600异常
                Application.Current.MainWindow?.Activate();
                MessageBox.Error(e.Message);
            }
        }
    }
    
    private CaptureModes GetCaptureMode(IntPtr hWnd)
    {
        var mode = SystemControl.GetAutoCaptureMode(hWnd, Config.CaptureMode.ToCaptureMode());
        _logger.LogDebug($"使用截图模式 {mode}");
        return mode;
    }

    /// <summary>
    /// 捕获测试命令
    /// </summary>
    [RelayCommand]
    private void OnStartCaptureTest()
    {
        var picker = new PickerWindow(true);
        
        if (picker.PickCaptureTarget(new WindowInteropHelper(UIDispatcherHelper.MainWindow).Handle, out var hWnd))
        {
            if (hWnd != IntPtr.Zero)
            {
                var captureWindow = new CaptureTestWindow();
                var mode = Config.CaptureMode.ToCaptureMode();
                mode = SystemControl.GetAutoCaptureMode(hWnd, mode);
                _logger.LogDebug($"使用截图模式 {mode}");
                captureWindow.StartCapture(hWnd, mode);
                captureWindow.Show();
            }
            else
            {
                MessageBox.Error("选择的窗体句柄为空");
            }
        }
    }
    
    [RelayCommand]
    private void OnManualPickWindow()
    {
        var picker = new PickerWindow();
        if (picker.PickCaptureTarget(new WindowInteropHelper(UIDispatcherHelper.MainWindow).Handle, out var hWnd))
        {
            if (hWnd != IntPtr.Zero)
            {
                _hWnd = hWnd;
                Start(hWnd);
                AskAdjustGameWindowSize(hWnd);
            }
            else
            {
                MessageBox.Error("选择的窗体句柄为空！");
            }
        }
    }
    
    [RelayCommand]
    private async Task OpenDisplayAdvancedGraphicsSettingsAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("ms-settings:display-advancedgraphics"));
    }
    
    private bool CanStopTrigger() => StopButtonEnabled;

    [RelayCommand(CanExecute = nameof(CanStopTrigger))]
    private void OnStopTrigger()
    {
        Stop();
    }

    private void Stop()
    {
        lock (this)
        {
            if (TaskDispatcherEnabled)
            {
                _taskDispatcher.Stop();
                if (_maskWindow != null && _maskWindow.IsExist())
                {
                    _maskWindow.Topmost = false;
                    _maskWindow?.Hide();
                }
                else
                {
                    _maskWindow?.Close();
                    _maskWindow = null;
                }
                
                TaskDispatcherEnabled = false;
                TaskContext.Instance().IsInitialized = false;
            }
        }
    }
    
    private void OnUiTaskStopTick(object? sender, EventArgs e)
    {
        UIDispatcherHelper.Invoke(Stop);
    }

    private void OnUiTaskStartTick(object? sender, EventArgs e)
    {
        UIDispatcherHelper.Invoke(() => Start(_hWnd));
    }

    private void AskAdjustGameWindowSize(IntPtr hWnd)
    {
        var info = TaskContext.Instance().SystemInfo;
        if (!info.IsGameRatio16_9 && !info.IsGameFullscreenMode && info.GameProcessName != "MuMuNxDevice")
        {
            int w, h;
            Application.Current.MainWindow?.Activate();
            MessageBoxResult result = MessageBox.Question("自动调整游戏窗口以适应16:9分辨率？");
            if (result.Equals(MessageBoxResult.Yes))
            {
                SystemControl.ActivateWindow(hWnd);
                var size = SystemControl.RestoreWindowGetSize(hWnd);
                if (size.Width * 9 < size.Height * 16)
                {
                    // 缩短height
                    w = size.Width;
                    h = size.Width * 9 / 16;
                }
                else
                {
                    // 缩短width
                    w = size.Height * 16 / 9;
                    h = size.Height;
                }
                SystemControl.ResizeWindowClientRect(hWnd, w, h);
                Stop();
                Start(hWnd);
                TaskControl.Logger.LogInformation("已经帮你自动调整了游戏窗口尺寸并重启截图器");
            }
            else
            {
                SystemControl.ActivateWindow(hWnd);
            }
        }
    }

}