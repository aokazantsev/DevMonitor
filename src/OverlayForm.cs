using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DevMonitor
{
    internal sealed class OverlayForm : Form
    {
        private const int WsExLayered = 0x80000;
        private const int WsExToolWindow = 0x80;
        private const int WmNcLButtonDown = 0xA1;
        private const int WmExitSizeMove = 0x232;
        private const int HtCaption = 0x2;
        private const int RefreshIntervalMs = 1000;

        private const int WindowWidth = 232;
        private const int ContentPadding = 12;
        private const int RowHeight = 20;
        private const int GroupGap = 11;
        private const int CornerRadius = 14;
        private const float FontSizePx = 13f;

        private static readonly Color Background = Color.FromArgb(232, 246, 248, 251);
        private static readonly Color Border = Color.FromArgb(40, 15, 23, 42);
        private static readonly Color Divider = Color.FromArgb(36, 15, 23, 42);
        private static readonly Color LabelText = Color.FromArgb(255, 71, 85, 105);
        private static readonly Color NormalText = Color.FromArgb(255, 15, 23, 42);
        private static readonly Color WarningText = Color.FromArgb(255, 146, 64, 14);
        private static readonly Color CriticalText = Color.FromArgb(255, 185, 28, 28);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        private readonly MetricsCollector collector = new MetricsCollector();
        private readonly HistoryRecorder recorder = new HistoryRecorder();
        private readonly WeekDayVisibility weekDayVisibility = new WeekDayVisibility();
        private StatisticsForm statisticsForm;
        private SettingsForm settingsForm;
        private readonly WindowPositionStore positionStore = new WindowPositionStore();
        private readonly Timer refreshTimer = new Timer();
        private readonly float scale;
        private readonly Font labelFont;
        private readonly Font valueFont;
        private readonly Bitmap canvas;
        private readonly Graphics canvasGraphics;
        private readonly GraphicsPath backgroundShape;
        private readonly Brush backgroundBrush = new SolidBrush(Background);
        private readonly Brush labelBrush = new SolidBrush(LabelText);
        private readonly Brush normalBrush = new SolidBrush(NormalText);
        private readonly Brush warningBrush = new SolidBrush(WarningText);
        private readonly Brush criticalBrush = new SolidBrush(CriticalText);
        private readonly Pen borderPen;
        private readonly Pen dividerPen;
        private readonly StringFormat leftAligned = new StringFormat { LineAlignment = StringAlignment.Center };
        private readonly StringFormat segmentFormat = CreateSegmentFormat();
        private string lastSignature;
        private readonly NotifyIcon trayIcon = new NotifyIcon();
        private readonly ToolStripMenuItem toggleItem = new ToolStripMenuItem();
        private readonly ToolStripMenuItem startupItem = new ToolStripMenuItem("Запускать при входе в Windows");
        private bool? isStartupEnabled;
        private const int TrayTextLimit = 63;
        private const string TrayIconResource = "DevMonitor.app.ico";

        public OverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Text = "DevMonitor";

            scale = ReadDpiScale();
            labelFont = new Font("Segoe UI", FontSizePx * scale, FontStyle.Regular, GraphicsUnit.Pixel);
            valueFont = new Font("Segoe UI Semibold", FontSizePx * scale, FontStyle.Regular, GraphicsUnit.Pixel);
            Size = new Size(Scaled(WindowWidth), ContentHeight());
            Location = positionStore.Load(Size);
            ConfigureTrayIcon();

            canvas = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            canvasGraphics = Graphics.FromImage(canvas);
            canvasGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            canvasGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            backgroundShape = RoundedRectangle(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Scaled(CornerRadius));
            borderPen = new Pen(Border, Math.Max(1f, scale));
            dividerPen = new Pen(Divider, Math.Max(1f, scale));

            refreshTimer.Interval = RefreshIntervalMs;
            refreshTimer.Tick += OnRefreshTick;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WsExLayered | WsExToolWindow;
                return parameters;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            OnRefreshTick(this, EventArgs.Empty);
            refreshTimer.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, WmNcLButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
        }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if (message.Msg == WmExitSizeMove) positionStore.Save(Location);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                refreshTimer.Dispose();
                collector.Dispose();
                labelFont.Dispose();
                valueFont.Dispose();
                canvasGraphics.Dispose();
                canvas.Dispose();
                backgroundShape.Dispose();
                borderPen.Dispose();
                dividerPen.Dispose();
                foreach (Brush brush in new[] { backgroundBrush, labelBrush, normalBrush, warningBrush, criticalBrush })
                {
                    brush.Dispose();
                }
                leftAligned.Dispose();
                segmentFormat.Dispose();
            }
            base.Dispose(disposing);
        }

        private void OnRefreshTick(object sender, EventArgs e)
        {
            MetricsSnapshot snapshot = collector.Collect();
            recorder.Record(snapshot, DateTime.Now);
            List<List<MetricRow>> groups = MetricRowsFormatter.Format(snapshot);
            UpdateTrayText(groups);
            if (!Visible) return;
            string signature = Signature(groups);
            if (signature == lastSignature) return;
            lastSignature = signature;

            canvasGraphics.Clear(Color.Transparent);
            canvasGraphics.FillPath(backgroundBrush, backgroundShape);
            canvasGraphics.DrawPath(borderPen, backgroundShape);
            DrawGroups(groups);
            LayeredWindowPainter.Paint(Handle, Location, canvas);
        }

        private void DrawGroups(List<List<MetricRow>> groups)
        {
            float left = Scaled(ContentPadding);
            float right = Width - Scaled(ContentPadding);
            float rowHeight = Scaled(RowHeight);
            float gap = Scaled(GroupGap);
            float top = Scaled(ContentPadding);
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                if (groupIndex > 0)
                {
                    float dividerY = (float)Math.Round(top + gap / 2f) + 0.5f;
                    canvasGraphics.DrawLine(dividerPen, left, dividerY, right, dividerY);
                    top += gap;
                }
                foreach (MetricRow row in groups[groupIndex])
                {
                    var cell = new RectangleF(left, top, right - left, rowHeight);
                    canvasGraphics.DrawString(row.Label, labelFont, labelBrush, cell, leftAligned);
                    DrawSegmentsRightAligned(row.Segments, cell);
                    top += rowHeight;
                }
            }
        }

        private void DrawSegmentsRightAligned(ValueSegment[] segments, RectangleF cell)
        {
            float x = cell.Right;
            for (int index = segments.Length - 1; index >= 0; index--)
            {
                float width = canvasGraphics.MeasureString(segments[index].Text, valueFont, PointF.Empty, segmentFormat).Width;
                x -= width;
                var segmentCell = new RectangleF(x, cell.Top, width + 1f, cell.Height);
                canvasGraphics.DrawString(segments[index].Text, valueFont, BrushOf(segments[index].Severity), segmentCell, segmentFormat);
            }
        }

        private static StringFormat CreateSegmentFormat()
        {
            var format = (StringFormat)StringFormat.GenericTypographic.Clone();
            format.LineAlignment = StringAlignment.Center;
            format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap;
            return format;
        }

        private Brush BrushOf(Severity severity)
        {
            switch (severity)
            {
                case Severity.Critical:
                    return criticalBrush;
                case Severity.Warning:
                    return warningBrush;
                default:
                    return normalBrush;
            }
        }

        private static string Signature(List<List<MetricRow>> groups)
        {
            var builder = new StringBuilder();
            foreach (List<MetricRow> group in groups)
            {
                foreach (MetricRow row in group)
                {
                    builder.Append(row.Label);
                    foreach (ValueSegment segment in row.Segments)
                    {
                        builder.Append('|').Append(segment.Text).Append('#').Append((int)segment.Severity);
                    }
                    builder.Append(';');
                }
            }
            return builder.ToString();
        }

        private int ContentHeight()
        {
            int rows = 0;
            foreach (int size in MetricRowsFormatter.GroupSizes) rows += size;
            int gaps = MetricRowsFormatter.GroupSizes.Length - 1;
            return Scaled(ContentPadding * 2 + RowHeight * rows + GroupGap * gaps);
        }

        private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
        {
            float diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void ConfigureTrayIcon()
        {
            trayIcon.Icon = LoadTrayIcon();
            trayIcon.Text = Text;
            trayIcon.ContextMenuStrip = BuildMenu();
            trayIcon.MouseClick += OnTrayIconClick;
            trayIcon.Visible = true;
        }

        private static Icon LoadTrayIcon()
        {
            using (System.IO.Stream stream = typeof(OverlayForm).Assembly.GetManifestResourceStream(TrayIconResource))
            {
                if (stream != null) return new Icon(stream, SystemInformation.SmallIconSize);
            }
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            toggleItem.Click += delegate { ToggleOverlay(); };
            menu.Items.Add(toggleItem);
            menu.Items.Add("Статистика", null, delegate { ShowStatistics(); });
            menu.Items.Add("Настройки", null, delegate { ShowSettings(); });
            startupItem.Click += delegate { ToggleStartup(); };
            menu.Items.Add(startupItem);
            menu.Items.Add("О приложении…", null, delegate { AboutForm.ShowSingle(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Выход", null, delegate { Close(); });
            menu.Opening += delegate
            {
                toggleItem.Text = Visible ? "Свернуть в трей" : "Развернуть";
                if (!isStartupEnabled.HasValue) isStartupEnabled = StartupTask.IsEnabled();
                startupItem.Checked = isStartupEnabled.Value;
            };
            return menu;
        }

        private void OnTrayIconClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) ToggleOverlay();
        }

        private void ToggleOverlay()
        {
            if (Visible)
            {
                Hide();
                return;
            }
            Show();
            lastSignature = null;
            OnRefreshTick(this, EventArgs.Empty);
        }

        private void UpdateTrayText(List<List<MetricRow>> groups)
        {
            var builder = new StringBuilder(Text);
            foreach (List<MetricRow> group in groups)
            {
                MetricRow row = group[0];
                if (!HasAnyValue(row)) continue;
                builder.Append('\n').Append(row.Label).Append(' ');
                foreach (ValueSegment segment in row.Segments) builder.Append(segment.Text.Trim());
            }
            string text = builder.ToString().Replace("·", " · ");
            if (text.Length > TrayTextLimit) text = text.Substring(0, TrayTextLimit);
            if (trayIcon.Text != text) trayIcon.Text = text;
        }

        private static bool HasAnyValue(MetricRow row)
        {
            foreach (ValueSegment segment in row.Segments)
            {
                foreach (char symbol in segment.Text)
                {
                    if (char.IsDigit(symbol)) return true;
                }
            }
            return false;
        }

        private void ToggleStartup()
        {
            bool wasEnabled = StartupTask.IsEnabled();
            string problem = wasEnabled ? StartupTask.Disable() : StartupTask.Enable(Application.ExecutablePath);
            isStartupEnabled = StartupTask.IsEnabled();
            if (problem != null)
            {
                MessageBox.Show(problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string message = isStartupEnabled.Value
                ? "DevMonitor будет запускаться при входе в Windows (через 20 с, с правами администратора)."
                : "Автозапуск при входе в Windows выключен.";
            trayIcon.ShowBalloonTip(4000, Text, message, ToolTipIcon.Info);
        }

        private void ShowSettings()
        {
            if (settingsForm == null || settingsForm.IsDisposed)
            {
                settingsForm = new SettingsForm();
                settingsForm.Show();
                return;
            }
            if (settingsForm.WindowState == FormWindowState.Minimized) settingsForm.WindowState = FormWindowState.Normal;
            settingsForm.Activate();
        }

        private void ShowStatistics()
        {
            if (statisticsForm == null || statisticsForm.IsDisposed)
            {
                statisticsForm = new StatisticsForm(recorder, weekDayVisibility);
                statisticsForm.Show();
                return;
            }
            if (statisticsForm.WindowState == FormWindowState.Minimized) statisticsForm.WindowState = FormWindowState.Normal;
            statisticsForm.Activate();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            recorder.Flush();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            if (statisticsForm != null && !statisticsForm.IsDisposed) statisticsForm.Close();
            if (settingsForm != null && !settingsForm.IsDisposed) settingsForm.Close();
            base.OnFormClosing(e);
        }

        private float ReadDpiScale()
        {
            using (Graphics graphics = CreateGraphics())
            {
                return graphics.DpiX / 96f;
            }
        }

        private int Scaled(int value)
        {
            return (int)Math.Round(value * scale);
        }
    }
}
