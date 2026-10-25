using System.Runtime.InteropServices;

namespace BetterIdentityV.Helpers.Win32;

/// <summary>
/// 查询 Windows 内核适配器类型，区分渲染设备和沿用显卡名称的间接显示设备。
/// </summary>
internal static class GpuAdapterHelper
{
    private const int AdapterTypeQuery = 15; // KMTQAITYPE_ADAPTERTYPE

    [Flags]
    private enum AdapterType : uint
    {
        RenderSupported = 1u << 0,
        SoftwareDevice = 1u << 2,
        IndirectDisplayDevice = 1u << 6
    }

    public static bool IsHardwareRenderAdapter(long adapterLuid)
    {
        var type = QueryAdapterType(adapterLuid);
        return (type & AdapterType.RenderSupported) != 0 &&
               (type & (AdapterType.SoftwareDevice | AdapterType.IndirectDisplayDevice)) == 0;
    }

    private static unsafe AdapterType QueryAdapterType(long adapterLuid)
    {
        var open = new OpenAdapterFromLuid { AdapterLuid = adapterLuid };
        var status = D3DKMTOpenAdapterFromLuid(ref open);
        if (status != 0)
            throw new InvalidOperationException($"打开显卡适配器失败，NTSTATUS=0x{status:X8}");

        try
        {
            var type = default(AdapterType);
            var query = new QueryAdapterInfo
            {
                AdapterHandle = open.AdapterHandle,
                Type = AdapterTypeQuery,
                PrivateDriverData = (nint)(&type),
                PrivateDriverDataSize = (uint)sizeof(AdapterType)
            };
            status = D3DKMTQueryAdapterInfo(ref query);
            if (status != 0)
                throw new InvalidOperationException($"查询显卡适配器类型失败，NTSTATUS=0x{status:X8}");

            return type;
        }
        finally
        {
            var close = new CloseAdapter { AdapterHandle = open.AdapterHandle };
            _ = D3DKMTCloseAdapter(ref close);
        }
    }

    // 原生 LUID 由两个 32 位整数构成，整体结构采用 4 字节对齐。
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct OpenAdapterFromLuid
    {
        public long AdapterLuid;
        public uint AdapterHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct QueryAdapterInfo
    {
        public uint AdapterHandle;
        public int Type;
        public nint PrivateDriverData;
        public uint PrivateDriverDataSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CloseAdapter
    {
        public uint AdapterHandle;
    }

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int D3DKMTOpenAdapterFromLuid(ref OpenAdapterFromLuid value);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int D3DKMTQueryAdapterInfo(ref QueryAdapterInfo value);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int D3DKMTCloseAdapter(ref CloseAdapter value);
}
