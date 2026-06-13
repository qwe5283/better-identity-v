using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using BetterIdentityV.Core.Config;
using BetterIdentityV.Model;
using BetterIdentityV.Service.Interface;
using Microsoft.Extensions.Logging;
using MessageBox = Wpf.Ui.Violeta.Controls.MessageBox;

namespace BetterIdentityV.Service;

public class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly IConfigService _configService;
    
    private const string NoticeUrl = "https://api.github.com/repos/qwe5283/better-identity-v/releases/latest";
    private const string DownloadPageUrl = "https://github.com/qwe5283/better-identity-v/releases/latest";
    
    public AllConfig Config { get; set; }
    
    public UpdateService(IConfigService configService)
    {
        _logger = App.GetLogger<UpdateService>();
        _configService = configService;
        Config = _configService.Get();
    }
    
    /// <summary>
    /// Please call me in main thread
    /// </summary>
    /// <param name="option"></param>
    public async Task CheckUpdateAsync(UpdateOption option)
    {
        string newVersion = await GetLatestVersionAsync(option);
        
        if (string.IsNullOrWhiteSpace(newVersion))
        {
            if (option.Trigger == UpdateTrigger.Manual)
                await MessageBox.InformationAsync("检查更新暂不可用！");
            return;
        }

        if (!Global.IsNewVersion(newVersion))
        {
            if (option.Trigger == UpdateTrigger.Manual)
                await MessageBox.InformationAsync("当前已是最新版本！");
            return;
        }
        
        OpenCheckUpdateWindow(option, newVersion);
    }

    private void OpenCheckUpdateWindow(UpdateOption option, string newVersion)
    {
        var result = MessageBox.Question("发现新版本，需要打开下载页吗？");
        if (result.Equals(MessageBoxResult.Yes))
        {
            Process.Start(new ProcessStartInfo(DownloadPageUrl) { UseShellExecute = true });
        }
    }
    
    private async Task<string> GetLatestVersionAsync(UpdateOption option)
    {
        return await UpdateFromGithub();
    }

    /// <summary>
    /// 从 GitHub API 读取最新版本号
    /// </summary>
    /// <returns></returns>
    private async Task<string> UpdateFromGithub()
    {
        try
        {
            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "BetterIdentityV");
            string data = await httpClient.GetStringAsync(NoticeUrl);
            
            using JsonDocument doc = JsonDocument.Parse(data);
            JsonElement root = doc.RootElement;
            
            if (root.TryGetProperty("tag_name", out JsonElement tagNameElement))
            {
                string tagName = tagNameElement.GetString()!;
                _logger.LogInformation($"最新版本号: {tagName}");
                return tagName;
            }
        }
        catch (Exception e)
        {
            _ = e;
        }

        return string.Empty;
    }
}