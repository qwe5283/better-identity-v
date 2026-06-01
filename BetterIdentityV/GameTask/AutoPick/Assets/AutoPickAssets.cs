using BetterIdentityV.Core.Recognition;
using BetterIdentityV.GameTask.Model;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using Vanara.PInvoke;

namespace BetterIdentityV.GameTask.AutoPick.Assets;

public class AutoPickAssets : BaseAssets<AutoPickAssets>
{
    private readonly ILogger<AutoPickAssets> _logger = App.GetLogger<AutoPickAssets>();
    
    public readonly Dictionary<PickableItemType, RecognitionObject> PickPrimarySlotItemTemplates;
    public readonly Dictionary<PickableItemType, RecognitionObject> PickSecondarySlotItemTemplates;
    public readonly Dictionary<PickableItemType, RecognitionObject> CurrentPrimaryItemTemplates;
    public readonly Dictionary<PickableItemType, RecognitionObject> CurrentSecondaryItemTemplates;
    
    public Rect PickablePrimaryItemRect;
    public Rect PickableSecondaryItemRect;
    public Rect CurrentPrimaryItemRect;
    public Rect CurrentSecondaryItemRect;
    
    public User32.VK PickToPrimarySlotVk = User32.VK.VK_1;
    public User32.VK PickToSecondarySlotVk = User32.VK.VK_2;

