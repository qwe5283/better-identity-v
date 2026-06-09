using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using System.Security.Principal;
using BetterIdentityV.Core.Config;
using Vanara.PInvoke;
using Wpf.Ui.Violeta.Controls;

namespace BetterIdentityV.Helpers;

internal static class RuntimeHelper
{
    public static bool IsElevated { get; } = GetElevated();
    
    private static bool GetElevated()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    
    public static void EnsureElevated()
    {
        if (!IsElevated)
        {
            RestartAsElevated();
        }
    }

    private static string ReArguments()
    {
        string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

        for (int i = default; i < args.Length; i++)
        {
            args[i] = $@"""{args[i]}""";
        }
        return string.Join(" ", args);
    }

    private static void RestartAsElevated(string fileName = null!, string dir = null!, string args = null!, int? exitCode = null, bool forced = false)
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                UseShellExecute = true,
                WorkingDirectory = dir ?? Global.StartUpPath,
                FileName = fileName ?? "BetterIDV.exe",
                Arguments = args ?? ReArguments(),
                Verb = "runas"
            };
            try
            {
                _ = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                // Wpf.Ui.Violeta.Controls.MessageBox模态消息框会干扰WPF的初始化
                User32.MessageBox(HWND.NULL, 
                    "提权启动BetterIDV失败，针对游戏客户端的模拟操作功能将不可用！\r\n请尝试右键以管理员身份运行的方式启动BetterIDV",
                    "提权失败",
                    User32.MB_FLAGS.MB_OK | User32.MB_FLAGS.MB_ICONWARNING);
                return;
            }
        }
        catch (Win32Exception)
        {
            return;
        }
        if (forced)
        {
            Process.GetCurrentProcess().Kill();
        }
        Environment.Exit(exitCode ?? 'r' + 'u' + 'n' + 'a' + 's');
    }
}

internal static class RuntimeExtension
{
    /// <summary>
    /// 管理员提权
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IHostBuilder UseElevated(this IHostBuilder app)
    {
        RuntimeHelper.EnsureElevated();
        return app;
    }
}