using System.IO;
using System.Windows.Media.Imaging;
using BetterIdentityV.Core.Geometry;
using BetterIdentityV.GameTask.AutoQTE;
using BetterIdentityV.GameTask.AutoQTE.Core;
using BetterIdentityV.GameTask.AutoTarget;
using BetterIdentityV.View.DebugOverlay;
using OpenCvSharp;

namespace VisualDebugValidation;

internal static partial class Program
{
    private static readonly (ImageSize Size, RectD Crop)[] QteCases =
    [
        (new ImageSize(1920, 1080), new RectD(0, 0, 1920, 1080)),
        (new ImageSize(1366, 768), new RectD(0, 0, 1366, 768)),
        (new ImageSize(1600, 1200), new RectD(0, 300, 1600, 900)),
        (new ImageSize(1024, 768), new RectD(0, 192, 1024, 576)),
        (new ImageSize(1920, 1200), new RectD(0, 120, 1920, 1080)),
        (new ImageSize(2560, 1080), new RectD(320, 0, 1920, 1080)),
        (new ImageSize(2561, 1080), new RectD(320, 0, 1920, 1080)),
        (new ImageSize(3440, 1440), new RectD(440, 0, 2560, 1440)),
        (new ImageSize(1080, 1920), new RectD(0, 1312, 1080, 608))
    ];

    private static void CheckQteDetection()
    {
        var regions = QteRegionGeometry.Default;
        Assert(regions.DetectionBounds == new RectD(619, 552, 696, 528), "QTE ROI differs from app_qte.py");
        Equal(regions.RedBand.InnerRadius, 250, "red band inner radius");
        Equal(regions.YellowBand.InnerRadius, 311, "yellow band inner radius");
        Equal(regions.RedBand.StartAngle, 200, "QTE start angle");
        Equal(regions.RedBand.EndAngle, 340, "QTE end angle");

        using var canonical = CreateQtePattern();
        using var detector = new QTEDetector(new QTEAssets(), regions);
        var baseline = detector.Process(canonical);
        Assert(baseline.RedAngle is >= 211 and <= 213, "synthetic red pointer was not detected at 212 degrees");
        Assert(baseline.YellowSpan is { Start: >= 249 and <= 251, End: >= 255 and <= 257 },
            "synthetic yellow target was not detected at 250-256 degrees");

        foreach (var item in QteCases)
        {
            var geometry = QteGeometry.Calculate(item.Size);
            Assert(geometry.CropBounds == item.Crop, $"unexpected QTE crop for {item.Size}: {geometry.CropBounds}");
            var bottomRight = geometry.NormalizedToCapture.Map(new PointD(1920, 1080));
            Equal(bottomRight.Y, item.Size.Height, "QTE crop must align to the bottom");
            Equal(bottomRight.X, item.Crop.X + item.Crop.Width, "QTE crop right edge");

            // 在人工指定的裁剪区域外填充干扰色，验证归一化确实丢弃了上方/两侧画面。
            using var raw = EmbedPattern(canonical, item.Size, item.Crop);
            using var normalized = QteFrameNormalizer.Normalize(raw, geometry);
            var topPixel = normalized.At<Vec3b>(0, 0);
            Assert(topPixel.Item0 == 0 && topPixel.Item1 == 0 && topPixel.Item2 == 0,
                "normalization leaked pixels outside the expected crop");
            var result = detector.Process(normalized);
            Assert(result.RedAngle is { } red && Math.Abs(red - baseline.RedAngle!.Value) <= 1,
                $"red detection changed after QTE crop for {item.Size}: {result.RedAngle}");
            Assert(result.YellowSpan is { } yellow &&
                   Math.Abs(yellow.Start - baseline.YellowSpan!.Value.Start) <= 1 &&
                   Math.Abs(yellow.End - baseline.YellowSpan.Value.End) <= 1,
                $"yellow detection changed after QTE crop for {item.Size}: {result.YellowSpan}");

            foreach (var dpi in new[] { 1d, 1.25, 1.5, 2 })
            {
                var viewport = new OverlayViewport(item.Size, item.Size.Width / dpi, item.Size.Height / dpi, dpi);
                var mapper = new CoordinateMapper(geometry.NormalizedToCapture, viewport);
                var mapped = mapper.Map(new PointD(619, 552));
                Equal(mapped.X * dpi, item.Crop.X + 619 * item.Crop.Width / 1920, "QTE cropped ROI X");
                Equal(mapped.Y * dpi, item.Crop.Y + 552 * item.Crop.Height / 1080, "QTE cropped ROI Y");
            }
        }

        using var empty = new Mat(1080, 1920, MatType.CV_8UC3, Scalar.All(0));
        var emptyResult = detector.Process(empty);
        Assert(emptyResult.RedAngle is null && emptyResult.YellowSpan is null,
            "blank frame produced QTE observations");
        Console.WriteLine("PASS: QTE reference ROI, bottom/center cropping, real OpenCV detection at 9 resolutions and 100%-200% DPI.");
    }

    private static Mat CreateQtePattern()
    {
        using var hsv = new Mat(1080, 1920, MatType.CV_8UC3, Scalar.All(0));
        FillBand(hsv, 250, 338, 208, 212, new Scalar(4, 220, 220));
        FillBand(hsv, 311, 338, 250, 256, new Scalar(18, 110, 230));
        var bgr = new Mat();
        Cv2.CvtColor(hsv, bgr, ColorConversionCodes.HSV2BGR);
        return bgr;
    }