    private AutoPickAssets()
    {
        // 图标矩形ROI区域
        PickablePrimaryItemRect = new Rect((int)(1703 * AssetScale), (int)(783 * AssetScale),
            (int)(128 * AssetScale), (int)(128 * AssetScale));
        PickableSecondaryItemRect = new Rect((int)(1573 * AssetScale), (int)(783 * AssetScale),
            (int)(128 * AssetScale), (int)(128 * AssetScale));
        CurrentPrimaryItemRect = new Rect((int)(1788 * AssetScale), (int)(932 * AssetScale),
            (int)(128 * AssetScale), (int)(128 * AssetScale));
        CurrentSecondaryItemRect = new Rect((int)(1658 * AssetScale), (int)(932 * AssetScale),
            (int)(128 * AssetScale), (int)(128 * AssetScale));
        
        // 拾取槽图标模板初始化 (拾取槽与物品槽图标大小不一致，不能混用)
        PickPrimarySlotItemTemplates = new Dictionary<PickableItemType, RecognitionObject>
        {
            [PickableItemType.Controller] = CreateItemRo("(主拾)遥控器", "PickSlot_Controller.png", PickablePrimaryItemRect),
            [PickableItemType.Decoy] = CreateItemRo("(主拾)魔术棒", "PickSlot_Decoy.png", PickablePrimaryItemRect),
            [PickableItemType.ElbowPads] = CreateItemRo("(主拾)护肘", "PickSlot_ElbowPads.png", PickablePrimaryItemRect),
            [PickableItemType.Euphoria] = CreateItemRo("(主拾)香水", "PickSlot_Euphoria.png", PickablePrimaryItemRect),
            [PickableItemType.FlareGun] = CreateItemRo("(主拾)信号枪", "PickSlot_FlareGun.png", PickablePrimaryItemRect),
            [PickableItemType.Flashlight] = CreateItemRo("(主拾)手电筒", "PickSlot_Flashlight.png", PickablePrimaryItemRect),
            [PickableItemType.GulliverTravels] = CreateItemRo("(主拾)游记", "PickSlot_GulliverTravels.png", PickablePrimaryItemRect),
            [PickableItemType.Map] = CreateItemRo("(主拾)地图", "PickSlot_Map.png", PickablePrimaryItemRect),
            [PickableItemType.PoseidonWatch] = CreateItemRo("(主拾)怀表", "PickSlot_PoseidonWatch.png", PickablePrimaryItemRect),
            [PickableItemType.RugbyBall] = CreateItemRo("(主拾)橄榄球", "PickSlot_RugbyBall.png", PickablePrimaryItemRect),
            [PickableItemType.Dovlin] = CreateItemRo("(主拾)多夫林", "PickSlot_Dovlin.png", PickablePrimaryItemRect),
            [PickableItemType.Syringe] = CreateItemRo("(主拾)镇静剂", "PickSlot_Syringe.png", PickablePrimaryItemRect),
            [PickableItemType.Toolkit] = CreateItemRo("(主拾)工具箱", "PickSlot_Toolkit.png", PickablePrimaryItemRect)
        };
        PickSecondarySlotItemTemplates = new Dictionary<PickableItemType, RecognitionObject>
        {
            [PickableItemType.Controller] = CreateItemRo("(副拾)遥控器", "PickSlot_Controller.png", PickableSecondaryItemRect),
            [PickableItemType.Decoy] = CreateItemRo("(副拾)魔术棒", "PickSlot_Decoy.png", PickableSecondaryItemRect),
            [PickableItemType.ElbowPads] = CreateItemRo("(副拾)护肘", "PickSlot_ElbowPads.png", PickableSecondaryItemRect),
            [PickableItemType.Euphoria] = CreateItemRo("(副拾)香水", "PickSlot_Euphoria.png", PickableSecondaryItemRect),
            [PickableItemType.FlareGun] = CreateItemRo("(副拾)信号枪", "PickSlot_FlareGun.png", PickableSecondaryItemRect),
            [PickableItemType.Flashlight] = CreateItemRo("(副拾)手电筒", "PickSlot_Flashlight.png", PickableSecondaryItemRect),
            [PickableItemType.GulliverTravels] = CreateItemRo("(副拾)游记", "PickSlot_GulliverTravels.png", PickableSecondaryItemRect),
            [PickableItemType.Map] = CreateItemRo("(副拾)地图", "PickSlot_Map.png", PickableSecondaryItemRect),
            [PickableItemType.PoseidonWatch] = CreateItemRo("(副拾)怀表", "PickSlot_PoseidonWatch.png", PickableSecondaryItemRect),
            [PickableItemType.RugbyBall] = CreateItemRo("(副拾)橄榄球", "PickSlot_RugbyBall.png", PickableSecondaryItemRect),
            [PickableItemType.Dovlin] = CreateItemRo("(副拾)多夫林", "PickSlot_Dovlin.png", PickableSecondaryItemRect),
            [PickableItemType.Syringe] = CreateItemRo("(副拾)镇静剂", "PickSlot_Syringe.png", PickableSecondaryItemRect),
            [PickableItemType.Toolkit] = CreateItemRo("(副拾)工具箱", "PickSlot_Toolkit.png", PickableSecondaryItemRect)
        };
        
        // 物品槽图标模板初始化
        CurrentPrimaryItemTemplates = new Dictionary<PickableItemType, RecognitionObject>
        {
            [PickableItemType.Controller] = CreateItemRo("(主物)遥控器", "ItemSlot_Controller.png", CurrentPrimaryItemRect),
            [PickableItemType.Euphoria] = CreateItemRo("(主物)香水", "ItemSlot_Euphoria.png", CurrentPrimaryItemRect),
            [PickableItemType.Flashlight] = CreateItemRo("(主物)手电筒", "ItemSlot_Flashlight.png", CurrentPrimaryItemRect),
            [PickableItemType.Syringe] = CreateItemRo("(主物)镇静剂", "ItemSlot_Syringe.png", CurrentPrimaryItemRect),
        };
        CurrentSecondaryItemTemplates = new Dictionary<PickableItemType, RecognitionObject>
        {
            [PickableItemType.Controller] = CreateItemRo("(副物)遥控器", "ItemSlot_Controller.png", CurrentSecondaryItemRect),
            [PickableItemType.Euphoria] = CreateItemRo("(副物)香水", "ItemSlot_Euphoria.png", CurrentSecondaryItemRect),
            [PickableItemType.Flashlight] = CreateItemRo("(副物)手电筒", "ItemSlot_Flashlight.png", CurrentSecondaryItemRect),
            [PickableItemType.Syringe] = CreateItemRo("(副物)镇静剂", "ItemSlot_Syringe.png", CurrentSecondaryItemRect),
        };
    }
    
    private RecognitionObject CreateItemRo(string name, string fileName, Rect roi, bool useMask = true)
    {
        return new RecognitionObject
        {
            Name = name,
            UseMask = useMask,
            Use3Channels = true,
            RegionOfInterest = roi,
            DrawOnWindow = true,
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoPick", fileName),
        }.InitTemplate();
    }
}