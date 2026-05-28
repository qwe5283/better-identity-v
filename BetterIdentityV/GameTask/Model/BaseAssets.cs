using BetterIdentityV.Model;
using OpenCvSharp;

namespace BetterIdentityV.GameTask.Model;

/// <summary>
/// 游戏各类任务的素材基类
/// 必须继承自BaseAssets
/// 且必须晚于TaskContext初始化，也就是 TaskContext.Instance().IsInitialized = true;
/// 在整个任务生命周期开始时,必须先使用 DestroyInstance() 销毁实例,保证资源的类型正确引用
/// </summary>
/// <typeparam name="T"></typeparam>
public class BaseAssets<T> : Singleton<T> where T : class
{
    protected readonly ISystemInfo systemInfo;

    protected BaseAssets()
    {
        this.systemInfo = TaskContext.Instance().SystemInfo;
    }

    protected BaseAssets(ISystemInfo systemInfo)
    {
        this.systemInfo = systemInfo;
    }

    protected Rect CaptureRect => systemInfo.ScaleMax1080PCaptureRect;
    protected double AssetScale => systemInfo.AssetScale;

}