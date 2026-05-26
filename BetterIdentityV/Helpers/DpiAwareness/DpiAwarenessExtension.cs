using System.Windows;

namespace BetterIdentityV.Helpers.DpiAwareness;

internal static class DpiAwarenessExtension
{
    public static void InitializeDpiAwareness(this Window window)
    {
        _ = new DpiAwarenessController(window);
    }
}