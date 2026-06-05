using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using BetterIdentityV.Helpers;
using Wpf.Ui.Controls;

namespace BetterIdentityV.View.Windows;

public partial class AboutWindow : FluentWindow
{
    private int _clickCount;
    private DateTime _lastClickTime;
    private const int RequiredClicks = 5;
    private const int MaxIntervalMs = 200;
    
    public AboutWindow()
    {
        InitializeComponent();
        
        RzTextBlock.Inlines.Clear();
        RzTextBlock.Inlines.Add(new Run(Base64Helper.DecodeToString("5Li654ix5Y+R55S18J+agA==")));
        RzTextBlock.Inlines.Add(new LineBreak());
        RzTextBlock.Inlines.Add(new Run(Base64Helper.DecodeToString("5oS/5LiW55WM5a+55q+P5Liq5Lq65rip5p+U5Lul5b6F8J+Yig==")));

        ResizeMode = ResizeMode.NoResize;
        MouseDown += Window_MouseDown;
    }
    
    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var now = DateTime.Now;
        if ((now - _lastClickTime).TotalMilliseconds <= MaxIntervalMs)
        {
            _clickCount++;
        }
        else
        {
            _clickCount = 1;
        }

        _lastClickTime = now;

        if (_clickCount >= RequiredClicks)
        {

            RzTextBlock.Visibility = Visibility.Visible;
            
            _clickCount = 0;
        }
    }
    
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}