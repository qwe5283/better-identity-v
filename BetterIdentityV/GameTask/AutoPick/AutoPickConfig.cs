using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterIdentityV.GameTask.AutoPick;

public partial class AutoPickConfig : ObservableObject
{
    [ObservableProperty] private bool _enabled = true;
}