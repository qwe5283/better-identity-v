using BetterIdentityV.Core.Recognition;
using BetterIdentityV.Core.Simulator;
using BetterIdentityV.GameTask.AutoPick.Assets;
using BetterIdentityV.GameTask.Model.Area;
using BetterIdentityV.Helpers;
using Microsoft.Extensions.Logging;
using OpenCvSharp;

namespace BetterIdentityV.GameTask.AutoPick;

public class AutoPickTrigger : ITaskTrigger
{
    private readonly ILogger<AutoPickTrigger> _logger = App.GetLogger<AutoPickTrigger>();
    
    public string Name => "自动拾取";
    public bool IsEnabled { get; set; }
    public int Priority => 30;
    public bool IsExclusive => false;
    
    private readonly AutoPickAssets _autoPickAssets = AutoPickAssets.Instance;

    private PickableItem? _pickItem, _primaryItem, _secondaryItem;

    public void Init()
    {
        var config = TaskContext.Instance().Config.AutoPickConfig;
        IsEnabled = config.Enabled;
    }

    public void OnCapture(CaptureContent content)
    {
        _pickItem = FindPickableItem(content);
        _primaryItem = FindCurrentItem(content);
        _secondaryItem = FindCurrentItem(content, false);
        
        _logger.LogDebug($"主物品槽:{_primaryItem}, 副物品槽:{_secondaryItem}, 可拾取:{_pickItem}");

        if (_pickItem != null && _primaryItem == null)
        {
            // 拾取到主物品槽
            _logger.LogInformation("拾取到主物品槽");
            // Simulation.SendInput.Keyboard.KeyPress(_autoPickAssets.PickToPrimarySlotVk);
        }
        else if (_pickItem != null && _secondaryItem == null)
        {
            // 拾取到副物品槽
            _logger.LogInformation("拾取到副物品槽");
            // Simulation.SendInput.Keyboard.KeyPress(_autoPickAssets.PickToSecondarySlotVk);
        }
        
    }

    /// <summary>
    /// 检测可拾取的物品
    /// </summary>
    /// <returns></returns>
    private PickableItem? FindPickableItem(CaptureContent content)
    {
        // 镇静剂
        using Region syringe = content.CaptureRectArea.Find(_autoPickAssets.SyringeRo);
        if (syringe.IsExist())
            return PickableItem.Syringe;
        // 橄榄球
        using Region rugbyBall = content.CaptureRectArea.Find(_autoPickAssets.RugbyBallRo);
        if (rugbyBall.IsExist())
            return PickableItem.RugbyBall;
        // 护腕
        using Region elbowPads = content.CaptureRectArea.Find(_autoPickAssets.ElbowPadsRo);
        if (elbowPads.IsExist())
            return PickableItem.ElbowPads;

        return null;
    }

    /// <summary>
    /// 检测物品栏的物品
    /// </summary>
    /// <param name="content">捕获内容</param>
    /// <param name="findPrimaryItemSlot">true或缺省表示寻找1号物品栏, false表示寻找2号物品栏</param>
    /// <returns></returns>
    private PickableItem? FindCurrentItem(CaptureContent content, bool findPrimaryItemSlot = true)
    {
        Rect roi = findPrimaryItemSlot
            ? _autoPickAssets.CurrentPrimaryItemRect
            : _autoPickAssets.CurrentSecondaryItemRect;
        
        // 镇静剂
        RecognitionObject ro = _autoPickAssets.SyringeRo.Clone();
        ro.RegionOfInterest = roi;
        using Region syringe = content.CaptureRectArea.Find(ro);
        if (syringe.IsExist())
            return PickableItem.Syringe;
        // 橄榄球
        ro = _autoPickAssets.RugbyBallRo.Clone();
        ro.RegionOfInterest = roi;
        using Region rugbyBall = content.CaptureRectArea.Find(ro);
        if (rugbyBall.IsExist())
            return PickableItem.RugbyBall;
        // 护腕
        ro = _autoPickAssets.ElbowPadsRo.Clone();
        ro.RegionOfInterest = roi;
        using Region elbowPads = content.CaptureRectArea.Find(ro);
        if (elbowPads.IsExist())
            return PickableItem.ElbowPads;

        return null;
    }
}