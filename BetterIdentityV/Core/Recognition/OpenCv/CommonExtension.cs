using Color = System.Windows.Media.Color;

namespace BetterIdentityV.Core.Recognition.OpenCv;

public static class CommonExtension
{
    public static Color ToWindowsColor(this System.Drawing.Color color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }
}