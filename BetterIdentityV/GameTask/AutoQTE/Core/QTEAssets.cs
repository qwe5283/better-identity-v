using OpenCvSharp;

namespace BetterIdentityV.GameTask.AutoQTE.Core;

public class QTEAssets
{
    // 红色HSV范围（两段）
    public Scalar RedLower1 = new Scalar(0, 157, 90);
    public Scalar RedUpper1 = new Scalar(8, 255, 255);
    public Scalar RedLower2 = new Scalar(175, 157, 90);
    public Scalar RedUpper2 = new Scalar(180, 255, 255);

    // 黄色HSV范围
    public Scalar YellowLower = new Scalar(14, 70, 140);
    public Scalar YellowUpper = new Scalar(23, 140, 255);

    // 圆弧归一化几何参数 (基于16:9比例)
    public double ArcCenterX = 0.504;
    public double ArcCenterY = 0.834;
    public double Radius = 0.313;
    public double Thickness = 0.02;
    public int StartAngle = 200;
    public int EndAngle = 340;

    // 延迟补偿与追踪器参数
    public double ClientDelayMs = 0.0;
    public double CooldownSec = 1.5;
    public double RedTimeWindowSec = 0.4;
    public double MinAngularSpeedDps = 60.0;
    public double NewRedMaxAngle = 215.0;
    public double MinYellowSpanDeg = 4.0;
    public double MaxYellowSpanDeg = 10.0;
    public double YellowLagSec = 0.4;
    public double YellowStableTolerance = 0.3;
    public double RedInnerExtendMult = 4.0;
    public int RedRadialSegments = 8;
}