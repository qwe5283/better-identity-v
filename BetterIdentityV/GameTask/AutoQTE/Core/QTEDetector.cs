using OpenCvSharp;

namespace BetterIdentityV.GameTask.AutoQTE.Core;

public class QTEDetector : IDisposable
{
    public int Width { get; }
    public int Height { get; }

    private readonly QTEAssets _assets;
    private readonly Point _arcCenter;
    private readonly int _radius;
    private readonly int _thickness;
    private readonly int _redInnerRadius;
    private readonly int _minYellowArea;

    private readonly Mat _arcMask;
    private readonly Rect _arcRoi;
    private readonly Mat _arcMaskRoi;
    private readonly Mat _kernelNoise;
    private bool _disposed = false;
    
    public QTEDetector(int width, int height, QTEAssets assets)
    {
        Width = width;
        Height = height;
        _assets = assets;

        _arcCenter = new Point((int)(assets.ArcCenterX * width), (int)(assets.ArcCenterY * height));
        _radius = (int)(assets.Radius * height);
        _thickness = (int)(assets.Thickness * height);
        _redInnerRadius = Math.Max(_radius - (int)(_thickness * assets.RedInnerExtendMult), 1);

        _arcMask = GenerateCircularArcMask();
        _minYellowArea = Math.Max(1, (int)(Cv2.CountNonZero(_arcMask) * 0.025));

        int kSize = Math.Max(3, height / 300);
        if (kSize % 2 == 0) kSize++;
        _kernelNoise = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(kSize, kSize));

