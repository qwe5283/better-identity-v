using OpenCvSharp;

namespace BetterIdentityV.GameTask.AutoQTE.Core;

public readonly record struct QTEAngleSpan(double Start, double End);

public readonly record struct QTEDetectionResult(double? RedAngle, QTEAngleSpan? YellowSpan);

public class QTEDetector : IDisposable
{
    private readonly QTEAssets _assets;
    private readonly Point2f _localCenter;
    private readonly Rect _roiRect;
    private readonly bool[] _validAngleMask;

    private const int PolarHeight = 720;
    private const int PolarWidth = QTEAssets.Radius;
    private const double AngleResolution = PolarHeight / 360d;

    public QTEDetector(QTEAssets assets)
    {
        _assets = assets;

        var squareHalf = QTEAssets.Radius + 10;
        var x1 = Math.Max(0, QTEAssets.ArcCenterX - squareHalf);
        var y1 = Math.Max(0, QTEAssets.ArcCenterY - squareHalf);
        var x2 = Math.Min(1920, QTEAssets.ArcCenterX + squareHalf);
        var y2 = Math.Min(1080, QTEAssets.ArcCenterY + squareHalf);

        _roiRect = new Rect(x1, y1, x2 - x1, y2 - y1);
        _localCenter = new Point2f(QTEAssets.ArcCenterX - x1, QTEAssets.ArcCenterY - y1);
        _validAngleMask = BuildValidAngleMask();
    }

    public QTEDetectionResult Process(Mat frame1080p)
    {
        if (frame1080p.Empty())
        {
            return default;
        }

        using var frame = EnsureBgr(frame1080p);
        if (frame.Width < _roiRect.Right || frame.Height < _roiRect.Bottom)
        {
            return default;
        }

        using var roi = new Mat(frame, _roiRect);
        using var hsv = new Mat();
        Cv2.CvtColor(roi, hsv, ColorConversionCodes.BGR2HSV);

        using var maskRed1 = new Mat();
        using var maskRed2 = new Mat();
        using var maskRed = new Mat();
        using var maskYellow = new Mat();
        Cv2.InRange(hsv, _assets.RedLower1, _assets.RedUpper1, maskRed1);
        Cv2.InRange(hsv, _assets.RedLower2, _assets.RedUpper2, maskRed2);
        Cv2.BitwiseOr(maskRed1, maskRed2, maskRed);
        Cv2.InRange(hsv, _assets.YellowLower, _assets.YellowUpper, maskYellow);

        using var polarRed = new Mat();
        using var polarYellow = new Mat();
        Cv2.WarpPolar(
            maskRed,
            polarRed,
            new Size(PolarWidth, PolarHeight),
            _localCenter,
            QTEAssets.Radius,
            InterpolationFlags.Linear,
            WarpPolarMode.Linear);
        Cv2.WarpPolar(
            maskYellow,
            polarYellow,
            new Size(PolarWidth, PolarHeight),
            _localCenter,
            QTEAssets.Radius,
            InterpolationFlags.Linear,
            WarpPolarMode.Linear);

        var innerYellowRadius = Math.Max(0, QTEAssets.Radius - QTEAssets.Thickness - 5);
        var innerRedRadius = Math.Max(0, QTEAssets.Radius - QTEAssets.Thickness * 4);

        var redAngle = GetRedAngle(polarRed, innerRedRadius);
        var yellowSpan = GetYellowSpan(polarYellow, innerYellowRadius);

        return new QTEDetectionResult(redAngle, yellowSpan);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private static Mat EnsureBgr(Mat source)
    {
        if (source.Channels() == 3)
        {
            return source.Clone();
        }

        var converted = new Mat();
        if (source.Channels() == 4)
        {
            Cv2.CvtColor(source, converted, ColorConversionCodes.BGRA2BGR);
        }
        else if (source.Channels() == 1)
        {
            Cv2.CvtColor(source, converted, ColorConversionCodes.GRAY2BGR);
        }
        else
        {
            throw new NotSupportedException($"Unsupported frame channel count: {source.Channels()}");
        }

        return converted;
    }

    private double? GetRedAngle(Mat polarRed, int innerRadius)
    {
        var radialThickness = QTEAssets.Radius - innerRadius;
        var threshold = radialThickness * 255 * 0.8;
        var maxIndex = -1;

        for (var angleIndex = 0; angleIndex < polarRed.Rows; angleIndex++)
        {
            if (!_validAngleMask[angleIndex])
            {
                continue;
            }

            var sum = SumRowBand(polarRed, angleIndex, innerRadius, QTEAssets.Radius);
            if (sum > threshold)
            {
                maxIndex = angleIndex;
            }
        }

        return maxIndex < 0 ? null : maxIndex / AngleResolution;
    }

    private QTEAngleSpan? GetYellowSpan(Mat polarYellow, int innerRadius)
    {
        var radialThickness = QTEAssets.Radius - innerRadius;
        var threshold = radialThickness * 255 * 0.2;
        var bestStart = -1;
        var bestEnd = -1;
        var bestLength = 0;
        var currentStart = -1;
        var currentLength = 0;

        for (var angleIndex = 0; angleIndex < polarYellow.Rows; angleIndex++)
        {
            var valid = _validAngleMask[angleIndex] &&
                        SumRowBand(polarYellow, angleIndex, innerRadius, QTEAssets.Radius) > threshold;

            if (valid)
            {
                if (currentStart < 0)
                {
                    currentStart = angleIndex;
                }

                currentLength++;
                continue;
            }

            CommitCurrentSegment(angleIndex - 1);
        }

        CommitCurrentSegment(polarYellow.Rows - 1);

        if (bestStart < 0)
        {
            return null;
        }

        var spanDeg = (bestEnd - bestStart) / AngleResolution;
        if (spanDeg is < QTEAssets.MinYellowSpanDeg or > QTEAssets.MaxYellowSpanDeg)
        {
            return null;
        }

        return new QTEAngleSpan(bestStart / AngleResolution, bestEnd / AngleResolution);

        void CommitCurrentSegment(int currentEnd)
        {
            if (currentStart < 0)
            {
                return;
            }

            if (currentLength > bestLength)
            {
                bestStart = currentStart;
                bestEnd = currentEnd;
                bestLength = currentLength;
            }

            currentStart = -1;
            currentLength = 0;
        }
    }

    private static long SumRowBand(Mat mat, int row, int startCol, int endCol)
    {
        long sum = 0;
        for (var col = startCol; col < endCol; col++)
        {
            sum += mat.At<byte>(row, col);
        }

        return sum;
    }

    private static bool[] BuildValidAngleMask()
    {
        var mask = new bool[PolarHeight];
        var startIndex = (int)(QTEAssets.StartAngle * AngleResolution);
        var endIndex = (int)(QTEAssets.EndAngle * AngleResolution);

        for (var i = startIndex; i < endIndex && i < mask.Length; i++)
        {
            mask[i] = true;
        }

        return mask;
    }
}
