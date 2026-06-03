using System.Diagnostics;
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
    
    private readonly AutoPickAssets _assets = AutoPickAssets.Instance;

    private PickableItemType? _pickPrimaryItem, _pickSecondaryItem, _currentPrimaryItem, _currentSecondaryItem;

    private DateTime _lastPickToPrimarySlotTime, _lastPickToSecondarySlotTime;
    private readonly TimeSpan _coolDownInterval = TimeSpan.FromSeconds(3);

    public void Init()
    {
        var config = TaskContext.Instance().Config.AutoPickConfig;
        IsEnabled = config.Enabled;
    }

    public void OnCapture(CaptureContent content)
    {
        SpeedTimer speedTimer = new SpeedTimer();
        
        _pickPrimaryItem = IdentifySlotItem(content, _assets.PickPrimarySlotItemTemplates);
        _pickSecondaryItem = IdentifySlotItem(content, _assets.PickSecondarySlotItemTemplates);
        _currentPrimaryItem = IdentifySlotItem(content, _assets.CurrentPrimaryItemTemplates);
        _currentSecondaryItem = IdentifySlotItem(content, _assets.CurrentSecondaryItemTemplates);
        
        speedTimer.Record("识别完成");
        Debug.WriteLine($"主物品槽:{_currentPrimaryItem}, 副物品槽:{_currentSecondaryItem}, 主可拾取:{_pickPrimaryItem}, 副可拾取:{_pickSecondaryItem}");

        DateTime now = DateTime.UtcNow;
        
        if (_pickPrimaryItem != null && _currentPrimaryItem == null && now - _lastPickToPrimarySlotTime > _coolDownInterval)
        {
            // 拾取到主物品槽
            _logger.LogInformation($"拾取{_pickPrimaryItem}到主物品槽");
            Simulation.SendInput.Keyboard.KeyPress(_assets.PickToPrimarySlotVk);
            _lastPickToPrimarySlotTime = now;
        }
        else if (_pickSecondaryItem != null && _currentSecondaryItem == null && now - _lastPickToSecondarySlotTime > _coolDownInterval)
        {
            // 拾取到副物品槽
            _logger.LogInformation($"拾取{_pickSecondaryItem}到副物品槽");
            Simulation.SendInput.Keyboard.KeyPress(_assets.PickToSecondarySlotVk);
            _lastPickToSecondarySlotTime = now;
        }
        
        speedTimer.DebugPrint();
        
    }
    
    /// <summary>
    /// 识别物品槽中的物品图标
    /// </summary>
    /// <param name="content">捕获帧</param>
    /// <param name="Templates">模板识别对象列表</param>
    /// <returns></returns>
    private PickableItemType? IdentifySlotItem(CaptureContent content, Dictionary<PickableItemType, RecognitionObject> Templates)
    {
        if (Templates.Count == 0) return null;
        
        foreach (var (itemType, ro) in Templates)
        {
            var found = content.CaptureRectArea.Find(ro);
            if (found.IsExist())
            {
                return itemType;
            }
        }

        return null;
    }

}