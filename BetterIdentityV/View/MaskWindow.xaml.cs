using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using BetterIdentityV.Core.Config;
using BetterIdentityV.Core.Recognition.OpenCv;
using BetterIdentityV.GameTask;
using BetterIdentityV.Helpers;
using BetterIdentityV.Helpers.DpiAwareness;
using BetterIdentityV.View.Drawable;
using Microsoft.Extensions.Logging;
using Serilog.Sinks.RichTextBox.Abstraction;
using Vanara.PInvoke;
using FontFamily = System.Windows.Media.FontFamily;

namespace BetterIdentityV.View;

/// <summary>
/// 一个用于覆盖在游戏窗口上的窗口，用于显示识别结果、显示日志、设置区域位置等
/// 请使用 Instance 方法获取单例
/// </summary>
public partial class MaskWindow : Window
{
    private static MaskWindow? _maskWindow;
    
    private static readonly Typeface _typeface;

    private nint _hWnd;
    
    private IRichTextBox? _richTextBox;
    
    private readonly ILogger<MaskWindow> _logger = App.GetLogger<MaskWindow>();

    static MaskWindow()
    {
        // 设置字体
        _typeface = new FontFamily("Microsoft Yahei UI").GetTypefaces().First();
        
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MaskWindow), new FrameworkPropertyMetadata(typeof(MaskWindow)));
    }

    public static MaskWindow Instance()
    {
        if (_maskWindow == null)
        {
            throw new Exception("MaskWindow 未初始化");
        }
        return _maskWindow;
    }
    
    public bool IsExist()
    {
        return _maskWindow != null && PresentationSource.FromVisual(_maskWindow) != null;
    }

    public void RefreshPosition()
    {
        var currentRect = SystemControl.GetCaptureRect(TaskContext.Instance().CaptureAreaHandle);
        
        Invoke(() =>
        {
            double dpiScale = DpiHelper.ScaleY;
            
            Left = currentRect.Left / dpiScale;
            Top = currentRect.Top / dpiScale;
            Width = currentRect.Width / dpiScale;
            Height = currentRect.Height / dpiScale;
        });
    }

    public MaskWindow()
    {
        _maskWindow = this;

        this.SetResourceReference(StyleProperty, typeof(MaskWindow));
        InitializeComponent();
        //this.InitializeDpiAwareness();

        LogTextBox.TextChanged += LogTextBoxTextChanged;
        Loaded += OnLoaded;
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _richTextBox = App.GetService<IRichTextBox>(); // 获取注册的用于桥接Serilog的IRichTextBox实例
        if (_richTextBox != null)
        {
            _richTextBox.RichTextBox = LogTextBox;
        }
        
        RefreshPosition();
        PrintSystemInfo();
    }

    private void PrintSystemInfo()
    {
        _logger.LogInformation("BetterIDV {Version}", Global.Version);
        var systemInfo = TaskContext.Instance().SystemInfo;
        var width = systemInfo.GameScreenSize.Width;
        var height = systemInfo.GameScreenSize.Height;
        var dpiScale = TaskContext.Instance().DpiScale;
        _logger.LogInformation("遮罩窗口已启动，游戏大小{Width}x{Height}，素材缩放{Scale}，DPI缩放{Dpi}",
            width, height, systemInfo.AssetScale.ToString("F"), dpiScale);
        
        if (width * 9 != height * 16)
        {
            _logger.LogError("当前游戏分辨率不是16:9，可能影响功能正常使用！");
        }
    }

    // 在窗口句柄初始化后，窗口子元素布局显示之前调用
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        this.SetLayeredWindow(); // 设置点击穿透
        this.SetChildWindow(); // 设为子窗口
        this.HideFromAltTab(); // 在 Alt+Tab 中隐藏
    }

    private void LogTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        var textRange = new TextRange(LogTextBox.Document.ContentStart, LogTextBox.Document.ContentEnd);
        if (textRange.Text.Length > 10000)
        {
            LogTextBox.Document.Blocks.Clear();
        }

        LogTextBox.ScrollToEnd();
    }
    
    public void Refresh()
    {
        Dispatcher.Invoke(InvalidateVisual);
    }
    
    public void Invoke(Action action)
    {
        Dispatcher.Invoke(action);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        try
        {
            var count = VisionContext.Instance().DrawContent.RectList.Count + VisionContext.Instance().DrawContent.LineList.Count + VisionContext.Instance().DrawContent.TextList.Count;
            if (count == 0)
                return;
            
            // 先有上方判断的原因是，有可能Render的时候，配置还未初始化
            // if (!TaskContext.Instance().Config.MaskWindowConfig.DisplayRecognitionResultsOnMask)
            //     return;
            
            foreach (var kv in VisionContext.Instance().DrawContent.RectList)
            {
                foreach (var drawable in kv.Value)
                {
                    if (!drawable.IsEmpty)
                    {
                        drawingContext.DrawRectangle(Brushes.Transparent,
                            new Pen(new SolidColorBrush(drawable.Pen.Color.ToWindowsColor()), drawable.Pen.Width),
                            drawable.Rect);
                    }
                }
            }
            
            foreach (var kv in VisionContext.Instance().DrawContent.LineList)
            {
                foreach (var drawable in kv.Value)
                {
                    drawingContext.DrawLine(new Pen(new SolidColorBrush(drawable.Pen.Color.ToWindowsColor()), drawable.Pen.Width), drawable.P1, drawable.P2);
                }
            }

            foreach (var kv in VisionContext.Instance().DrawContent.TextList)
            {
                foreach (var drawable in kv.Value)
                {
                    if (!drawable.IsEmpty)
                    {
                        drawingContext.DrawText(new FormattedText(drawable.Text,
                            CultureInfo.GetCultureInfo("zh-cn"),
                            FlowDirection.LeftToRight,
                            new Typeface(_typeface.FontFamily, _typeface.Style, FontWeights.Bold, _typeface.Stretch),
                            12, Brushes.Red, 1), drawable.Point);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        base.OnRender(drawingContext);
    }

    public RichTextBox LogBox => LogTextBox;
}

/// <summary>
/// 遮罩窗口的拓展类，用于设置窗口拓展样式
/// </summary>
file static class MaskWindowExtension
{
    public static void HideFromAltTab(this Window window)
    {
        HideFromAltTab(new WindowInteropHelper(window).Handle);
    }

    public static void HideFromAltTab(nint hWnd)
    {
        int style = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE);

        style |= (int)User32.WindowStylesEx.WS_EX_TOOLWINDOW;
        User32.SetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE, style);
    }

    public static void SetLayeredWindow(this Window window, bool isLayered = true)
    {
        SetLayeredWindow(new WindowInteropHelper(window).Handle, isLayered);
    }

    private static void SetLayeredWindow(nint hWnd, bool isLayered = true)
    {
        int style = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE);

        if (isLayered)
        {
            style |= (int)User32.WindowStylesEx.WS_EX_TRANSPARENT;
            style |= (int)User32.WindowStylesEx.WS_EX_LAYERED;
        }
        else
        {
            style &= ~(int)User32.WindowStylesEx.WS_EX_TRANSPARENT;
            style &= ~(int)User32.WindowStylesEx.WS_EX_LAYERED;
        }

        _ = User32.SetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE, style);
    }

    public static void SetChildWindow(this Window window)
    {
        SetChildWindow(new WindowInteropHelper(window).Handle);
    }

    private static void SetChildWindow(nint hWnd)
    {
        int style = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_STYLE);

        style |= (int)User32.WindowStyles.WS_CHILD;
        _ = User32.SetWindowLong(hWnd, User32.WindowLongFlags.GWL_STYLE, style);
    }
}