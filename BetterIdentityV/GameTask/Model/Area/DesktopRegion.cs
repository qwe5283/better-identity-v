using BetterIdentityV.GameTask.Model.Area.Converter;
using OpenCvSharp;

namespace BetterIdentityV.GameTask.Model.Area;

/// <summary>
/// 桌面区域类
/// 无缩放的桌面屏幕大小
/// 主要用于点击操作
/// </summary>
public class DesktopRegion : Region
{
    public GameCaptureRegion Derive(Mat captureMat, int x, int y)
    {
        return new GameCaptureRegion(captureMat, x, y, this, new TranslationConverter(x, y));
    }
}