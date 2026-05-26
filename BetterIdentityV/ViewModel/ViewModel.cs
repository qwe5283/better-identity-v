using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Abstractions.Controls;

namespace BetterIdentityV.ViewModel;

public class ViewModel : ObservableObject, INavigationAware
{
    public Task OnNavigatedToAsync()
    {
        OnNavigatedTo();

        return Task.CompletedTask;
    }
    
    public virtual void OnNavigatedTo() { }

    public Task OnNavigatedFromAsync()
    {
        OnNavigatedFrom();

        return Task.CompletedTask;
    }
    
    public virtual void OnNavigatedFrom() { }
}