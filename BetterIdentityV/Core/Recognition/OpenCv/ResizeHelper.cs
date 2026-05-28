using OpenCvSharp;

namespace BetterIdentityV.Core.Recognition.OpenCv;

public class ResizeHelper
{
    /// <summary>
    /// 等比例缩放
    /// </summary>
    /// <param name="src">原图</param>
    /// <param name="scale">缩放尺寸倍数</param>
    /// <param name="interpolation">插值方法，可缺省，默认使用双线性插值</param>
    /// <returns>缩放处理后图像</returns>
    public static Mat Resize(Mat src, double scale, InterpolationFlags interpolation = InterpolationFlags.Linear)
    {
        if (Math.Abs(scale - 1) > 0.00001)
        {
            return Resize(src, scale, scale, interpolation);
        }
        return src;
    }

    /// <summary>
    /// 宽高不同比例缩放
    /// </summary>
    /// <param name="src">原图</param>
    /// <param name="widthScale">宽度尺寸缩放倍数</param>
    /// <param name="heightScale">高度尺寸缩放倍数</param>
    /// <param name="interpolation">插值方法，可缺省，默认使用双线性插值</param>
    /// <returns>缩放处理后图像</returns>
    public static Mat Resize(Mat src, double widthScale, double heightScale, InterpolationFlags interpolation = InterpolationFlags.Linear)
    {
        if (Math.Abs(widthScale - 1) > 0.00001 || Math.Abs(heightScale - 1) > 0.00001)
        {
            var dst = new Mat();
            Cv2.Resize(src, dst, new Size(src.Width * widthScale, src.Height * heightScale), 0, 0, interpolation);
            return dst;
        }

        return src;
    }
    
}