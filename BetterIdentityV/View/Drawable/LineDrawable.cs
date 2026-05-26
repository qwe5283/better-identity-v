using System.Drawing;
using Point = System.Windows.Point;

namespace BetterIdentityV.View.Drawable;

[Serializable]
public class LineDrawable
{
    public Point P1 { get; set; }

    public Point P2 { get; set; }

    public Pen Pen { get; set; } = new(Color.Red, 2);

    public LineDrawable(double x1, double y1, double x2, double y2)
    {
        P1 = new Point(x1, y1);
        P2 = new Point(x2, y2);
    }

    public LineDrawable(Point p1, Point p2)
    {
        P1 = p1;
        P2 = p2;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        LineDrawable other = (LineDrawable)obj;
        return P1.Equals(other.P1) && P2.Equals(other.P2);
    }

    public override int GetHashCode()
    {
        return P1.GetHashCode() + P2.GetHashCode();
    }
}