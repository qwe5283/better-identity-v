using BetterIdentityV.Core.Config;
using BetterIdentityV.Helpers.Win32;
using BetterIdentityV.Model;
using BetterIdentityV.Service.Interface;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SharpDX.DXGI;
using Wpf.Ui;

namespace BetterIdentityV.ViewModel.Pages;

public partial class TriggerSettingsPageViewModel : ViewModel
{
    [ObservableProperty] private List<string> _primaryPickButtonNames;
    [ObservableProperty] private List<string> _secondaryPickButtonNames;

    public AllConfig Config { get; set; }

    public IReadOnlyList<GpuDeviceItem> GpuDevices { get; }
    
    private readonly ILogger<TriggerSettingsPageViewModel> _logger = App.GetLogger<TriggerSettingsPageViewModel>();
    private readonly INavigationService _navigationService;

    public TriggerSettingsPageViewModel(IConfigService configService, INavigationService navigationService)
    {
        Config = configService.Get();
        _navigationService = navigationService;
        GpuDevices = LoadGpuDevices();

        _primaryPickButtonNames = new List<string> { "1", "F1" };
        if (!string.IsNullOrEmpty(Config.AutoPickConfig.PrimaryPickKey) &&
            !_primaryPickButtonNames.Contains(Config.AutoPickConfig.PrimaryPickKey.ToUpper()))
        {
            _primaryPickButtonNames.Insert(0, Config.AutoPickConfig.PrimaryPickKey.ToUpper());
        }
        _secondaryPickButtonNames = new List<string> { "2", "F2" };
        if (!string.IsNullOrEmpty(Config.AutoPickConfig.SecondaryPickKey) &&
            !_secondaryPickButtonNames.Contains(Config.AutoPickConfig.SecondaryPickKey.ToUpper()))
        {
            _secondaryPickButtonNames.Insert(0, Config.AutoPickConfig.SecondaryPickKey.ToUpper());
        }
    }

    private IReadOnlyList<GpuDeviceItem> LoadGpuDevices()
    {
        var devices = new List<GpuDeviceItem>();
        try
        {
            using var factory = new Factory1();
            var adapters = factory.Adapters1;
            try
            {
                for (var deviceId = 0; deviceId < adapters.Length; deviceId++)
                {
                    var description = adapters[deviceId].Description1;
                    if ((description.Flags & AdapterFlags.Software) != 0)
                        continue;

                    try
                    {
                        // 间接显示设备也可能使用真实显卡名称，且不会标记为 Software。
                        if (!GpuAdapterHelper.IsHardwareRenderAdapter(description.Luid))
                            continue;
                    }
                    catch (Exception ex)
                    {
                        // 无法确认类型时不放入可选列表，单个设备失败不影响其他显卡。
                        _logger.LogWarning(ex, "无法确认显卡设备 {DeviceId}（{Name}）的类型，已跳过",
                            deviceId, description.Description.Trim());
                        continue;
                    }

                    // 筛选后仍使用原始编号，不能以列表下标代替 DML 设备编号。
                    devices.Add(new GpuDeviceItem(deviceId, description.Description.Trim()));
                }
            }
            finally
            {
                foreach (var adapter in adapters)
                    adapter.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取 DirectML 显卡列表失败");
        }

        var selectedDevice = Config.AutoTargetConfig.GpuDevice;
        if (devices.All(device => device.DeviceId != selectedDevice))
        {
            devices.Add(new GpuDeviceItem(selectedDevice, $"不可用设备（编号 {selectedDevice}）")
            {
                IsAvailable = false
            });
        }

        // 部分显示适配器具有相同名称，附带编号以区分，但不改变原有设备映射。
        var duplicateNames = devices.GroupBy(device => device.Name)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();
        return devices.Select(device => duplicateNames.Contains(device.Name)
            ? device with { DisplayName = $"{device.Name}（设备 {device.DeviceId}）" }
            : device).ToList();
    }
}