    private static void FillBand(Mat image, double innerRadius, double outerRadius,
        double startAngle, double endAngle, Scalar color)
    {
        var points = new List<Point>();
        void Add(double radius, double angle)
        {
            var radians = angle * Math.PI / 180;
            points.Add(new Point((int)Math.Round(967 + radius * Math.Cos(radians)),
                (int)Math.Round(900 + radius * Math.Sin(radians))));
        }
        for (var angle = startAngle; angle <= endAngle + 0.001; angle += 0.1)
            Add(outerRadius, angle);
        for (var angle = endAngle; angle >= startAngle - 0.001; angle -= 0.1)
            Add(innerRadius, angle);
        Cv2.FillPoly(image, [points.ToArray()], color);
    }

    private static Mat EmbedPattern(Mat canonical, ImageSize size, RectD crop)
    {
        var raw = new Mat(size.Height, size.Width, MatType.CV_8UC3, new Scalar(255, 0, 255));
        using var destination = new Mat(raw, new Rect((int)crop.X, (int)crop.Y, (int)crop.Width, (int)crop.Height));
        Cv2.Resize(canonical, destination, destination.Size(), 0, 0, InterpolationFlags.Linear);
        return raw;
    }

    private static void CheckQteRendering(string artifactPath)
    {
        Directory.CreateDirectory(artifactPath);
        foreach (var item in QteCases)
        {
            const double dpi = 2;
            using var fixture = new RenderFixture(item.Size, item.Size.Width / dpi, item.Size.Height / dpi);
            using var session = fixture.Hub.OpenSession(AutoQteDebugState.Source);
            var geometry = QteGeometry.Calculate(item.Size);
            session.Set(AutoQteDebugState.Geometry, geometry);
            session.Set(AutoQteDebugState.Frame, new QteFrameSnapshot(1, DateTimeOffset.UtcNow, geometry,
                new QTEDetectionResult(210, new QTEAngleSpan(250, 256)), new QTETrackResult(null, "Wait Speed")));
            fixture.Controller.Refresh();
            var bitmap = fixture.Render(dpi);
            var roiEdge = geometry.NormalizedToCapture.Map(new PointD(619, 800));
            Assert(HasInk(bitmap, roiEdge), $"QTE ROI was not mapped onto the raw capture for {item.Size}");
            var pointer = geometry.NormalizedToCapture.Map(AtAngle(294, 210));
            Assert(HasInk(bitmap, pointer), "QTE live pointer was not rendered");
            var yellow = geometry.NormalizedToCapture.Map(AtAngle(324, 253));
            Assert(HasInk(bitmap, yellow), "QTE observed yellow span was not rendered");
            if (item.Size == new ImageSize(1600, 1200) || item.Size == new ImageSize(2560, 1080))
                fixture.SavePreview(Path.Combine(artifactPath, $"autoqte-{item.Size.Width}x{item.Size.Height}.png"));

            session.Set(AutoQteDebugState.Frame, new QteFrameSnapshot(2, DateTimeOffset.UtcNow,
                geometry, default, new QTETrackResult(null, "No Red")));
            fixture.Controller.Refresh();
            bitmap = fixture.Render(dpi);
            Assert(!HasInk(bitmap, pointer), "missing QTE observation left a stale pointer");
            Assert(!HasInk(bitmap, yellow), "missing QTE observation left a stale yellow span");
            Assert(HasInk(bitmap, roiEdge), "missing QTE observation removed the static ROI");

            fixture.Control.CaptureSize = new ImageSize(item.Size.Width + 100, item.Size.Height);
            fixture.Controller.Refresh();
            Assert(IsEmpty(fixture.Render()), "QTE old-resolution graphics were not hidden");
            fixture.Control.CaptureSize = item.Size;
            fixture.Controller.Refresh();

            using var other = fixture.Hub.OpenSession(AutoTargetDebugState.Source);
            other.Set(AutoTargetDebugState.Geometry, AutoTargetGeometry.Calculate(item.Size, 90));
            fixture.Controller.Refresh();
            session.Dispose();
            fixture.Controller.Refresh();
            Assert(!IsEmpty(fixture.Render()), "closing QTE removed the AutoTarget overlay");
            other.Dispose();
            fixture.Controller.Refresh();
            Assert(IsEmpty(fixture.Render()), "closed QTE/AutoTarget sessions left visible graphics");
        }
        Console.WriteLine("PASS: QTE cropped WPF ROI/annular sectors, live observations, empty frames, resize and coexistence with AutoTarget.");
    }

    private static PointD AtAngle(double radius, double angle)
    {
        var radians = angle * Math.PI / 180;
        return new PointD(967 + radius * Math.Cos(radians), 900 + radius * Math.Sin(radians));
    }

    private static bool HasInk(BitmapSource bitmap, PointD point)
    {
        var centerX = (int)Math.Round(point.X);
        var centerY = (int)Math.Round(point.Y);
        for (var y = Math.Max(0, centerY - 2); y <= Math.Min(bitmap.PixelHeight - 1, centerY + 2); y++)
            for (var x = Math.Max(0, centerX - 2); x <= Math.Min(bitmap.PixelWidth - 1, centerX + 2); x++)
                if (Alpha(bitmap, x, y) > 0)
                    return true;
        return false;
    }
}
