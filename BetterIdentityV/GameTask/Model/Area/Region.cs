using System.Drawing;
using BetterIdentityV.GameTask.Model.Area.Converter;
using BetterIdentityV.View.Drawable;
using OpenCvSharp;

namespace BetterIdentityV.GameTask.Model.Area;

/// <summary>
/// 区域基类
/// 用于描述一个区域，可以是一个矩形，也可以是一个点
/// </summary>
public class Region : IDisposable
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    
    public int Top
    {
        get => Y;
        set => Y = value;
    }

    /// <summary>
    /// Gets the y-coordinate that is the sum of the Y and Height property values of this Rect structure.
    /// </summary>
    public int Bottom => Y + Height;

    /// <summary>
    /// Gets the x-coordinate of the left edge of this Rect structure.
    /// </summary>
    public int Left
    {
        get => X;
        set => X = value;
    }

    /// <summary>
    /// Gets the x-coordinate that is the sum of X and Width property values of this Rect structure.
    /// </summary>
    public int Right => X + Width;
    
    /// <summary>
    /// 存放OCR识别的结果文本
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    public Region()
    {
    }
    
    public Region(int x, int y, int width, int height, Region? owner = null, INodeConverter? converter = null, DrawContent? drawContent = null)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Prev = owner;
        PrevConverter = converter;
        this.drawContent = drawContent ?? VisionContext.Instance().DrawContent;
    }
    
    public Region(Rect rect, Region? owner = null, INodeConverter? converter = null) : this(rect.X, rect.Y, rect.Width, rect.Height, owner, converter)
    {
    }
    
    public Region? Prev { get; }
    
    /// <summary>
    /// 本区域节点向上一个区域节点坐标的转换器
    /// </summary>
    public INodeConverter? PrevConverter { get; }
    
    /// <summary>
    /// 绘图上下文
    /// </summary>
    protected readonly DrawContent drawContent;

    public void Dispose()
    {
        
    }
}