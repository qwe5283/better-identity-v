using System.Collections.Concurrent;
using System.IO;
using BetterIdentityV.Core.Config;
using BetterIdentityV.Core.Recognition.OpenCv;
using BetterIdentityV.GameTask.AutoPick.Assets;
using BetterIdentityV.GameTask.Model;
using BetterIdentityV.View.Drawable;
using OpenCvSharp;

namespace BetterIdentityV.GameTask;

public class GameTaskManager
{
    public static ConcurrentDictionary<string, ITaskTrigger>? TriggerDictionary { get; set; }
    
    /// <summary>
    /// 加载和初始化triggers
    /// 一定要在任务上下文初始化完毕后使用
    /// </summary>
    /// <returns></returns>
    public static List<ITaskTrigger> LoadInitialTriggers()
    {
        ReloadAssets();
        TriggerDictionary = new ConcurrentDictionary<string, ITaskTrigger>();

        TriggerDictionary.TryAdd("AutoQTE", new AutoQTE.AutoQTETrigger());
        TriggerDictionary.TryAdd("AutoPick", new AutoPick.AutoPickTrigger());
        TriggerDictionary.TryAdd("SoundTrigger", new CooldownSoundTrigger.SoundTrigger());
        
        return ConvertToTriggerList();
    }

    public static List<ITaskTrigger> ConvertToTriggerList(bool allEnabled = false)
    {
        if (TriggerDictionary is null)
        {
            return [];
        }
        
        var loadedTriggers = TriggerDictionary.Values.ToList();
        
        loadedTriggers.ForEach(i => i.Init());
        if (allEnabled)
        {
            loadedTriggers.ForEach(i => i.IsEnabled = true);
        }
        
        loadedTriggers = [.. loadedTriggers.OrderByDescending(i => i.Priority)];
        return loadedTriggers;
    }

    public static void RefreshTriggerConfigs()
    {
        if (TriggerDictionary is { Count: > 0 })
        {
            TriggerDictionary.GetValueOrDefault("AutoQTE")?.Init();
            TriggerDictionary.GetValueOrDefault("AutoPick")?.Init();
            TriggerDictionary.GetValueOrDefault("SoundTrigger")?.Init();
            // 清理画布
            VisionContext.Instance().DrawContent.ClearAll();
        }
        
        ReloadAssets();
    }
    
    public static void ReloadAssets()
    {
        AutoPickAssets.DestroyInstance();
    }
    
    /// <summary>
    /// 获取素材图片并缩放
    /// todo 支持多语言
    /// </summary>
    /// <param name="featName">任务名称</param>
    /// <param name="assertName">素材文件名</param>
    /// <param name="flags"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public static Mat LoadAssetImage(string featName, string assertName, ImreadModes flags = ImreadModes.Color)
    {
        return LoadAssetImage(featName, assertName, TaskContext.Instance().SystemInfo, flags);
    }
    
    /// <summary>
    /// 获取素材图片并缩放
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public static Mat LoadAssetImage(string featName, string assertName, ISystemInfo systemInfo, ImreadModes flags = ImreadModes.Color)
    {
        var assetsFolder = Global.Absolute($@"GameTask\{featName}\Assets\{systemInfo.GameScreenSize.Width}x{systemInfo.GameScreenSize.Height}");
        if (!Directory.Exists(assetsFolder))
        {
            assetsFolder = Global.Absolute($@"GameTask\{featName}\Assets\1920x1080");
        }

        if (!Directory.Exists(assetsFolder))
        {
            throw new FileNotFoundException($"未找到{featName}的素材文件夹");
        }

        var filePath = Path.Combine(assetsFolder, assertName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"未找到{featName}中的{assertName}文件");
        }

        var mat = Mat.FromStream(File.OpenRead(filePath), flags);
        if (systemInfo.GameScreenSize.Width != 1920)
        {
            mat = ResizeHelper.Resize(mat, systemInfo.AssetScale);
        }

        return mat;
    }
}
