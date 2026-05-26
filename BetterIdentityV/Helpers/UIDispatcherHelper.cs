using System.Windows;

namespace BetterIdentityV.Helpers;

public static class UIDispatcherHelper
{
    public static Window MainWindow => Application.Current.Dispatcher.Invoke(() => Application.Current.MainWindow) ?? throw new InvalidOperationException();

    public static void Invoke(Action callback, params object[] args)
    {
        _ = Application.Current.Dispatcher.Invoke(callback, args);
    }
}