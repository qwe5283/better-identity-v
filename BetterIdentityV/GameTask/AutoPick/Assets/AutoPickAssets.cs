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
    
    public User32.VK PickToPrimarySlotVk = User32.VK.VK_1;
    public User32.VK PickToSecondarySlotVk = User32.VK.VK_2;

    private AutoPickAssets()
    {
        // 图标矩形ROI区域
        var pickablePrimaryItemRect = new Rect((int)(1703 * AssetScale), (int)(783 * AssetScale),
            (int)(128 * AssetScale), (int)(128 * AssetScale));
        var pickableSecondaryItemRect = new Rect((int)(1573 * AssetScale), (int)(783 * AssetScale),
            (int)(128 * AssetScale), (int)(128 * AssetScale));
        var currentPrimaryItemRect = new Rect((int)(1788 * AssetScale), (int)(932 * AssetScale),
            (int)(128 * AssetScale), (int)(128 * AssetScale));
        var currentSecondaryItemRect = new Rect((int)(1658 * AssetScale), (int)(932 * AssetScale),
            (int)(128 * AssetScale), (int)(128 * AssetScale));
        
        // 拾取槽图标模板初始化 (拾取槽与物品槽图标大小不一致，不能混用；识别对象是引用类型，动态更改ROI属性会导致遮罩绘制问题)
        PickPrimarySlotItemTemplates = new Dictionary<PickableItemType, RecognitionObject>
        {
            [PickableItemType.Controller] = CreateItemRo("(主拾)遥控器", "PickSlot_Controller.png", pickablePrimaryItemRect),
            [PickableItemType.Decoy] = CreateItemRo("(主拾)魔术棒", "PickSlot_Decoy.png", pickablePrimaryItemRect),
            [PickableItemType.ElbowPads] = CreateItemRo("(主拾)护肘", "PickSlot_ElbowPads.png", pickablePrimaryItemRect),
            [PickableItemType.Euphoria] = CreateItemRo("(主拾)香水", "PickSlot_Euphoria.png", pickablePrimaryItemRect),
            [PickableItemType.FlareGun] = CreateItemRo("(主拾)信号枪", "PickSlot_FlareGun.png", pickablePrimaryItemRect),
            [PickableItemType.Flashlight] = CreateItemRo("(主拾)手电筒", "PickSlot_Flashlight.png", pickablePrimaryItemRect),
            [PickableItemType.GulliverTravels] = CreateItemRo("(主拾)游记", "PickSlot_GulliverTravels.png", pickablePrimaryItemRect),
            [PickableItemType.Map] = CreateItemRo("(主拾)地图", "PickSlot_Map.png", pickablePrimaryItemRect),
            [PickableItemType.PoseidonWatch] = CreateItemRo("(主拾)怀表", "PickSlot_PoseidonWatch.png", pickablePrimaryItemRect),
            [PickableItemType.RugbyBall] = CreateItemRo("(主拾)橄榄球", "PickSlot_RugbyBall.png", pickablePrimaryItemRect),
            [PickableItemType.Dovlin] = CreateItemRo("(主拾)多夫林", "PickSlot_Dovlin.png", pickablePrimaryItemRect),
            [PickableItemType.Syringe] = CreateItemRo("(主拾)镇静剂", "PickSlot_Syringe.png", pickablePrimaryItemRect),
            [PickableItemType.Toolkit] = CreateItemRo("(主拾)工具箱", "PickSlot_Toolkit.png", pickablePrimaryItemRect),
            [PickableItemType.SmokeBottle] = CreateItemRo("(主拾)烟雾瓶", "PickSlot_SmokeBottle.png", pickablePrimaryItemRect),
            [PickableItemType.BlackMud] = CreateItemRo("(主拾)污泥", "PickSlot_BlackMud.png", pickablePrimaryItemRect),
            [PickableItemType.ColdsnapFlask] = CreateItemRo("(主拾)速冻瓶", "PickSlot_ColdsnapFlask.png", pickablePrimaryItemRect),
            [PickableItemType.BogusBag] = CreateItemRo("(主拾)博格包", "PickSlot_BogusBag.png", pickablePrimaryItemRect),
        };
        PickSecondarySlotItemTemplates = new Dictionary<PickableItemType, RecognitionObject>
        {
            [PickableItemType.Controller] = CreateItemRo("(副拾)遥控器", "PickSlot_Controller.png", pickableSecondaryItemRect),
            [PickableItemType.Decoy] = CreateItemRo("(副拾)魔术棒", "PickSlot_Decoy.png", pickableSecondaryItemRect),
            [PickableItemType.ElbowPads] = CreateItemRo("(副拾)护肘", "PickSlot_ElbowPads.png", pickableSecondaryItemRect),
            [PickableItemType.Euphoria] = CreateItemRo("(副拾)香水", "PickSlot_Euphoria.png", pickableSecondaryItemRect),
            [PickableItemType.FlareGun] = CreateItemRo("(副拾)信号枪", "PickSlot_FlareGun.png", pickableSecondaryItemRect),
            [PickableItemType.Flashlight] = CreateItemRo("(副拾)手电筒", "PickSlot_Flashlight.png", pickableSecondaryItemRect),
            [PickableItemType.GulliverTravels] = CreateItemRo("(副拾)游记", "PickSlot_GulliverTravels.png", pickableSecondaryItemRect),
            [PickableItemType.Map] = CreateItemRo("(副拾)地图", "PickSlot_Map.png", pickableSecondaryItemRect),
            [PickableItemType.PoseidonWatch] = CreateItemRo("(副拾)怀表", "PickSlot_PoseidonWatch.png", pickableSecondaryItemRect),
            [PickableItemType.RugbyBall] = CreateItemRo("(副拾)橄榄球", "PickSlot_RugbyBall.png", pickableSecondaryItemRect),
            [PickableItemType.Dovlin] = CreateItemRo("(副拾)多夫林", "PickSlot_Dovlin.png", pickableSecondaryItemRect),
            [PickableItemType.Syringe] = CreateItemRo("(副拾)镇静剂", "PickSlot_Syringe.png", pickableSecondaryItemRect),
            [PickableItemType.Toolkit] = CreateItemRo("(副拾)工具箱", "PickSlot_Toolkit.png", pickableSecondaryItemRect),
            [PickableItemType.SmokeBottle] = CreateItemRo("(副拾)烟雾瓶", "PickSlot_SmokeBottle.png", pickableSecondaryItemRect),
            [PickableItemType.BlackMud] = CreateItemRo("(副拾)污泥", "PickSlot_BlackMud.png", pickableSecondaryItemRect),
            [PickableItemType.ColdsnapFlask] = CreateItemRo("(副拾)速冻瓶", "PickSlot_ColdsnapFlask.png", pickableSecondaryItemRect),
            [PickableItemType.BogusBag] = CreateItemRo("(副拾)博格包", "PickSlot_BogusBag.png", pickableSecondaryItemRect),
        };
        
        // 物品槽图标模板初始化
        CurrentPrimaryItemTemplates = new Dictionary<PickableItemType, RecognitionObject>
        {
            [PickableItemType.Controller] = CreateItemRo("(主物)遥控器", "ItemSlot_Controller.png", currentPrimaryItemRect),
            [PickableItemType.Decoy] = CreateItemRo("(主物)魔术棒", "ItemSlot_Decoy.png", currentPrimaryItemRect),
            [PickableItemType.Euphoria] = CreateItemRo("(主物)香水", "ItemSlot_Euphoria.png", currentPrimaryItemRect),
            [PickableItemType.GulliverTravels] = CreateItemRo("(主物)游记", "ItemSlot_GulliverTravels.png", currentPrimaryItemRect),
            [PickableItemType.Flashlight] = CreateItemRo("(主物)手电筒", "ItemSlot_Flashlight.png", currentPrimaryItemRect),
            [PickableItemType.PoseidonWatch] = CreateItemRo("(主物)怀表", "ItemSlot_PoseidonWatch.png", currentPrimaryItemRect),
            [PickableItemType.RugbyBall] = CreateItemRo("(主物)橄榄球", "ItemSlot_RugbyBall.png", currentPrimaryItemRect),
            [PickableItemType.Syringe] = CreateItemRo("(主物)镇静剂", "ItemSlot_Syringe.png", currentPrimaryItemRect),
            [PickableItemType.SmokeBottle] = CreateItemRo("(主物)烟雾瓶", "ItemSlot_SmokeBottle.png", currentPrimaryItemRect),
            [PickableItemType.ColdsnapFlask] = CreateItemRo("(主物)速冻瓶", "ItemSlot_ColdsnapFlask.png", currentPrimaryItemRect),
            [PickableItemType.BogusBag] = CreateItemRo("(主物)博格包", "ItemSlot_BogusBag.png", currentPrimaryItemRect),
        };
        CurrentSecondaryItemTemplates = new Dictionary<PickableItemType, RecognitionObject>
        {
            [PickableItemType.Controller] = CreateItemRo("(副物)遥控器", "ItemSlot_Controller.png", currentSecondaryItemRect),
            [PickableItemType.Decoy] = CreateItemRo("(副物)魔术棒", "ItemSlot_Decoy.png", currentSecondaryItemRect),
            [PickableItemType.Euphoria] = CreateItemRo("(副物)香水", "ItemSlot_Euphoria.png", currentSecondaryItemRect),
            [PickableItemType.GulliverTravels] = CreateItemRo("(副物)游记", "ItemSlot_GulliverTravels.png", currentSecondaryItemRect),
            [PickableItemType.Flashlight] = CreateItemRo("(副物)手电筒", "ItemSlot_Flashlight.png", currentSecondaryItemRect),
            [PickableItemType.PoseidonWatch] = CreateItemRo("(副物)怀表", "ItemSlot_PoseidonWatch.png", currentSecondaryItemRect),
            [PickableItemType.RugbyBall] = CreateItemRo("(副物)橄榄球", "ItemSlot_RugbyBall.png", currentSecondaryItemRect),
            [PickableItemType.Syringe] = CreateItemRo("(副物)镇静剂", "ItemSlot_Syringe.png", currentSecondaryItemRect),
            [PickableItemType.SmokeBottle] = CreateItemRo("(副物)烟雾瓶", "ItemSlot_SmokeBottle.png", currentSecondaryItemRect),
            [PickableItemType.ColdsnapFlask] = CreateItemRo("(副物)速冻瓶", "ItemSlot_ColdsnapFlask.png", currentSecondaryItemRect),
            [PickableItemType.BogusBag] = CreateItemRo("(副物)博格包", "ItemSlot_BogusBag.png", currentSecondaryItemRect),
        };
    }
    
    private RecognitionObject CreateItemRo(string name, string fileName, Rect roi, bool useMask = true)
    {
        return new RecognitionObject
        {
            Name = name,
            UseMask = useMask,
            Threshold = 0.85,
            RegionOfInterest = roi,
            DrawOnWindow = true,
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoPick", fileName),
        }.InitTemplate();
    }
}