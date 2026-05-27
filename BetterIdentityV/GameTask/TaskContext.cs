using BetterIdentityV.Core.Config;
using BetterIdentityV.Core.Simulator;
using BetterIdentityV.GameTask.Model;
using BetterIdentityV.Helpers;
using BetterIdentityV.Service;

namespace BetterIdentityV.GameTask;

/// <summary>
/// 任务上下文，使用单例
/// </summary>
public class TaskContext
{
    private static TaskContext? _uniqueInstance;
    private static object? _instanceLocker;
    
    private TaskContext()
    {
    }

    public static TaskContext Instance()
    {
        return LazyInitializer.EnsureInitialized(ref _uniqueInstance, ref _instanceLocker, () => new TaskContext()); // 延迟初始化
    }

    public void Init(IntPtr hWnd)
    {
        Init(hWnd, SystemControl.FindCaptureAreaHandle(hWnd));
    }

    public void Init(IntPtr hWnd, IntPtr captureAreaHandle)
    {
        GameHandle = hWnd;
        CaptureAreaHandle = captureAreaHandle == IntPtr.Zero ? hWnd : captureAreaHandle;
        PostMessageSimulator = Simulation.PostMessage(GameHandle);
        // TODO: 捕获ArgumentException
        SystemInfo = new SystemInfo(hWnd, CaptureAreaHandle);
        DpiScale = DpiHelper.ScaleY;
        IsInitialized = true;
    }
    
    public bool IsInitialized { get; set; }

    public IntPtr GameHandle { get; set; }
    
    /// <summary>
    /// 对于客户端来说，捕获区句柄就是游戏窗口。对于MuMu模拟器来说，捕获区句柄就是其中嵌套的子窗口。
    /// </summary>
    public IntPtr CaptureAreaHandle { get; set; }
    
    public PostMessageSimulator PostMessageSimulator { get; private set; }
    
    public float DpiScale { get; set; }
    
    public SystemInfo SystemInfo { get; set; }

    public AllConfig Config
    {
        get
        {
            if (ConfigService.Config == null)
            {
                throw new Exception("Config未初始化");
            }
            return ConfigService.Config;
        }
    }
}