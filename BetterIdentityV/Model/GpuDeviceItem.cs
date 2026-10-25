namespace BetterIdentityV.Model;

/// <summary>
/// 显卡下拉项，保留 DirectML 使用的原始 DXGI 设备编号。
/// </summary>
public sealed record GpuDeviceItem(int DeviceId, string Name)
{
    public string DisplayName { get; init; } = Name;

    public bool IsAvailable { get; init; } = true;
}
