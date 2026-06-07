using OpenCvSharp;

namespace BetterIdentityV.GameTask.AutoQTE.Core;

public class QTEDetector : IDisposable
{
    private readonly QTEAssets _assets;
    
    public QTEDetector(QTEAssets assets)
    {
        _assets = assets;
    }

    public void Dispose()
    {
        
    }
}