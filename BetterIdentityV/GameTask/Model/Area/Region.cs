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
    
    /// <summary>
    /// 直接在遮罩窗口绘制【自己】
    /// </summary>
    /// <param name="name"></param>
    /// <param name="pen"></param>
    /// <param name="confidence"></param>
    public void DrawSelf(string name, Pen? pen = null, double? confidence = null)
    {
        // 相对自己是 0, 0 坐标
        DrawRect(0, 0, Width, Height, name, pen, confidence);
    }
    
    /// <summary>
    /// 直接在遮罩窗口绘制当前区域下的【指定区域】
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="name"></param>
    /// <param name="pen"></param>
    /// <param name="confidence"></param>
    public void DrawRect(int x, int y, int w, int h, string name, Pen? pen = null, double? confidence = null)
    {
        var drawable = ToRectDrawable(x, y, w, h, name, pen);
        drawContent.PutRect(name, drawable, confidence);
    }

    public void DrawRect(Rect rect, string name, Pen? pen = null, double? confidence = null)
    {
        var drawable = ToRectDrawable(rect.X, rect.Y, rect.Width, rect.Height, name, pen);
        drawContent.PutRect(name, drawable, confidence);
    }
    
    /// <summary>
    /// 转换【指定区域】到遮罩窗口绘制矩形
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="name"></param>
    /// <param name="pen"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public RectDrawable ToRectDrawable(int x, int y, int w, int h, string name, Pen? pen = null)
    {
        var res = ConvertRes<GameCaptureRegion>.ConvertPositionToTargetRegion(x, y, w, h, this);
        return res.TargetRegion.ConvertToRectDrawable(res.X, res.Y, res.Width, res.Height, pen, name);
    }
    
    public bool IsEmpty()
    {
        return Width == 0 && Height == 0 && X == 0 && Y == 0;
    }
    
    /// <summary>
    /// 语义化包装
    /// </summary>
    /// <returns></returns>
    public bool IsExist()
    {
        return !IsEmpty();
    }

    public void Dispose()
    {
    }
    
    
    /// <summary>
    /// 派生一个点类型的区域
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public Region Derive(int x, int y)
    {
        return Derive(x, y, 0, 0);
    }

    /// <summary>
    /// 派生一个矩形类型的区域
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <returns></returns>
    public Region Derive(int x, int y, int w, int h)
    {
        return new Region(x, y, w, h, this, new TranslationConverter(x, y), this.drawContent);
    }

    public Region Derive(Rect rect)
    {
        return Derive(rect.X, rect.Y, rect.Width, rect.Height);
    }
}