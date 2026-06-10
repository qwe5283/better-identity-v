using System.ComponentModel;

namespace BetterIdentityV.GameCapture;

public enum CaptureModes
{
    [Description("BitBlt")]
    [DefaultValue(1)]
    BitBlt = 0,

    [Description("DwmGetDxSharedSurface")]
    [DefaultValue(2)]
    DwmGetDxSharedSurface = 2,

    [Description("WindowsGraphicsCapture")]
    [DefaultValue(3)]
    WindowsGraphicsCapture = 1,

    [Description("WindowsGraphicsCapture(HDR)")]
    [DefaultValue(4)]
    WindowsGraphicsCaptureHdr = 3,
    
    [Description("自动(推荐)")]
    [DefaultValue(0)]
    Auto = 4,
}