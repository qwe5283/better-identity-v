using BetterIdentityV.GameTask.Model.Area.Converter;
using BetterIdentityV.View.Drawable;
using OpenCvSharp;

namespace BetterIdentityV.GameTask.Model.Area;

/// <summary>
/// 游戏捕获区域类
/// 主要用于转换到遮罩窗口的坐标
/// </summary>
public class GameCaptureRegion(Mat mat, int initX, int initY, Region? owner = null, INodeConverter? converter = null, DrawContent? drawContent = null) : ImageRegion(mat, initX, initY, owner, converter, drawContent)
{
    
    /// <summary>
    /// 游戏窗口初始截图大于1080P的统一转换到1080P
    /// </summary>
    /// <returns></returns>
    public ImageRegion DeriveTo1080P()
    {
        if (Width <= 1920)
        {
            return this;
        }

        var scale = Width / 1920d;

        var newMat = new Mat();
        Cv2.Resize(SrcMat, newMat, new Size(1920, Height / scale));
        Dispose();
        return new ImageRegion(newMat, 0, 0, this, new ScaleConverter(scale));
    }
}