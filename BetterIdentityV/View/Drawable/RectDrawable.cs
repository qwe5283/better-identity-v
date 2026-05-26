using System.Drawing;
using System.Windows;

namespace BetterIdentityV.View.Drawable;

[Serializable]
public class RectDrawable
{
    public string? Name { get; set; }
    public Rect Rect { get; }
    public Pen Pen { get; } = new(Color.Red, 2);
    
    public RectDrawable(Rect rect, Pen? pen = null, string? name = null)
    {
        Rect = rect;
        Name = name;

        if (pen != null)
        {
            Pen = pen;
        }
    }
    
    public RectDrawable(Rect rect, string? name)
    {
        Rect = rect;
        Name = name;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        RectDrawable other = (RectDrawable)obj;
        return Rect.Equals(other.Rect);
    }
    
    public override int GetHashCode()
    {
        return Rect.GetHashCode();
    }

    public bool IsEmpty => Rect.IsEmpty;
}