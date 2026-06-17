namespace BetterIdentityV.Core.Audio;

/// <summary>
/// 音频匹配模板配置，定义匹配的目标音频样本及各项参数。
/// </summary>
public class AudioMatchPattern
{
    /// <summary>
    /// 模板名称，用于日志和匹配结果标识。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 目标音频样本的WAV文件路径。
    /// </summary>
    public string SamplePath { get; set; } = string.Empty;

    /// <summary>
    /// 匹配阈值，范围 [0, 1]。NCC得分达到此值才视为命中。
    /// </summary>
    public double Threshold { get; set; } = 0.1;

    /// <summary>
    /// NCC得分放大倍率，可用于统一调节灵敏度。
    /// </summary>
    public double Ratio { get; set; } = 1.0;

    /// <summary>
    /// 是否允许连续帧命中时重复触发，默认false。
    /// </summary>
    public bool AllowSuccessiveTrigger { get; set; } = false;

    /// <summary>
    /// 两次触发之间的最小冷却时间，默认0.5s。
    /// </summary>
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// 目标采样率（Hz），音频流和样本将统一重采样到该频率。
    /// </summary>
    public int SampleRate { get; set; } = 32000;

    /// <summary>
    /// 每次匹配的音频帧窗口长度，默认0.2s。
    /// </summary>
    public TimeSpan SampleWindow { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// 高通滤波器截止频率（Hz），低于该频率的信号将被衰减。
    /// </summary>
    public float HighPassCutoffHz { get; set; } = 1000;
}
