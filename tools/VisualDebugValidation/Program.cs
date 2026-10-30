using System.Collections.Immutable;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BetterIdentityV.Core.Diagnostics;
using BetterIdentityV.Core.Geometry;
using BetterIdentityV.GameTask.AutoTarget;
using BetterIdentityV.View.DebugOverlay;

namespace VisualDebugValidation;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            CheckGeometryAndMapping();
            CheckSessionLifecycle();
            CheckRendering(args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "artifacts"));
            CheckWindowLifecycle();
            Console.WriteLine("PASS: all visual debug checks completed.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void CheckGeometryAndMapping()
    {
        var cases = new[]
        {
            (Size: new ImageSize(1280, 720), Center: new PointD(640, 317)),
            (Size: new ImageSize(1920, 1080), Center: new PointD(960, 475.5)),
            (Size: new ImageSize(2560, 1080), Center: new PointD(1280, 475.5)),
            (Size: new ImageSize(1600, 1200), Center: new PointD(800, 546.25))
        };
        foreach (var item in cases)
        {
            var geometry = AutoTargetGeometry.Calculate(item.Size, 30);
            Equal(geometry.Deadzone.Center.X, item.Center.X, "deadzone center X");
            Equal(geometry.Deadzone.Center.Y, item.Center.Y, "deadzone center Y");
            Assert(geometry.Deadzone.Contains(new PointD(item.Center.X + 30, item.Center.Y)),
                "deadzone must include its boundary");
            Assert(!geometry.Deadzone.Contains(new PointD(item.Center.X + 30.01, item.Center.Y)),
                "deadzone must exclude points outside its boundary");

            foreach (var dpi in new[] { 1d, 1.25, 1.5, 2 })
            {
                var viewport = new OverlayViewport(item.Size, item.Size.Width / dpi,
                    item.Size.Height / dpi, dpi);
                var mapper = new CoordinateMapper(item.Size, viewport);
                var center = mapper.Map(geometry.Deadzone.Center);
                Equal(center.X * dpi, item.Center.X, "DPI mapping X");
                Equal(center.Y * dpi, item.Center.Y, "DPI mapping Y");
            }
        }

        // 归一化空间的横纵比例独立映射，不能用一个比例代替实际 resize。
        var normalized = new CoordinateMapper(new ImageSize(1920, 1080),
            new OverlayViewport(new ImageSize(1366, 768), 1366, 768, 1));
        Equal(normalized.Map(new PointD(1920, 1080)).X, 1366, "normalized right edge");
        Equal(normalized.Map(new PointD(1920, 1080)).Y, 768, "normalized bottom edge");
        Console.WriteLine("PASS: geometry, deadzone decisions, image mapping and 100%-200% DPI.");
    }

    private static void CheckSessionLifecycle()
    {
        var hub = new DebugHub();
        var key = new StateKey<Counter>("Counter");
        using var first = hub.OpenSession("same source");
        using var second = hub.OpenSession("same source");
        first.Set(key, new Counter(1));
        second.Set(key, new Counter(2));
        var captured = first.Capture();

        // UI 线程同步等待后台发布；如果写入路径等待 Dispatcher，此验证会超时。
        var writer = Task.Run(() =>
        {
            for (var index = 0; index < 10000; index++)
                first.Set(key, new Counter(index));
        });
        Assert(writer.Wait(TimeSpan.FromSeconds(5)), "diagnostic publication blocked on UI");
        Equal(first.Capture().Get(key)!.Value, 9999, "only the latest state is retained");
        Equal(captured.Get(key)!.Value, 1, "captured snapshots must stay unchanged");
        Equal(second.Capture().Get(key)!.Value, 2, "same-name sessions must be isolated");

        using var releaseLateWriter = new ManualResetEventSlim();
        var lateWriter = Task.Run(() =>
        {
            releaseLateWriter.Wait();
            return first.Set(key, new Counter(-1));
        });
        first.Dispose();
        releaseLateWriter.Set();
        Assert(lateWriter.Wait(TimeSpan.FromSeconds(5)) && !lateWriter.Result,
            "a closed session accepted an in-flight result");
        Assert(first.Capture().IsClosed && first.Capture().Get(key) is null,
            "closed sessions must release their states");
        Assert(hub.GetSessions().Count == 1, "closed session remained registered");
        Console.WriteLine("PASS: latest snapshots, thread independence, session isolation and late-write rejection.");
    }

    private static void CheckRendering(string artifactPath)
    {
        var size = new ImageSize(1920, 1080);
        using var fixture = new RenderFixture(size, 960, 540);
        using var session = fixture.Hub.OpenSession(AutoTargetDebugState.Source);
        var geometry = AutoTargetGeometry.Calculate(size, 90);
        var detections = ImmutableArray.Create(
            new DetectionAnnotation(new RectD(340, 300, 230, 360), 0, "目标 A", 0.93, true),
            new DetectionAnnotation(new RectD(1220, 270, 230, 320), 3, "目标 B", 0.87, false),
            new DetectionAnnotation(new RectD(1760, 0, 230, 160), 4, "顶部及右侧边界标签", 0.81, false));
        session.Set(AutoTargetDebugState.Geometry, geometry);
        session.Set(AutoTargetDebugState.Frame, Frame(1, geometry, detections));
        fixture.Controller.Refresh();

        var bitmap = fixture.Render();
        Assert(Alpha(bitmap, 170, 230) > 0, "selected detection border was not rendered");
        Assert(Alpha(bitmap, 180, 145) > 0, "label background was not rendered above the box");
        Assert(Alpha(bitmap, 900, 4) > 0, "top-edge label was clipped off-screen");
        Assert(Alpha(bitmap, 525, 238) > 0, "deadzone boundary was not rendered");
        Directory.CreateDirectory(artifactPath);
        fixture.SavePreview(Path.Combine(artifactPath, "autotarget-overlay.png"));

        session.Set(AutoTargetDebugState.Frame, Frame(2, geometry, []));
        fixture.Controller.Refresh();
        bitmap = fixture.Render();
        Assert(Alpha(bitmap, 170, 230) == 0 && Alpha(bitmap, 180, 145) == 0,
            "empty detections left a box or label behind");
        Assert(Alpha(bitmap, 525, 238) > 0, "empty detections removed the ROI");

        // 只更新配置几何时，旧帧和旧标签不得继续显示。
        session.Set(AutoTargetDebugState.Frame, Frame(3, geometry, detections));
        fixture.Controller.Refresh();
        var changedGeometry = AutoTargetGeometry.Calculate(size, 120);
        session.Set(AutoTargetDebugState.Geometry, changedGeometry);
        fixture.Controller.Refresh();
        bitmap = fixture.Render();
        Assert(Alpha(bitmap, 170, 230) == 0, "old detections survived a geometry change");
        Assert(Alpha(bitmap, 540, 238) > 0, "updated ROI radius did not take effect");

        // 实际捕获尺寸变化后，旧尺寸的几何和检测都必须隐藏。
        fixture.Control.CaptureSize = new ImageSize(1366, 768);
        fixture.Controller.Refresh();
        Assert(IsEmpty(fixture.Render()), "old-resolution overlay was stretched into the new viewport");
        var resizedGeometry = AutoTargetGeometry.Calculate(fixture.Control.CaptureSize, 60);
        session.Set(AutoTargetDebugState.Geometry, resizedGeometry);
        session.Set(AutoTargetDebugState.Frame, Frame(4, resizedGeometry, []));
        fixture.Controller.Refresh();
        Assert(!IsEmpty(fixture.Render()), "new-resolution overlay failed to recover");
        session.Dispose();
        fixture.Controller.Refresh();
        Assert(IsEmpty(fixture.Render()), "closed session left visible graphics behind");

        // 使用实际 WPF 位图渲染验证缩放后的物理像素位置。
        foreach (var dpi in new[] { 1d, 1.5, 2 })
        {
            using var scaled = new RenderFixture(size, size.Width / dpi, size.Height / dpi);
            using var scaledSession = scaled.Hub.OpenSession(AutoTargetDebugState.Source);
            scaledSession.Set(AutoTargetDebugState.Geometry, geometry);
            scaledSession.Set(AutoTargetDebugState.Frame, Frame(1, geometry, detections));
            scaled.Controller.Refresh();
            var scaledBitmap = scaled.Render(dpi);
            Assert(Alpha(scaledBitmap, 340, 460) > 0, "DPI-scaled border shifted in physical pixels");
            Assert(Alpha(scaledBitmap, 1050, 476) > 0, "DPI-scaled ROI shifted in physical pixels");
        }
        Console.WriteLine("PASS: WPF boxes, labels, ROI, empty frames, resize, cleanup and rasterized DPI mapping.");
        Console.WriteLine($"Preview: {Path.GetFullPath(Path.Combine(artifactPath, "autotarget-overlay.png"))}");
    }

    private static AutoTargetFrameSnapshot Frame(long id, AutoTargetGeometry geometry,
        ImmutableArray<DetectionAnnotation> detections) => new(id, DateTimeOffset.UtcNow, geometry, detections);

    private static void CheckWindowLifecycle()
    {
        var hub = new DebugHub();
        var size = new ImageSize(1280, 720);
        var control = new DebugOverlayControl { CaptureSize = size };
        using var controller = new DebugOverlayController(hub, control);
        // 在屏幕外创建真实 WPF 宿主，验证 Loaded、可见性和定时刷新，不激活用户窗口。
        var window = new Window
        {
            Content = control, Width = 640, Height = 360,
            Left = -32000, Top = -32000, WindowStyle = WindowStyle.None,
            ShowInTaskbar = false, ShowActivated = false
        };
        try
        {
            using var session = hub.OpenSession(AutoTargetDebugState.Source);
            var geometry = AutoTargetGeometry.Calculate(size, 60);
            session.Set(AutoTargetDebugState.Geometry, geometry);
            window.Show();
            PumpDispatcher();
            Assert(VisualTreeHelper.GetChildrenCount(control) == 2,
                "loaded host failed to attach independent visual layers");

            control.Visibility = Visibility.Collapsed;
            session.Dispose();
            control.Visibility = Visibility.Visible;
            PumpDispatcher();
            Assert(VisualTreeHelper.GetChildrenCount(control) == 0,
                "showing the overlay restored a closed session");

            using var resumed = hub.OpenSession(AutoTargetDebugState.Source);
            resumed.Set(AutoTargetDebugState.Geometry, geometry);
            PumpDispatcher();
            Assert(VisualTreeHelper.GetChildrenCount(control) == 2,
                "timer did not discover the new session");
            controller.Dispose();
            Assert(VisualTreeHelper.GetChildrenCount(control) == 0,
                "controller shutdown left visual layers attached");
        }
        finally
        {
            window.Close();
        }
        Console.WriteLine("PASS: real WPF host loading, timer, visibility changes and controller shutdown.");
    }

    private static void PumpDispatcher()
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    private static byte Alpha(BitmapSource bitmap, int x, int y)
    {
        var pixel = new byte[4];
        bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
        return pixel[3];
    }

    private static bool IsEmpty(BitmapSource bitmap)
    {
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        for (var offset = 3; offset < pixels.Length; offset += 4)
            if (pixels[offset] != 0)
                return false;
        return true;
    }

    private static void Equal(double actual, double expected, string message) =>
        Assert(Math.Abs(actual - expected) < 0.000001, $"{message}: expected {expected}, got {actual}");

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed record Counter(int Value);

    private sealed class RenderFixture : IDisposable
    {
        public DebugHub Hub { get; } = new();
        public DebugOverlayControl Control { get; }
        public DebugOverlayController Controller { get; }

        public RenderFixture(ImageSize size, double width, double height)
        {
            Control = new DebugOverlayControl { CaptureSize = size, Width = width, Height = height };
            Control.Measure(new Size(width, height));
            Control.Arrange(new Rect(0, 0, width, height));
            Control.UpdateLayout();
            Controller = new DebugOverlayController(Hub, Control);
        }

        public RenderTargetBitmap Render(double dpi = 1)
        {
            var bitmap = new RenderTargetBitmap((int)Math.Round(Control.Width * dpi),
                (int)Math.Round(Control.Height * dpi), 96 * dpi, 96 * dpi, PixelFormats.Pbgra32);
            bitmap.Render(Control);
            return bitmap;
        }

        public void SavePreview(string path)
        {
            var background = new DrawingVisual();
            using (var context = background.RenderOpen())
            {
                context.DrawRectangle(new SolidColorBrush(Color.FromRgb(20, 24, 32)), null,
                    new Rect(0, 0, Control.Width, Control.Height));
                var grid = new Pen(new SolidColorBrush(Color.FromRgb(34, 40, 50)), 1);
                for (var x = 0; x < Control.Width; x += 40)
                    context.DrawLine(grid, new Point(x, 0), new Point(x, Control.Height));
                for (var y = 0; y < Control.Height; y += 40)
                    context.DrawLine(grid, new Point(0, y), new Point(Control.Width, y));
            }
            var bitmap = new RenderTargetBitmap((int)Control.Width, (int)Control.Height,
                96, 96, PixelFormats.Pbgra32);
            bitmap.Render(background);
            bitmap.Render(Control);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(path);
            encoder.Save(stream);
        }

        public void Dispose() => Controller.Dispose();
    }
}
