using System.Collections.Concurrent;
using BetterIdentityV.View.Drawable;

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
        // ReloadAssets();
        TriggerDictionary = new ConcurrentDictionary<string, ITaskTrigger>();

        TriggerDictionary.TryAdd("AutoQTE", new AutoQTE.AutoQTETrigger());
        TriggerDictionary.TryAdd("AutoPick", new AutoPick.AutoPickTrigger());
        
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
            // 清理画布
            VisionContext.Instance().DrawContent.ClearAll();
        }
        
    }
}