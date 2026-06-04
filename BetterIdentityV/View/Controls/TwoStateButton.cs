using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace BetterIdentityV.View.Controls;

public class TwoStateButton : Button
{
    public TwoStateButton()
    {
        if (TryFindResource(typeof(Button)) is Style style)
        {
            Style = style;
        }

        Loaded += OnLoaded;
        ApplicationThemeManager.Changed += OnThemeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        OnThemeChanged(ApplicationThemeManager.GetAppTheme(), Colors.Transparent);
    }
    
    private void OnThemeChanged(ApplicationTheme currentTheme, Color systemAccent)
    {
        EnableBackground = currentTheme == ApplicationTheme.Dark ? Brushes.DarkGreen : Brushes.GreenYellow;
        DisableBackground = currentTheme == ApplicationTheme.Dark ? Brushes.DarkRed : Brushes.OrangeRed;
        UpdateButton();
    }
    
    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TwoStateButton button)
        {
            button.UpdateButton();
        }
    }

    private static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(TwoStateButton), new PropertyMetadata(false, OnIsCheckedChanged));
    
    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }
    
    private static readonly DependencyProperty EnableContentProperty = DependencyProperty.Register(nameof(EnableContent), typeof(object), typeof(TwoStateButton), new PropertyMetadata("启动"));
    
    public object EnableContent
    {
        get => (string)GetValue(EnableContentProperty);
        set => SetValue(EnableContentProperty, value);
    }
    
    
    private static readonly DependencyProperty EnableIconProperty = DependencyProperty.Register(nameof(EnableIcon), typeof(IconElement), typeof(TwoStateButton), new PropertyMetadata(null));

    public IconElement EnableIcon
    {
        get => (IconElement)GetValue(EnableIconProperty);
        set => SetValue(EnableIconProperty, value);
    }

    private static readonly DependencyProperty EnableCommandProperty = DependencyProperty.Register(nameof(EnableCommand), typeof(ICommand), typeof(TwoStateButton), new PropertyMetadata(null));

    public ICommand EnableCommand
    {
        get => (ICommand)GetValue(EnableCommandProperty);
        set => SetValue(EnableCommandProperty, value);
    }
    
    public static readonly DependencyProperty EnableBackgroundProperty = DependencyProperty.Register(nameof(EnableBackground), typeof(Brush), typeof(TwoStateButton), new PropertyMetadata(null));

    public Brush EnableBackground
    {
        get => (Brush)GetValue(EnableBackgroundProperty);
        set => SetValue(EnableBackgroundProperty, value);
    }
    
    private static readonly DependencyProperty DisableContentProperty = DependencyProperty.Register(nameof(DisableContent), typeof(object), typeof(TwoStateButton), new PropertyMetadata("停止"));

    public object DisableContent
    {
        get => (string)GetValue(DisableContentProperty);
        set => SetValue(DisableContentProperty, value);
    }

    private static readonly DependencyProperty DisableIconProperty = DependencyProperty.Register(nameof(DisableIcon), typeof(IconElement), typeof(TwoStateButton), new PropertyMetadata(null));

    public IconElement DisableIcon
    {
        get => (IconElement)GetValue(DisableIconProperty);
        set => SetValue(DisableIconProperty, value);
    }

    private static readonly DependencyProperty DisableCommandProperty = DependencyProperty.Register(nameof(DisableCommand), typeof(ICommand), typeof(TwoStateButton), new PropertyMetadata(null));

    public ICommand DisableCommand
    {
        get => (ICommand)GetValue(DisableCommandProperty);
        set => SetValue(DisableCommandProperty, value);
    }
    
    public static readonly DependencyProperty DisableBackgroundProperty = DependencyProperty.Register(nameof(DisableBackground), typeof(Brush), typeof(TwoStateButton), new PropertyMetadata(null));

    public Brush DisableBackground
    {
        get => (Brush)GetValue(DisableBackgroundProperty);
        set => SetValue(DisableBackgroundProperty, value);
    }

    
    private void UpdateButton()
    {
        if (IsChecked)
        {
            Command = DisableCommand;
            Content = DisableContent;
            Icon = DisableIcon;
            Background = DisableBackground;
        }
        else
        {
            Command = EnableCommand;
            Content = EnableContent;
            Icon = EnableIcon;
            Background = EnableBackground;
        }
    }
}