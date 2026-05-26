using OpenCvSharp;

namespace BetterIdentityV.GameCapture;

public interface IGameCapture : IDisposable
{
    public bool IsCapturing { get; }

    public void Start(nint hWnd, Dictionary<string, object>? settings = null);

    public Mat? Capture();

    public void Stop();
}