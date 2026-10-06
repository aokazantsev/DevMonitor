using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace DevMonitor
{
    internal static class GuideShots
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [STAThread]
        private static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (o, e) => { File.WriteAllText("error.txt", e.Exception.ToString()); Environment.Exit(1); };
            Application.EnableVisualStyles();

            var overlay = new OverlayForm();
            overlay.Location = new Point(-4000, -4000);
            var recorder = new HistoryRecorder();
            int step = 0;
            StatisticsForm statistics = null;
            SettingsForm settings = null;
            ContextMenuStrip menu = null;
            var timer = new Timer { Interval = 900 };
            timer.Tick += delegate
            {
                FeedRecorder(recorder);
                switch (step)
                {
                    case 0:
                        SaveOverlay(overlay);
                        menu = ((NotifyIcon)typeof(OverlayForm).GetField("trayIcon", Private).GetValue(overlay)).ContextMenuStrip;
                        menu.Show(new Point(300, 300));
                        break;
                    case 1:
                        SaveMenu(menu);
                        menu.Close();
                        statistics = new StatisticsForm(recorder, new WeekDayVisibility());
                        statistics.Show();
                        break;
                    case 3:
                        Capture(statistics, "stats_today.png");
                        Find<TabControl>(statistics).SelectedIndex = 1;
                        break;
                    case 4:
                        Capture(statistics, "stats_week.png");
                        statistics.Close();
                        settings = new SettingsForm();
                        settings.Show();
                        break;
                    case 5:
                        Capture(settings, "settings.png");
                        settings.Close();
                        break;
                    case 6:
                        overlay.Close();
                        break;
                }
                step++;
            };
            overlay.Shown += delegate { timer.Start(); };
            Application.Run(overlay);
        }

        private static void FeedRecorder(HistoryRecorder recorder)
        {
            recorder.Record(FakeSnapshot(), DateTime.Now);
        }

        private static MetricsSnapshot FakeSnapshot()
        {
            return new MetricsSnapshot
            {
                CpuLoad = 82,
                CpuTemperature = 71,
                Gpu = new GpuReading { Load = 18, Temperature = 44, VramUsedBytes = (ulong)(1.6 * (1UL << 30)), VramTotalBytes = 8UL << 30 },
                HasRam = true,
                RamUsedBytes = (ulong)(21.4 * (1UL << 30)),
                RamTotalBytes = 32UL << 30,
                Processes = new ProcessUsageReport(
                    new ProcessGroupUsage(1, (long)(2.3 * (1L << 30))),
                    new ProcessGroupUsage(1, (long)(5.7 * (1L << 30))),
                    new ProcessGroupUsage(1, (long)(1.2 * (1L << 30))),
                    new ProcessGroupUsage(6, (long)(2.6 * (1L << 30))))
            };
        }

        private static void SaveOverlay(OverlayForm overlay)
        {
            var canvas = (Bitmap)typeof(OverlayForm).GetField("canvas", Private).GetValue(overlay);
            var graphics = (Graphics)typeof(OverlayForm).GetField("canvasGraphics", Private).GetValue(overlay);
            graphics.Clear(Color.Transparent);
            graphics.FillPath((Brush)typeof(OverlayForm).GetField("backgroundBrush", Private).GetValue(overlay),
                (GraphicsPath)typeof(OverlayForm).GetField("backgroundShape", Private).GetValue(overlay));
            graphics.DrawPath((Pen)typeof(OverlayForm).GetField("borderPen", Private).GetValue(overlay),
                (GraphicsPath)typeof(OverlayForm).GetField("backgroundShape", Private).GetValue(overlay));
            List<List<MetricRow>> groups = MetricRowsFormatter.Format(FakeSnapshot());
            typeof(OverlayForm).GetMethod("DrawGroups", Private).Invoke(overlay, new object[] { groups });

            const int margin = 36;
            using (var scene = new Bitmap(canvas.Width + margin * 2, canvas.Height + margin * 2))
            using (Graphics sceneGraphics = Graphics.FromImage(scene))
            using (var backdrop = new LinearGradientBrush(new Rectangle(0, 0, scene.Width, scene.Height),
                Color.FromArgb(71, 85, 105), Color.FromArgb(30, 41, 59), 60f))
            {
                sceneGraphics.FillRectangle(backdrop, 0, 0, scene.Width, scene.Height);
                sceneGraphics.DrawImage(canvas, margin, margin);
                scene.Save("overlay.png", ImageFormat.Png);
            }
        }

        private static void SaveMenu(ContextMenuStrip menu)
        {
            const int margin = 24;
            using (var menuImage = new Bitmap(menu.Width, menu.Height))
            {
                menu.DrawToBitmap(menuImage, new Rectangle(0, 0, menu.Width, menu.Height));
                using (var scene = new Bitmap(menu.Width + margin * 2, menu.Height + margin * 2))
                using (Graphics graphics = Graphics.FromImage(scene))
                using (var backdrop = new LinearGradientBrush(new Rectangle(0, 0, scene.Width, scene.Height),
                    Color.FromArgb(71, 85, 105), Color.FromArgb(30, 41, 59), 60f))
                using (var shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                {
                    graphics.FillRectangle(backdrop, 0, 0, scene.Width, scene.Height);
                    graphics.FillRectangle(shadow, margin + 4, margin + 4, menu.Width, menu.Height);
                    graphics.DrawImage(menuImage, margin, margin);
                    scene.Save("tray_menu.png", ImageFormat.Png);
                }
            }
        }

        private static void CaptureScreen(Rectangle bounds, int padding, string name)
        {
            Rectangle area = Rectangle.Inflate(bounds, padding, padding);
            using (var bitmap = new Bitmap(area.Width, area.Height))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
                bitmap.Save(name, ImageFormat.Png);
            }
        }

        private static void Capture(Form form, string name)
        {
            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                bitmap.Save(name, ImageFormat.Png);
            }
        }

        private static T Find<T>(Control root) where T : Control
        {
            foreach (Control child in root.Controls)
            {
                if (child is T) return (T)child;
                T nested = Find<T>(child);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