        _arcRoi = Cv2.BoundingRect(_arcMask);
        _arcMaskRoi = new Mat(_arcMask, _arcRoi);
    }

    private Mat GenerateCircularArcMask()
    {
        var mask = new Mat(Height, Width, MatType.CV_8UC1, Scalar.Black);
        int innerRadius = Math.Max(_radius - _thickness, 1);

        int numPoints = (int)((_assets.EndAngle - _assets.StartAngle) * 2 + 1);
        var angles = new double[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            angles[i] = (_assets.StartAngle + (double)i / (numPoints - 1) * (_assets.EndAngle - _assets.StartAngle)) * Math.PI / 180.0;
        }

        var outerPoints = new Point[numPoints];
        var innerPoints = new Point[numPoints];

        for (int i = 0; i < numPoints; i++)
        {
            double cosA = Math.Cos(angles[i]);
            double sinA = Math.Sin(angles[i]);
            outerPoints[i] = new Point((int)(_arcCenter.X + _radius * cosA), (int)(_arcCenter.Y + _radius * sinA));
            innerPoints[i] = new Point((int)(_arcCenter.X + innerRadius * cosA), (int)(_arcCenter.Y + innerRadius * sinA));
        }

        var polygon = new Point[numPoints * 2];
        Array.Copy(outerPoints, polygon, numPoints);
        Array.Copy(innerPoints.Reverse().ToArray(), 0, polygon, numPoints, numPoints);

        Cv2.FillPoly(mask, new[] { polygon }, Scalar.White);
        return mask;
    }
    
    public (double? redAngle, (double start, double end)? yellowSpan) ProcessFrame(Mat frame)
    {
        using var frameRoi = new Mat(frame, _arcRoi);
        using var hsvRoi = new Mat();
        Cv2.CvtColor(frameRoi, hsvRoi, ColorConversionCodes.BGR2HSV);

        using var maskYellow = new Mat();
        Cv2.InRange(hsvRoi, _assets.YellowLower, _assets.YellowUpper, maskYellow);
        Cv2.MorphologyEx(maskYellow, maskYellow, MorphTypes.Close, _kernelNoise);
        Cv2.BitwiseAnd(maskYellow, _arcMaskRoi, maskYellow);

        using var maskRed1 = new Mat();
        using var maskRed2 = new Mat();
        using var maskRed = new Mat();
        Cv2.InRange(hsvRoi, _assets.RedLower1, _assets.RedUpper1, maskRed1);
        Cv2.InRange(hsvRoi, _assets.RedLower2, _assets.RedUpper2, maskRed2);
        Cv2.BitwiseOr(maskRed1, maskRed2, maskRed);
        Cv2.MorphologyEx(maskRed, maskRed, MorphTypes.Close, _kernelNoise);

        double? redAngle = GetRedFrontAngle(maskRed);
        var yellowSpan = GetYellowAngleSpan(maskYellow);

        return (redAngle, yellowSpan);
    }
    
    private double? GetRedFrontAngle(Mat maskRed)
    {
        using var pointsMat = new Mat();
        Cv2.FindNonZero(maskRed, pointsMat);
        if (pointsMat.Empty()) return null;

        pointsMat.GetArray(out Point[] points);
        int localCenterX = _arcCenter.X - _arcRoi.X;
        int localCenterY = _arcCenter.Y - _arcRoi.Y;

        int N = _assets.RedRadialSegments;
        bool[,] segmentPresent = new bool[N, 360];
        var validPoints = new List<(Point pt, double angleDeg, double dist)>();

        double radiusDiff = _radius - _redInnerRadius;
        if (radiusDiff <= 0) return null;

        foreach (var pt in points)
        {
            double dx = pt.X - localCenterX;
            double dy = pt.Y - localCenterY;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist < _redInnerRadius || dist > _radius) continue;

            double angleDeg = Math.Atan2(dy, dx) * 180.0 / Math.PI;
            if (angleDeg < 0) angleDeg += 360.0;
            int angleBin = (int)Math.Floor(angleDeg);
            if (angleBin < 0 || angleBin >= 360) continue;

            int segIdx = (int)((dist - _redInnerRadius) / radiusDiff * N);
            if (segIdx >= N) segIdx = N - 1;
            if (segIdx < 0) segIdx = 0;

            segmentPresent[segIdx, angleBin] = true;
            validPoints.Add((pt, angleDeg, dist));
        }

        bool[] angleValid = new bool[360];
        for (int i = 0; i < 360; i++)
        {
            bool valid = true;
            for (int j = 0; j < N; j++)
            {
                if (!segmentPresent[j, i]) { valid = false; break; }
            }
            angleValid[i] = valid;
        }

        double maxAngle = -1;
        foreach (var item in validPoints)
        {
            int angleBin = (int)Math.Floor(item.angleDeg);
            if (angleBin >= 0 && angleBin < 360 && angleValid[angleBin])
            {
                if (item.angleDeg > maxAngle) maxAngle = item.angleDeg;
            }
        }

        return maxAngle >= 0 ? maxAngle : null;
    }
    
    private (double start, double end)? GetYellowAngleSpan(Mat maskYellow)
    {
        Cv2.FindContours(maskYellow, out Point[][] contours, out HierarchyIndex[] hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        if (contours.Length == 0) return null;

        Point[] maxContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
        if (Cv2.ContourArea(maxContour) < _minYellowArea) return null;

        int localCenterX = _arcCenter.X - _arcRoi.X;
        int localCenterY = _arcCenter.Y - _arcRoi.Y;

        double minAngle = 360, maxAngle = 0;
        foreach (var pt in maxContour)
        {
            double dx = pt.X - localCenterX;
            double dy = pt.Y - localCenterY;
            double angleDeg = Math.Atan2(dy, dx) * 180.0 / Math.PI;
            if (angleDeg < 0) angleDeg += 360.0;
            if (angleDeg < minAngle) minAngle = angleDeg;
            if (angleDeg > maxAngle) maxAngle = angleDeg;
        }

        return (minAngle, maxAngle);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _arcMask?.Dispose();
            _arcMaskRoi?.Dispose();
            _kernelNoise?.Dispose();
            _disposed = true;
        }
    }
}