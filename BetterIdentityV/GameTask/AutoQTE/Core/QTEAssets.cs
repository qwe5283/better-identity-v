using OpenCvSharp;
using Vanara.PInvoke;

namespace BetterIdentityV.GameTask.AutoQTE.Core;

public class QTEAssets
{
    // HSV范围
    public Scalar RedLower1 = new Scalar(0, 157, 90);
    public Scalar RedUpper1 = new Scalar(8, 255, 255);
    public Scalar RedLower2 = new Scalar(175, 157, 90);
    public Scalar RedUpper2 = new Scalar(180, 255, 255);
    
    public Scalar YellowLower = new Scalar(14, 70, 140);
    public Scalar YellowUpper = new Scalar(23, 140, 255);

    // 圆弧几何参数 (基于1080P)
    public const int ArcCenterX = 967;
    public const int ArcCenterY = 900;
    public const int Radius = 338;
    public const int Thickness = 22;
    
    public const int StartAngle = 200;
    public const int EndAngle = 340;

    // 状态机与鲁棒性参数
    public const double CooldownSec = 1.5;
    public const double NewRedMaxAngle = 215.0;
    public const double MinAngularSpeedDps = 60.0;
    public const double RedTimeWindowSec = 0.3;
    
    public const double MinYellowSpanDeg = 4.0;
    public const double MaxYellowSpanDeg = 10.0;
    public const double YellowLagSec = 0.25;
    public const double YellowStableTolerance = 0.5;

    public double HitTimeWindow = 0.3;
    public double HitTimeTolerance = 0.05; // 击打预测时间容差不应小于游戏渲染帧间隔
    
    // 击打按键
    public User32.VK VkHitQTE = User32.VK.VK_SPACE;

}