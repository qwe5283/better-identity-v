using System.Windows.Media;
using BetterIdentityV.Core.Config;
using Wpf.Ui.Controls;

namespace BetterIdentityV.Helpers.Ui;

public class WindowHelper
{
    public static void TryApplySystemBackdrop(System.Windows.Window window)
    {
        // TODO: TryApplySystemBackdrop
    }
    
    /// <summary>
    /// 根据主题类型应用主题到指定窗口
    /// </summary>
    /// <param name="window">要应用主题的窗口</param>
    /// <param name="themeType">主题类型</param>
    public static void ApplyThemeToWindow(System.Windows.Window window, ThemeType themeType)
    {
        switch (themeType)
        {
            case ThemeType.DarkNone:
                window.Background = new SolidColorBrush(Color.FromArgb(255, 32, 32, 32));
                WindowBackdrop.ApplyBackdrop(window, WindowBackdropType.None);
                break;

            case ThemeType.LightNone:
                window.Background = new SolidColorBrush(Color.FromArgb(255, 243, 243, 243));
                WindowBackdrop.ApplyBackdrop(window, WindowBackdropType.None);
                break;

            case ThemeType.DarkMica:
                window.Background = new SolidColorBrush(Colors.Transparent);
                WindowBackdrop.ApplyBackdrop(window, WindowBackdropType.Mica);
                break;

            case ThemeType.LightMica:
                window.Background = new SolidColorBrush(Colors.Transparent);
                WindowBackdrop.ApplyBackdrop(window, WindowBackdropType.Mica);
                break;

            case ThemeType.DarkAcrylic:
                window.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
                WindowBackdrop.ApplyBackdrop(window, WindowBackdropType.Acrylic);
                break;

            case ThemeType.LightAcrylic:
                window.Background = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
                WindowBackdrop.ApplyBackdrop(window, WindowBackdropType.Acrylic);
                break;

            default:
                window.Background = new SolidColorBrush(Colors.Transparent);
                WindowBackdrop.ApplyBackdrop(window, WindowBackdropType.Mica);
                break;
        }
    }
}