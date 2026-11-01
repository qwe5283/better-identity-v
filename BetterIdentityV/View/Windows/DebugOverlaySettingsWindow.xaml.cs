using BetterIdentityV.Helpers.Ui;
using BetterIdentityV.View.DebugOverlay;
using BetterIdentityV.ViewModel.Windows;
using Wpf.Ui.Controls;

namespace BetterIdentityV.View.Windows;

public partial class DebugOverlaySettingsWindow : FluentWindow
{
    public DebugOverlaySettingsWindow(DebugOverlaySettings settings)
    {
        DataContext = new DebugOverlaySettingsViewModel(settings);
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowHelper.TryApplySystemBackdrop(this);
    }
}
