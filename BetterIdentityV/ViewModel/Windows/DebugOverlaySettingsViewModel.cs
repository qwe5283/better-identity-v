using BetterIdentityV.GameTask.AutoQTE;
using BetterIdentityV.GameTask.AutoTarget;
using BetterIdentityV.View.DebugOverlay;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterIdentityV.ViewModel.Windows;

public sealed partial class DebugOverlaySettingsViewModel : ObservableObject
{
    private readonly DebugOverlaySettings _settings;

    [ObservableProperty] private bool _isAutoQteVisible;
    [ObservableProperty] private bool _isAutoTargetVisible;

    public DebugOverlaySettingsViewModel(DebugOverlaySettings settings)
    {
        _settings = settings;
        _isAutoQteVisible = settings.IsVisible(AutoQteDebugState.Source);
        _isAutoTargetVisible = settings.IsVisible(AutoTargetDebugState.Source);
    }

    partial void OnIsAutoQteVisibleChanged(bool value) =>
        _settings.SetVisible(AutoQteDebugState.Source, value);

    partial void OnIsAutoTargetVisibleChanged(bool value) =>
        _settings.SetVisible(AutoTargetDebugState.Source, value);
}
