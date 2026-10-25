using System.Collections.Concurrent;

using System.Windows;
using System.Windows.Threading;

namespace BetterIdentityV.View.Drawable;

/// <summary>
/// 定义和管理可在遮罩窗口上绘制的图形元素
/// </summary>
public class DrawContent
{
    private int _rectListRefreshPending;
    
    /// <summary>
    /// 在遮罩窗口上绘制的矩形
    /// </summary>
    public ConcurrentDictionary<string, List<RectDrawable>> RectList { get; set; } = new();

    /// <summary>
    /// 在遮罩窗口上绘制的文本
    /// </summary>
    public ConcurrentDictionary<string, List<TextDrawable>> TextList { get; set; } = new();

    /// <summary>
    /// 在遮罩窗口上绘制的线段
    /// </summary>
    public ConcurrentDictionary<string, List<LineDrawable>> LineList { get; set; } = new();
    
    public virtual void PutRect(string key, RectDrawable newRect, double? confidence = null)
    {
        if (RectList.TryGetValue(key, out var prevRect))
        {
            if (prevRect.Count == 0 && newRect.Equals(prevRect[0]))
            {
                return;
            }
        }

        RectList[key] = [newRect];

        if (confidence != null)
        {
            var textPoint = new System.Windows.Point(newRect.Rect.X, newRect.Rect.Y - 16);
            TextList[key + "_confidence"] = [new TextDrawable(
                $"{confidence:F2} {key}", textPoint)];
        }
        else
        {
            TextList.TryRemove(key + "_confidence", out _);
        }
        
        MaskWindow.Instance().Refresh();
    }
    
    public virtual void RemoveRect(string key)
    {
        if (RectList.TryGetValue(key, out _))
        {
            RectList.TryRemove(key, out _);
            TextList.TryRemove(key + "_confidence", out _);
            MaskWindow.Instance().Refresh();
        }
    }
    
    /// <summary>
    /// 按帧整体替换一组矩形，空结果移除旧框；合并高频更新，异步通知遮罩重绘。
    /// </summary>
    public virtual void PutOrRemoveRectList(string key, List<RectDrawable>? rectangles)
    {
        if (rectangles is { Count: > 0 })
        {
            RectList[key] = rectangles;
        }
        else if (!RectList.TryRemove(key, out _))
        {
            return;
        }

        if (Interlocked.Exchange(ref _rectListRefreshPending, 1) != 0)
            return;

        _ = Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Interlocked.Exchange(ref _rectListRefreshPending, 0);
            // 截图线程可能先于遮罩窗口启动，窗口尚未创建时只保留最新绘图数据。
            Application.Current.Windows.OfType<MaskWindow>().FirstOrDefault()?.Refresh();
        }, DispatcherPriority.Render);
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
