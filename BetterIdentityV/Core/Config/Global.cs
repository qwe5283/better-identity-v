using System.IO;
using System.Reflection;
using Semver;

namespace BetterIdentityV.Core.Config;

public class Global
{
    /// <summary>
    /// 项目版本
    /// </summary>
    public static string Version { get; } = Assembly.GetEntryAssembly()?.
        GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.
        InformationalVersion!;
    
    /// <summary>
    /// 启动路径
    /// </summary>
    public static string StartUpPath { get; set; } = AppContext.BaseDirectory;
    
    /// <summary>
    /// 将相对路径转换为绝对路径
    /// </summary>
    /// <param name="relativePath">相对路径</param>
    /// <returns></returns>
    public static string Absolute(string relativePath)
    {
        return Path.Combine(StartUpPath, relativePath);
    }

    /// <summary>
    /// 新获取到的版本号与当前版本号比较，判断是否为新版本
    /// </summary>
    /// <param name="currentVersion">新获取到的版本</param>
    /// <returns></returns>
    public static bool IsNewVersion(string currentVersion)
    {
        return IsNewVersion(Version, currentVersion);
    }
    
    /// <summary>
    /// 新获取到的版本号与当前版本号比较，判断是否为新版本
    /// </summary>
    /// <param name="oldVersion">老版本</param>
    /// <param name="currentVersion">新获取到的版本</param>
    /// <returns>是否需要更新</returns>
    public static bool IsNewVersion(string oldVersion, string currentVersion)
    {
        // 版本号使用SemVer格式（即主版本号.次版本号.修订号[-预发布后缀]）
        try
        {
            var oldVersionX = SemVersion.Parse(oldVersion);
            var currentVersionX = SemVersion.Parse(currentVersion);

            if (currentVersionX.CompareSortOrderTo(oldVersionX) > 0)
                // 需要更新
                return true;
        }
        catch
        {
            ///
        }

        // 不需要更新
        return false;
    }
}