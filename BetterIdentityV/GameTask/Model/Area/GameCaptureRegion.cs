using System.Drawing;
using BetterIdentityV.GameTask.Model.Area.Converter;
using BetterIdentityV.View.Drawable;
using OpenCvSharp;
using Size = OpenCvSharp.Size;

namespace BetterIdentityV.GameTask.Model.Area;

/// <summary>
/// 游戏捕获区域类
/// 主要用于转换到遮罩窗口的坐标
/// </summary>
public class GameCaptureRegion(Mat mat, int initX, int initY, Region? owner = null, INodeConverter? converter = null, DrawContent? drawContent = null) : ImageRegion(mat, initX, initY, owner, converter, drawContent)
{
    /// <summary>
    /// 在游戏捕获图像的坐标维度进行转换到遮罩窗口的坐标维度
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="pen"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public RectDrawable ConvertToRectDrawable(int x, int y, int w, int h, Pen? pen = null, string? name = null)
    {
        var scale = TaskContext.Instance().DpiScale;
        System.Windows.Rect newRect = new(x / scale, y / scale, w / scale, h / scale);
        return new RectDrawable(newRect, pen, name);
    }
    
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