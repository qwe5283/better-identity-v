using BetterIdentityV.GameTask.Model.Area.Converter;
using BetterIdentityV.View.Drawable;
using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BetterIdentityV.GameTask.Model.Area;

public class ImageRegion : Region
{
    private Mat? _cacheGreyMat;
    private Image<Rgb24>? _cacheImage;
    
    public Mat SrcMat { get; }
    
    public ImageRegion(Mat mat, int x, int y, Region? owner = null, INodeConverter? converter = null,
        DrawContent? drawContent = null) : base(x, y, mat.Width, mat.Height, owner, converter, drawContent)
    {
        SrcMat = mat;
    }
}