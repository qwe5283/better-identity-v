using BetterIdentityV.Core.Geometry;

namespace VisualDebugValidation;

internal static partial class Program
{
    private static void CheckAspectRatioViewport()
    {
        var ratio = new ImageSize(16, 9);
        var horizontalCases = new[]
        {
            (Alignment: HorizontalViewportAlignment.Left, X: 0d, PixelX: 0d),
            (Alignment: HorizontalViewportAlignment.Center, X: 320.5, PixelX: 320d),
            (Alignment: HorizontalViewportAlignment.Right, X: 641d, PixelX: 641d)
        };
        var verticalCases = new[]
        {
            (Alignment: VerticalViewportAlignment.Top, Y: 0d, PixelY: 0d),
            (Alignment: VerticalViewportAlignment.Center, Y: 150.5, PixelY: 150d),
            (Alignment: VerticalViewportAlignment.Bottom, Y: 301d, PixelY: 301d)
        };
        foreach (var horizontal in horizontalCases)
        foreach (var vertical in verticalCases)
        {
            var wide = AspectRatioViewport.Calculate(new ImageSize(2561, 1080), ratio,
                horizontal.Alignment, vertical.Alignment);
            Assert(wide.Bounds == new RectD(horizontal.X, 0, 1920, 1080), "incorrect horizontal viewport alignment");
            Assert(wide.PixelBounds == new RectD(horizontal.PixelX, 0, 1920, 1080), "incorrect horizontal pixel alignment");

            var tall = AspectRatioViewport.Calculate(new ImageSize(1600, 1201), ratio,
                horizontal.Alignment, vertical.Alignment);
            Assert(tall.Bounds == new RectD(0, vertical.Y, 1600, 900), "incorrect vertical viewport alignment");
            Assert(tall.PixelBounds == new RectD(0, vertical.PixelY, 1600, 900), "incorrect vertical pixel alignment");
        }

        var portrait = AspectRatioViewport.Calculate(new ImageSize(1080, 1920), ratio,
            vertical: VerticalViewportAlignment.Bottom);
        Assert(portrait.Bounds == new RectD(0, 1312.5, 1080, 607.5), "precise portrait geometry was rounded");
        Assert(portrait.PixelBounds == new RectD(0, 1312, 1080, 608), "portrait pixel crop rounding changed");

        var nearRatio = AspectRatioViewport.Calculate(new ImageSize(1366, 768), ratio);
        Assert(nearRatio.PixelBounds == new RectD(0, 0, 1366, 768), "near-ratio pixel tolerance changed");
        Equal(nearRatio.Bounds.Width, 768 * 16d / 9, "precise near-ratio width");
        Equal(nearRatio.Bounds.X, (1366 - 768 * 16d / 9) / 2, "precise near-ratio center");

        var roundDown = AspectRatioViewport.Calculate(new ImageSize(8, 10), ratio,
            vertical: VerticalViewportAlignment.Bottom);
        Assert(roundDown.PixelBounds == new RectD(0, 6, 8, 4), "half-to-even crop rounding changed");
        var roundUp = AspectRatioViewport.Calculate(new ImageSize(24, 20), ratio,
            vertical: VerticalViewportAlignment.Bottom);
        Assert(roundUp.PixelBounds == new RectD(0, 6, 24, 14), "half-to-even crop rounding changed");

        var square = AspectRatioViewport.Calculate(new ImageSize(1600, 900), new ImageSize(1, 1));
        Assert(square.Bounds == new RectD(350, 0, 900, 900), "custom viewport aspect ratio failed");

        // 检查极小尺寸、奇偶边界和各种对齐的包含关系，像素裁剪不应越界或出现零尺寸。
        int[] dimensions = [1, 2, 7, 8, 9, 15, 16, 17, 1079, 1080, 1081, 1366];
        foreach (var width in dimensions)
        foreach (var height in dimensions)
        foreach (var horizontal in horizontalCases)
        foreach (var vertical in verticalCases)
        {
            var viewport = AspectRatioViewport.Calculate(new ImageSize(width, height), ratio,
                horizontal.Alignment, vertical.Alignment);
            CheckViewportBounds(viewport.Bounds, width, height);
            CheckViewportBounds(viewport.PixelBounds, width, height);
            Equal(viewport.Bounds.Width / viewport.Bounds.Height, 16d / 9, "precise viewport ratio");
            var pixel = viewport.PixelBounds;
            Assert(pixel.X == Math.Floor(pixel.X) && pixel.Y == Math.Floor(pixel.Y) &&
                   pixel.Width == Math.Floor(pixel.Width) && pixel.Height == Math.Floor(pixel.Height),
                "pixel viewport contains fractional boundaries");
        }

        ExpectInvalidViewport(() => AspectRatioViewport.Calculate(new ImageSize(0, 1080), ratio));
        ExpectInvalidViewport(() => AspectRatioViewport.Calculate(new ImageSize(1920, 1080), new ImageSize(16, 0)));
        ExpectInvalidViewport(() => AspectRatioViewport.Calculate(new ImageSize(1920, 1080), ratio,
            (HorizontalViewportAlignment)99));
        ExpectInvalidViewport(() => AspectRatioViewport.Calculate(new ImageSize(1920, 1080), ratio,
            vertical: (VerticalViewportAlignment)99));
        Console.WriteLine("PASS: shared viewport, all 9 alignments, exact/pixel bounds, odd dimensions, tolerance and input validation.");
    }

    private static void CheckViewportBounds(RectD bounds, int width, int height)
    {
        Assert(bounds.Width > 0 && bounds.Height > 0 && bounds.X >= 0 && bounds.Y >= 0 &&
               bounds.X + bounds.Width <= width + 1e-6 && bounds.Y + bounds.Height <= height + 1e-6,
            "viewport exceeded image bounds");
    }

    private static void ExpectInvalidViewport(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }
        throw new InvalidOperationException("viewport accepted invalid input");
    }
}
