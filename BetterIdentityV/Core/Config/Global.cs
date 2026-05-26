using System.IO;
using System.Reflection;

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
}