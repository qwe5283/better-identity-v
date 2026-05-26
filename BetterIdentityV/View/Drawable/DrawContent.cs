using System.Collections.Concurrent;

namespace BetterIdentityV.View.Drawable;

/// <summary>
/// 定义和管理可在遮罩窗口上绘制的图形元素
/// </summary>
public class DrawContent
{
    
    /// <summary>
    /// 在遮罩窗口上绘制的矩形
    /// </summary>
    public ConcurrentDictionary<string, List<RectDrawable>> RectList { get; set; } = new();

    /// <summary>
    /// 在遮罩窗口上绘制的文本
    /// </summary>
    public ConcurrentDictionary<string, List<TextDrawable>> TextList { get; set; } = new();

    /// <summary>
    /// 在遮罩窗口上绘制的文本
    /// </summary>
    public ConcurrentDictionary<string, List<LineDrawable>> LineList { get; set; } = new();
    
    public virtual void PutRect(string key, RectDrawable newRect)
    {
        if (RectList.TryGetValue(key, out var prevRect))
        {
            if (prevRect.Count == 0 && newRect.Equals(prevRect[0]))
            {
                return;
            }
        }

        RectList[key] = [newRect];
        MaskWindow.Instance().Refresh();
    }
    
    public virtual void RemoveRect(string key)
    {
        if (RectList.TryGetValue(key, out _))
        {
            RectList.TryRemove(key, out _);
            MaskWindow.Instance().Refresh();
        }
    }
    
    public virtual void PutLine(string key, LineDrawable newLine)
    {
        if (LineList.TryGetValue(key, out var prev))
        {
            if (prev.Count == 0 && newLine.Equals(prev[0]))
            {
                return;
            }
        }

        LineList[key] = [newLine];
        MaskWindow.Instance().Refresh();
    }


    public virtual void RemoveLine(string key)
    {
        if (LineList.TryGetValue(key, out _))
        {
            LineList.TryRemove(key, out _);
            MaskWindow.Instance().Refresh();
        }
    }
    
    
    /// <summary>
    /// 清理所有绘制内容
    /// </summary>
    public virtual void ClearAll()
    {
        if (RectList.IsEmpty && TextList.IsEmpty && LineList.IsEmpty)
        {
            return;
        }
        RectList.Clear();
        TextList.Clear();
        LineList.Clear();
        MaskWindow.Instance().Refresh();
    }

}