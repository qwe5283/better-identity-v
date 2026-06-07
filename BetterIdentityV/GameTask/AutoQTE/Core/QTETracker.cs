namespace BetterIdentityV.GameTask.AutoQTE.Core;

public readonly record struct QTETrackResult(double? HitTimeSec, string Status)
{
    public bool ShouldHit => Status is QTETrackStatus.PredictHit or QTETrackStatus.EmergencyHit;
}

public static class QTETrackStatus
{
    public const string PredictHit = "PREDICT HIT";
    public const string EmergencyHit = "EMERGENCY HIT";
}

/// <summary>
/// 状态追踪器：QTE状态机，负责运动趋势分析与击打预判。
/// </summary>
public class QTETracker
{
    private readonly QTEAssets _assets;
    private readonly Queue<(double Angle, double TimeSec)> _redHistory = new();
    private readonly Queue<(QTEAngleSpan Span, double TimeSec)> _yellowHistory = new();
    private readonly Queue<(double AddTimeSec, double HitTimeSec)> _hitTimeHistory = new();

    private QTEAngleSpan? _lockedYellow;
    private double _angularSpeed;
    private double _lastHitTimeSec = -999d;

    public QTETracker(QTEAssets assets)
    {
        _assets = assets;
    }

    public QTETrackResult Update(double? redAngle, QTEAngleSpan? yellowSpan, double currentTimeSec, double delayCompSec)
    {
        if (currentTimeSec - _lastHitTimeSec < QTEAssets.CooldownSec)
        {
            return new QTETrackResult(null, "Cooldown");
        }

        if (redAngle is null)
        {
            Clear(_redHistory);
            Clear(_hitTimeHistory);

            if (_yellowHistory.TryPeek(out var oldestYellow) &&
                currentTimeSec - oldestYellow.TimeSec > QTEAssets.YellowLagSec)
            {
                Clear(_yellowHistory);
                _lockedYellow = null;
            }

            return new QTETrackResult(null, "No Red");
        }

        var red = redAngle.Value;
        // 红色指针总是从左侧出现
        if (_redHistory.Count == 0 && red >= QTEAssets.NewRedMaxAngle)
        {
            return new QTETrackResult(null, "Red Not Left");
        }

        // 以防截图帧率>游戏帧率，过滤重复采样数据
        if (_redHistory.Count == 0 || Math.Abs(red - _redHistory.Last().Angle) > 0.1)
        {
            _redHistory.Enqueue((red, currentTimeSec));
            Console.WriteLine($"Add to redHistory with angle: {red}, time: {currentTimeSec:0.###}");
        }

        TrimByAge(_redHistory, currentTimeSec, QTEAssets.RedTimeWindowSec);

        if (_redHistory.Count >= 3)
        {
            var speed = FitSpeed(_redHistory);
            if (speed < QTEAssets.MinAngularSpeedDps)
            {
                return new QTETrackResult(null, "Too Slow");
            }

            _angularSpeed = speed;
        }
        else
        {
            return new QTETrackResult(null, "Wait Speed");
        }

        UpdateYellowLock(yellowSpan, currentTimeSec);

        if (_lockedYellow is null)
        {
            return new QTETrackResult(null, "No Yellow");
        }

        var lockedYellow = _lockedYellow.Value;
        var targetAngle = lockedYellow.Start + (lockedYellow.End - lockedYellow.Start) / 3d;
        var timeToTarget = (targetAngle - red) / _angularSpeed;
        var hitTimeSec = currentTimeSec + timeToTarget - delayCompSec;

        if (hitTimeSec > currentTimeSec)
        {
            _hitTimeHistory.Enqueue((currentTimeSec, hitTimeSec));
            TrimHitHistory(currentTimeSec);

            if (_hitTimeHistory.Count > 0 &&
                _hitTimeHistory.Last().AddTimeSec - _hitTimeHistory.Peek().AddTimeSec >
                _assets.HitTimeWindow * 0.5)
            {
                var minHit = _hitTimeHistory.Min(i => i.HitTimeSec);
                var maxHit = _hitTimeHistory.Max(i => i.HitTimeSec);
                Console.WriteLine($"预测队列样本数: {_hitTimeHistory.Count}, 极差: {maxHit - minHit} sec");
                if (maxHit - minHit <= _assets.HitTimeTolerance)
                {
                    var finalHitTime = _hitTimeHistory.Average(i => i.HitTimeSec);
                    _lastHitTimeSec = currentTimeSec;
                    Clear(_hitTimeHistory);
                    return new QTETrackResult(finalHitTime, QTETrackStatus.PredictHit);
                }
            }
            return new QTETrackResult(hitTimeSec, $"Approach R:{red:F1} T:{targetAngle:F1}");
        }

        _lastHitTimeSec = currentTimeSec;
        Clear(_hitTimeHistory);
        return new QTETrackResult(hitTimeSec, QTETrackStatus.EmergencyHit);
    }

    private void UpdateYellowLock(QTEAngleSpan? yellowSpan, double currentTimeSec)
    {
        if (yellowSpan is { } span)
        {
            _yellowHistory.Enqueue((span, currentTimeSec));
            if (_yellowHistory.Count >= 2)
            {
                var startMin = _yellowHistory.Min(i => i.Span.Start);
                var startMax = _yellowHistory.Max(i => i.Span.Start);
                var endMin = _yellowHistory.Min(i => i.Span.End);
                var endMax = _yellowHistory.Max(i => i.Span.End);
                var spanWidth = span.End - span.Start;

                if (startMax - startMin <= QTEAssets.YellowStableTolerance &&
                    endMax - endMin <= QTEAssets.YellowStableTolerance &&
                    spanWidth is >= QTEAssets.MinYellowSpanDeg and <= QTEAssets.MaxYellowSpanDeg)
                {
                    _lockedYellow = span;
                }
            }
        }
        else if (_lockedYellow is not null &&
                 (_yellowHistory.Count == 0 ||
                  currentTimeSec - _yellowHistory.Last().TimeSec > QTEAssets.YellowLagSec))
        {
            _lockedYellow = null;
        }

        TrimByAge(_yellowHistory, currentTimeSec, QTEAssets.YellowLagSec);
    }

    private void TrimHitHistory(double currentTimeSec)
    {
        while (_hitTimeHistory.Count > 0 &&
               currentTimeSec - _hitTimeHistory.Peek().AddTimeSec > _assets.HitTimeWindow)
        {
            _hitTimeHistory.Dequeue();
        }
    }

    private static double FitSpeed(IEnumerable<(double Angle, double TimeSec)> history)
    {
        var points = history.ToArray();
        var firstTime = points[0].TimeSec;
        var sumX = 0d;
        var sumY = 0d;
        var sumXy = 0d;
        var sumX2 = 0d;

        foreach (var (angle, timeSec) in points)
        {
            var x = timeSec - firstTime;
            sumX += x;
            sumY += angle;
            sumXy += x * angle;
            sumX2 += x * x;
        }

        var count = points.Length;
        var denominator = count * sumX2 - sumX * sumX;
        if (Math.Abs(denominator) < double.Epsilon)
        {
            return 0d;
        }

        return (count * sumXy - sumX * sumY) / denominator;
    }

    private static void TrimByAge<T>(Queue<(T Value, double TimeSec)> queue, double currentTimeSec, double maxAgeSec)
    {
        while (queue.Count > 0 && currentTimeSec - queue.Peek().TimeSec > maxAgeSec)
        {
            queue.Dequeue();
        }
    }

    private static void Clear<T>(Queue<T> queue)
    {
        queue.Clear();
    }
}
