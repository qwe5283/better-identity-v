using BetterIdentityV.Core.Recognition;
using BetterIdentityV.GameTask.Model;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using Vanara.PInvoke;

namespace BetterIdentityV.GameTask.AutoPick.Assets;

public class AutoPickAssets : BaseAssets<AutoPickAssets>
{
    private readonly ILogger<AutoPickAssets> _logger = App.GetLogger<AutoPickAssets>();

    public Rect PickableItemRect;
    public Rect CurrentPrimaryItemRect;
    public Rect CurrentSecondaryItemRect;
    
    public RecognitionObject RugbyBallRo;
    public RecognitionObject SyringeRo;
    public RecognitionObject ElbowPadsRo;
    
    public User32.VK PickToPrimarySlotVk = User32.VK.VK_1;
    public User32.VK PickToSecondarySlotVk = User32.VK.VK_2;

    private AutoPickAssets()
    {
        // 可拾取图标矩形ROI
        PickableItemRect = new Rect((int)(1703 * AssetScale),
            (int)(783 * AssetScale),
            (int)(128 * AssetScale),
            (int)(128 * AssetScale));
        // 物品栏1矩形ROI
        CurrentPrimaryItemRect = new Rect((int)(1788 * AssetScale),
            (int)(932 * AssetScale),
            (int)(128 * AssetScale),
            (int)(128 * AssetScale));
        // 物品栏2矩形ROI
        CurrentSecondaryItemRect = new Rect((int)(1658 * AssetScale),
            (int)(932 * AssetScale),
            (int)(128 * AssetScale),
            (int)(128 * AssetScale));
        // 橄榄球
        RugbyBallRo = new RecognitionObject()
        {
            Name = "RugbyBall",
            UseMask = true,
            DrawOnWindow = true,
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoPick", "RugbyBall.png"),
            RegionOfInterest = PickableItemRect,
        }.InitTemplate();
        // 镇静剂
        SyringeRo = new RecognitionObject()
        {
            Name = "Syringe",
            UseMask = true,
            DrawOnWindow = true,
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoPick", "Syringe.png"),
            RegionOfInterest = PickableItemRect,
        }.InitTemplate();
        // 护腕
        ElbowPadsRo = new RecognitionObject()
        {
            Name = "ElbowPads",
            UseMask = true,
            DrawOnWindow = true,
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoPick", "ElbowPads.png"),
            RegionOfInterest = PickableItemRect,
        }.InitTemplate();
    }
}