using System.Text.Json.Serialization;
using BetterIdentityV.GameCapture;
using BetterIdentityV.GameTask;
using BetterIdentityV.GameTask.AutoPick;
using BetterIdentityV.GameTask.AutoQTE;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterIdentityV.Core.Config;

[Serializable]
public partial class AllConfig : ObservableObject
{
    /// <summary>
    /// 窗口捕获的方式
    /// </summary>
    [ObservableProperty]
    private string _captureMode = CaptureModes.BitBlt.ToString();
    
    /// <summary>
    /// 触发器触发频率
    /// </summary>
    [ObservableProperty]
    private int _triggerInterval = 50;
    
    /// <summary>
    /// 遮罩窗口配置
    /// </summary>
    public MaskWindowConfig MaskWindowConfig { get; set; } = new();
    
    /// <summary>
    /// 通用配置
    /// </summary>
    public CommonConfig CommonConfig { get; set; } = new();

    /// <summary>
    /// 自动QTE校准配置
    /// </summary>
    public AutoQTEConfig AutoQTEConfig { get; set; } = new();
    
    /// <summary>
    /// 自动拾取配置
    /// </summary>
    public AutoPickConfig AutoPickConfig { get; set; } = new();
    
    [JsonIgnore]
    public Action? OnAnyChangedAction { get; set; }
    
    public void InitEvent()
    {
        PropertyChanged += OnAnyPropertyChanged;
        CommonConfig.PropertyChanged += OnAnyPropertyChanged;
        AutoQTEConfig.PropertyChanged += OnAnyPropertyChanged;
        AutoPickConfig.PropertyChanged += OnAnyPropertyChanged;
    }
    
    public void OnAnyPropertyChanged(object? sender, EventArgs args)
    {
        GameTaskManager.RefreshTriggerConfigs(); // 立即生效
        OnAnyChangedAction?.Invoke();
    }
}