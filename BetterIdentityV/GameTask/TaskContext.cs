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
        GameHandle = hWnd;
        PostMessageSimulator = Simulation.PostMessage(GameHandle);
        SystemInfo = new SystemInfo(hWnd);
        DpiScale = DpiHelper.ScaleY;
        IsInitialized = true;
    }
    
    public bool IsInitialized { get; set; }

    public IntPtr GameHandle { get; set; }
    
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