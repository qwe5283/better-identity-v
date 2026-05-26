using System.Diagnostics;

namespace BetterIdentityV.GameTask.AutoQTE.Core;

/// <summary>
/// 状态追踪器：QTE状态机，负责运动趋势分析与击打预判
/// </summary>
public class QTETracker
{
    private readonly QTEAssets _assets;
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private double CurrentTimeSec => _sw.Elapsed.TotalSeconds;

    private bool _triggered;
    private double _lastTriggerTime;

    private readonly Queue<(double angle, double time)> _redHistory = new();
    private double _angularSpeed;

    private readonly Queue<((double start, double end) span, double time)> _yellowHistory = new();
    private double _lastYellowTime = 0;
    private (double start, double end)? _lockedYellowSpan;

    public string StatusMsg = "Waiting";

    public QTETracker(QTEAssets assets)
    {
        _assets = assets;
    }

    public bool UpdateAndCheck(double? redFrontAngle, (double start, double end)? yellowSpan, double delaySec = 0.0)
    {
        double currentTime = CurrentTimeSec;

        if (_triggered)
        {
            if (currentTime - _lastTriggerTime >= _assets.CooldownSec)
            {
                _triggered = false;
                _yellowHistory.Clear();
                _redHistory.Clear();
                _lockedYellowSpan = null;
            }
            else
            {
                StatusMsg = "Cooldown";
                return false;
            }
        }

        if (!redFrontAngle.HasValue)
        {
            StatusMsg = "No Red";
            _redHistory.Clear();
            if (_yellowHistory.Count > 0 && currentTime - _lastYellowTime > _assets.YellowLagSec)
            {
                _yellowHistory.Clear();
                _lockedYellowSpan = null;
            }

            return false;
        }

        if (_redHistory.Count == 0 && redFrontAngle.Value >= _assets.NewRedMaxAngle)
        {
            StatusMsg = "Red Not On Left Side";
            return false;
        }

        _redHistory.Enqueue((redFrontAngle.Value, currentTime));
        if (!CheckRedMovingRight(currentTime))
        {
            StatusMsg = "Red Not Moving/Too Slow";
            return false;
        }

        if (!UpdateYellowState(yellowSpan, currentTime))
        {
            StatusMsg = "Yellow Missing/Unstable";
            return false;
        }

        if (!_lockedYellowSpan.HasValue) return false;

        double targetAngle = _lockedYellowSpan.Value.start +
                             (_lockedYellowSpan.Value.end - _lockedYellowSpan.Value.start) / 3.0;
        double timeToTarget = (targetAngle - _redHistory.Last().angle) / _angularSpeed;
        double timeToTrigger = timeToTarget - delaySec;

        if (timeToTrigger <= 0)
        {
            _triggered = true;
            _lastTriggerTime = currentTime;
            StatusMsg = ">>> HIT! SPACE <<<";
            _redHistory.Clear();
            _lockedYellowSpan = null;
            return true;
        }

        StatusMsg = $"Approaching... R:{redFrontAngle.Value:F1} T:{targetAngle:F1}";
        return false;
    }

    private bool CheckRedMovingRight(double currentTime)
    {
        while (_redHistory.Count > 0 && currentTime - _redHistory.Peek().time > _assets.RedTimeWindowSec)
        {
            _redHistory.Dequeue();
        }

        if (_redHistory.Count < 5) return false;

        double t0 = _redHistory.Peek().time;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        int n = _redHistory.Count;

        foreach (var item in _redHistory)
        {
            double x = item.time - t0;
            double y = item.angle;
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        double denom = (n * sumX2 - sumX * sumX);
        if (Math.Abs(denom) < 1e-9) return false;

        double speed = (n * sumXY - sumX * sumY) / denom;
        if (speed > _assets.MinAngularSpeedDps)
        {
            _angularSpeed = speed;
            return true;
        }

        return false;
    }

    private bool UpdateYellowState((double start, double end)? currentYellowSpan, double currentTime)
    {
        while (_yellowHistory.Count > 0 && currentTime - _yellowHistory.Peek().time > _assets.YellowLagSec)
        {
            _yellowHistory.Dequeue();
        }

        if (currentYellowSpan.HasValue)
        {
            _yellowHistory.Enqueue((currentYellowSpan.Value, currentTime));
            _lastYellowTime = currentTime;

            if (_yellowHistory.Count >= 2)
            {
                double minStart = double.MaxValue, maxStart = double.MinValue;
                double minEnd = double.MaxValue, maxEnd = double.MinValue;

                foreach (var item in _yellowHistory)
                {
                    if (item.span.start < minStart) minStart = item.span.start;
                    if (item.span.start > maxStart) maxStart = item.span.start;
                    if (item.span.end < minEnd) minEnd = item.span.end;
                    if (item.span.end > maxEnd) maxEnd = item.span.end;
                }

                if ((maxStart - minStart <= _assets.YellowStableTolerance) &&
                    (maxEnd - minEnd <= _assets.YellowStableTolerance))
                {
                    double spanWidth = currentYellowSpan.Value.end - currentYellowSpan.Value.start;
                    if (spanWidth > _assets.MinYellowSpanDeg && spanWidth < _assets.MaxYellowSpanDeg)
                    {
                        _lockedYellowSpan = currentYellowSpan;
                    }
                }
            }

            return _lockedYellowSpan.HasValue;
        }
        else
        {
            if (_lockedYellowSpan.HasValue)
            {
                if (_yellowHistory.Count > 0 && currentTime - _lastYellowTime < _assets.YellowLagSec)
                {
                    return true;
                }
                else
                {
                    _lockedYellowSpan = null;
                }
            }

            return false;
        }
    }
}