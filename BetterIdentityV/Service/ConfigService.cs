using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetterIdentityV.Core.Config;
using BetterIdentityV.Service.Interface;

namespace BetterIdentityV.Service;

public class ConfigService : IConfigService
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };
    
    public static AllConfig? Config { get; private set; }
    
    /// <summary>
    /// 获得软件配置
    /// </summary>
    /// <returns>软件配置</returns>
    public AllConfig Get()
    {
        if (Config == null)
        {
            Config = Read();
            Config.OnAnyChangedAction = Save; // 略微影响性能
            Config.InitEvent();
        }

        return Config;
    }

    /// <summary>
    /// 保存配置文件
    /// </summary>
    public void Save()
    {
        if (Config != null)
        {
            Write(Config);
        }
    }

    /// <summary>
    /// 从文件读取配置
    /// </summary>
    /// <returns></returns>
    public AllConfig Read()
    {
        try
        {
            var filePath = Global.Absolute(@"User/config.json");
            if (!File.Exists(filePath))
            {
                return new AllConfig();
            }

            var json = File.ReadAllText(filePath);
            var config = JsonSerializer.Deserialize<AllConfig>(json, JsonOptions);
            if (config == null)
            {
                return new AllConfig();
            }

            Config = config;
            return config;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            return new AllConfig();
        }
    }

    /// <summary>
    /// 将配置写入文件
    /// </summary>
    /// <param name="config">软件配置</param>
    public void Write(AllConfig config)
    {
        try
        {
            var path = Global.Absolute("User");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var file = Path.Combine(path, "config.json");
            File.WriteAllText(file, JsonSerializer.Serialize(config, JsonOptions));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }
    }
}