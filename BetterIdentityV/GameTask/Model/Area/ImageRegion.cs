using System.Text.RegularExpressions;
using BetterIdentityV.Core.Recognition;
using BetterIdentityV.Core.Recognition.OpenCv;
using BetterIdentityV.GameTask.Common;
using BetterIdentityV.GameTask.Model.Area.Converter;
using BetterIdentityV.View.Drawable;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace BetterIdentityV.GameTask.Model.Area;

public class ImageRegion : Region
{
    private Mat? _cacheGreyMat;
    private Image<Rgb24>? _cacheImage;
    
    public Mat SrcMat { get; }
    
    public Mat CacheGreyMat
    {
        get
        {
            if (_cacheGreyMat != null)
                return _cacheGreyMat;
            _cacheGreyMat = new Mat();
            Cv2.CvtColor(SrcMat, _cacheGreyMat, ColorConversionCodes.BGR2GRAY);
            return _cacheGreyMat;
        }
    }
    
    public unsafe Image<Rgb24> CacheImage
    {
        get
        {
            if (_cacheImage != null)
                return _cacheImage;

            using var mat = SrcMat.CvtColor(ColorConversionCodes.BGR2RGB);
            var bufferSize = (int)SrcMat.Step() * SrcMat.Height;
            using var image = Image.WrapMemory<Rgb24>(mat.DataPointer, bufferSize, mat.Width, mat.Height);
            _cacheImage = image.Clone();

            return _cacheImage;
        }
    }
    
    public ImageRegion(Mat mat, int x, int y, Region? owner = null, INodeConverter? converter = null,
        DrawContent? drawContent = null) : base(x, y, mat.Width, mat.Height, owner, converter, drawContent)
    {
        SrcMat = mat;
    }
    
    /// <summary>
    /// 在本区域内查找最优识别对象
    /// 或者对该区域进行识别
    /// 匹配
    /// RecognitionTypes.TemplateMatch
    /// RecognitionTypes.OcrMatch
    /// 识别
    /// RecognitionTypes.Ocr
    /// </summary>
    /// <param name="ro"></param>
    /// <param name="successAction">成功找到后做什么</param>
    /// <param name="failAction">失败后做什么</param>
    /// <returns>返回最优的一个识别结果RectArea</returns>
    /// <exception cref="Exception"></exception>
    public Region Find(RecognitionObject ro, Action<Region>? successAction = null, Action? failAction = null)
    {
        if (ro == null)
        {
            throw new Exception("识别对象不能为null");
        }

        if (RecognitionTypes.TemplateMatch.Equals(ro.RecognitionType))
        {
            Mat roi;
            Mat? template;
            if (ro.Use3Channels)
            {
                template = ro.TemplateImageMat;
                roi = SrcMat;
                Cv2.CvtColor(roi, roi, ColorConversionCodes.BGRA2BGR);
            }
            else
            {
                template = ro.TemplateImageGreyMat;
                roi = CacheGreyMat;
            }

            if (template == null)
            {
                throw new Exception($"[TemplateMatch]识别对象{ro.Name}的模板图片不能为null");
            }

            if (ro.RegionOfInterest != default)
            {
                // TODO roi 是可以加缓存的
                if (!(0 <= ro.RegionOfInterest.X && 0 <= ro.RegionOfInterest.Width &&
                      ro.RegionOfInterest.X + ro.RegionOfInterest.Width <= roi.Cols
                      && 0 <= ro.RegionOfInterest.Y && 0 <= ro.RegionOfInterest.Height &&
                      ro.RegionOfInterest.Y + ro.RegionOfInterest.Height <= roi.Rows))
                {
                    TaskControl.Logger.LogError("在图像{W1}x{H1}中查找模板,名称：{Name},ROI位置{X2}x{Y2},区域{H2}x{W2},边界溢出！",
                        roi.Width, roi.Height, ro.Name, ro.RegionOfInterest.X, ro.RegionOfInterest.Y,
                        ro.RegionOfInterest.Width, ro.RegionOfInterest.Height);
                }

                roi = new Mat(roi, ro.RegionOfInterest);
            }

            if (ro.BlurSigma > 0)
            {
                // TODO: roi是引用类型，对roi高斯模糊会影响后续判断
                roi = roi.GaussianBlur(new Size(0, 0), ro.BlurSigma);
                template = template.GaussianBlur(new Size(0, 0), ro.BlurSigma);
            }

            var p = MatchTemplateHelper.MatchTemplate(roi, template, ro.TemplateMatchMode, ro.MaskMat, ro.Threshold);
            if (p != new Point())
            {
                var newRa = Derive(p.X + ro.RegionOfInterest.X, p.Y + ro.RegionOfInterest.Y, template.Width,
                    template.Height);
                if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
                {
                    newRa.DrawSelf(ro.Name, ro.DrawOnWindowPen);
                }

                successAction?.Invoke(newRa);
                return newRa;
            }
            else
            {
                if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
                {
                    drawContent.RemoveRect(ro.Name);
                }

                failAction?.Invoke();
                return new Region();
            }
        }
        else
        {
            throw new Exception($"ImageRegion不支持的识别类型{ro.RecognitionType}");
        }
    }
}