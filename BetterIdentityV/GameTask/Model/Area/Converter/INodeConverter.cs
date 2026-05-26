namespace BetterIdentityV.GameTask.Model.Area.Converter;

public interface INodeConverter
{
    public (int x, int y, int w, int h) ToPrev(int x, int y, int w, int h);
}