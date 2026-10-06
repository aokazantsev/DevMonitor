using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace DevMonitor
{
    internal sealed class HistoryChart : Control
    {
        private const int DaysInWeek = 7;
        private const int MaxDayPoints = 480;
        private const int WeekBucketMinutes = 1;
        private const int MaxTimeLabels = 9;
        private const int GridLines = 4;
        private const float WeekLineWidth = 1.4f;
        private const float MaximumLightening = 0.55f;

        private static readonly int[] BucketSteps = { 1, 2, 5, 10, 15, 30, 60 };
        private static readonly int[] LabelSteps = { 5, 10, 15, 30, 60, 120, 180, 240 };

        private static readonly Color[] DayColors =
        {
            Color.FromArgb(37, 99, 235),
            Color.FromArgb(234, 88, 12),
            Color.FromArgb(22, 163, 74),
            Color.FromArgb(147, 51, 234),
            Color.FromArgb(219, 39, 119),
            Color.FromArgb(8, 145, 178),
            Color.FromArgb(133, 77, 14)
        };

        private static readonly Color Surface = Color.White;
        private static readonly Color Grid = Color.FromArgb(226, 232, 240);
        private static readonly Color Axis = Color.FromArgb(100, 116, 139);
        private static readonly Color HiddenDay = Color.FromArgb(148, 163, 184);
        private static readonly Color OverallAverage = Color.FromArgb(71, 85, 105);
        private static readonly Color WarningZone = Color.FromArgb(180, 83, 9);
        private static readonly Color CriticalZone = Color.FromArgb(185, 28, 28);

        private readonly List<KeyValuePair<RectangleF, DateTime>> legendHits = new List<KeyValuePair<RectangleF, DateTime>>();
        private List<MinuteRecord> records = new List<MinuteRecord>();
        private List<DateTime> days = new List<DateTime>();
        private DayWindow window = DayWindow.WholeDay;
        private int bucketMinutes = 1;
        private bool isWeek;
        private MetricKind kind;
        private float peak;

        private WeekDayVisibility visibility = new WeekDayVisibility();

        public WeekDayVisibility Visibility
        {
            get { return visibility; }
            set { visibility = value; }
        }

        public HistoryChart()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Surface;
        }

        public void DisplayWeek(List<MinuteRecord> source, DateTime weekStart, MetricKind metric, float scalePeak, DayWindow dayWindow)
        {
            var weekDays = new List<DateTime>();
            for (int day = 0; day < DaysInWeek; day++)
            {
                DateTime date = weekStart.AddDays(day);
                if (HasRecords(source, date)) weekDays.Add(date);
            }
            Apply(source, weekDays, metric, scalePeak, dayWindow, true);
        }

        public void DisplayDay(List<MinuteRecord> source, DateTime day, MetricKind metric, float scalePeak, DayWindow dayWindow)
        {
            var singleDay = new List<DateTime>();
            if (HasRecords(source, day)) singleDay.Add(day);
            Apply(source, singleDay, metric, scalePeak, dayWindow, false);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = HitLegend(e.Location).HasValue ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            DateTime? day = HitLegend(e.Location);
            if (!day.HasValue) return;
            visibility.Toggle(day.Value);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            graphics.Clear(Surface);
            legendHits.Clear();

            RectangleF plot = PlotArea();
            if (plot.Width <= 0 || plot.Height <= 0 || WindowMinutes() <= 0) return;

            int metric = (int)kind;
            var overall = new RunningStat();
            float observedMax = 0;
            foreach (MinuteRecord record in records)
            {
                if (!IsDrawn(record.Time)) continue;
                overall.Add(record.Average[metric]);
                if (record.Maximum[metric].HasValue) observedMax = Math.Max(observedMax, record.Maximum[metric].Value);
            }
            float scaleMax = MetricCatalog.ScaleMax(kind, Math.Max(peak, observedMax));
            DrawGrid(graphics, plot, scaleMax);
            if (isWeek) DrawWeekLegend(graphics, plot);

            if (overall.Count == 0)
            {
                bool isEverythingHidden = isWeek && days.Count > 0;
                DrawCentered(graphics, isEverythingHidden ? "Все дни скрыты — включи галочки в легенде" : "Нет данных за этот период", plot, Axis);
                return;
            }

            DrawTimeAxis(graphics, plot);
            DrawZones(graphics, plot, scaleMax);
            if (isWeek) DrawWeekSeries(graphics, plot, metric, scaleMax);
            else DrawDaySeries(graphics, plot, metric, scaleMax);
            DrawOverallAverage(graphics, plot, overall.Average.Value, scaleMax);
        }

        private void Apply(List<MinuteRecord> source, List<DateTime> drawnDays, MetricKind metric, float scalePeak, DayWindow dayWindow, bool week)
        {
            records = source;
            days = drawnDays;
            kind = metric;
            peak = scalePeak;
            window = dayWindow;
            isWeek = week;
            bucketMinutes = week ? WeekBucketMinutes : PickStep(BucketSteps, WindowMinutes(), MaxDayPoints);
            Invalidate();
        }

        private double WindowMinutes()
        {
            return (window.End - window.Start).TotalMinutes;
        }

        private bool IsDrawn(DateTime time)
        {
            TimeSpan timeOfDay = time.TimeOfDay;
            if (!days.Contains(time.Date) || timeOfDay < window.Start || timeOfDay >= window.End) return false;
            return !isWeek || visibility.IsVisible(time.Date);
        }

        private static bool HasRecords(List<MinuteRecord> source, DateTime day)
        {
            foreach (MinuteRecord record in source)
            {
                if (record.Time.Date == day) return true;
            }
            return false;
        }

        private void Bucketize(DateTime day, int metric, out RunningStat[] averages, out RunningStat[] maximums)
        {
            int count = (int)Math.Ceiling(WindowMinutes() / bucketMinutes);
            averages = NewStats(count);
            maximums = NewStats(count);
            DateTime start = day.Add(window.Start);
            DateTime end = day.Add(window.End);
            foreach (MinuteRecord record in records)
            {
                if (record.Time < start || record.Time >= end) continue;
                int bucket = (int)((record.Time - start).TotalMinutes / bucketMinutes);
                averages[bucket].Add(record.Average[metric]);
                maximums[bucket].Add(record.Maximum[metric]);
            }
        }

        private void DrawDaySeries(Graphics graphics, RectangleF plot, int metric, float scaleMax)
        {
            RunningStat[] averages;
            RunningStat[] maximums;
            Bucketize(days[0], metric, out averages, out maximums);
            Color dayColor = DayColors[WeekDays.IndexOf(days[0])];
            Color maximumColor = Lighten(dayColor, MaximumLightening);
            DrawSeries(graphics, plot, maximums, scaleMax, maximumColor, 1.2f, true);
            DrawSeries(graphics, plot, averages, scaleMax, dayColor, 1.8f, false);

            string interval = bucketMinutes == 1 ? "за минуту" : "за " + bucketMinutes + " мин";
            float x = plot.Left;
            x = DrawLegendItem(graphics, x, dayColor, "среднее " + interval);
            DrawLegendItem(graphics, x, maximumColor, "максимум " + interval);
        }

        private void DrawWeekSeries(Graphics graphics, RectangleF plot, int metric, float scaleMax)
        {
            foreach (DateTime day in days)
            {
                if (!visibility.IsVisible(day)) continue;
                RunningStat[] averages;
                RunningStat[] maximums;
                Bucketize(day, metric, out averages, out maximums);
                DrawSeries(graphics, plot, averages, scaleMax, DayColors[WeekDays.IndexOf(day)], WeekLineWidth, false);
            }
        }

        private void DrawWeekLegend(Graphics graphics, RectangleF plot)
        {
            float x = plot.Left;
            foreach (DateTime day in days)
            {
                float start = x;
                x = DrawCheckItem(graphics, x, DayColors[WeekDays.IndexOf(day)], WeekDays.Label(day), visibility.IsVisible(day));
                legendHits.Add(new KeyValuePair<RectangleF, DateTime>(new RectangleF(start, 0, x - start, Font.Height * 1.6f), day));
            }
        }

        private float DrawCheckItem(Graphics graphics, float x, Color color, string text, bool isChecked)
        {
            float size = Font.Height * 0.85f;
            float y = Font.Height * 0.3f;
            var box = new RectangleF(x, y + (Font.Height - size) / 2f, size, size);
            using (var border = new Pen(isChecked ? color : HiddenDay, 1.5f))
            using (var brush = new SolidBrush(isChecked ? Axis : HiddenDay))
            {
                if (isChecked)
                {
                    using (var fill = new SolidBrush(color))
                    using (var tick = new Pen(Surface, Math.Max(1.5f, size / 7f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    {
                        graphics.FillRectangle(fill, box);
                        graphics.DrawLines(tick, new[]
                        {
                            new PointF(box.Left + size * 0.22f, box.Top + size * 0.52f),
                            new PointF(box.Left + size * 0.42f, box.Top + size * 0.72f),
                            new PointF(box.Left + size * 0.78f, box.Top + size * 0.3f)
                        });
                    }
                }
                graphics.DrawRectangle(border, box.X, box.Y, box.Width, box.Height);
                graphics.DrawString(text, Font, brush, x + size + Font.Height * 0.35f, y);
                return x + size + Font.Height * 0.35f + graphics.MeasureString(text, Font).Width + Font.Height;
            }
        }

        private RectangleF PlotArea()
        {
            float unit = Font.Height;
            return new RectangleF(unit * 3f, unit * 1.6f, Width - unit * 4f, Height - unit * 3.4f);
        }

        private float XOfOffset(RectangleF plot, double minutesFromStart)
        {
            double clamped = Math.Max(0, Math.Min(WindowMinutes(), minutesFromStart));
            return plot.Left + (float)(plot.Width * clamped / WindowMinutes());
        }

        private DateTime? HitLegend(Point location)
        {
            foreach (KeyValuePair<RectangleF, DateTime> hit in legendHits)
            {
                if (hit.Key.Contains(location)) return hit.Value;
            }
            return null;
        }

        private void DrawGrid(Graphics graphics, RectangleF plot, float scaleMax)
        {
            using (var pen = new Pen(Grid))
            using (var brush = new SolidBrush(Axis))
            using (var format = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
            {
                for (int line = 0; line <= GridLines; line++)
                {
                    float value = scaleMax * line / GridLines;
                    float y = YOf(plot, value, scaleMax);
                    graphics.DrawLine(pen, plot.Left, y, plot.Right, y);
                    var labelCell = new RectangleF(0, y - Font.Height, plot.Left - Font.Height * 0.4f, Font.Height * 2);
                    graphics.DrawString(MetricCatalog.Format(kind, value), Font, brush, labelCell, format);
                }
            }
        }

        private void DrawTimeAxis(Graphics graphics, RectangleF plot)
        {
            int step = PickStep(LabelSteps, WindowMinutes(), MaxTimeLabels);
            using (var pen = new Pen(Grid))
            using (var brush = new SolidBrush(Axis))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
            {
                float labelTop = plot.Bottom + Font.Height * 0.3f;
                int startMinute = (int)window.Start.TotalMinutes;
                int firstMark = (startMinute + step - 1) / step * step;
                for (int minute = firstMark; minute <= window.End.TotalMinutes; minute += step)
                {
                    float x = XOfOffset(plot, minute - window.Start.TotalMinutes);
                    if (x > plot.Left + 1 && x < plot.Right - 1) graphics.DrawLine(pen, x, plot.Top, x, plot.Bottom);
                    string label = (minute / 60).ToString("00") + ":" + (minute % 60).ToString("00");
                    graphics.DrawString(label, Font, brush, new RectangleF(x - Font.Height * 2, labelTop, Font.Height * 4, Font.Height * 1.5f), format);
                }
            }
        }

        private void DrawZones(Graphics graphics, RectangleF plot, float scaleMax)
        {
            TemperatureZones zones = MetricCatalog.ZonesOf(kind);
            if (zones == null) return;
            DrawDashedLevel(graphics, plot, zones.Warning, scaleMax, WarningZone);
            DrawDashedLevel(graphics, plot, zones.Critical, scaleMax, CriticalZone);
        }

        private void DrawDashedLevel(Graphics graphics, RectangleF plot, float value, float scaleMax, Color color)
        {
            if (value > scaleMax) return;
            float y = YOf(plot, value, scaleMax);
            using (var pen = new Pen(Color.FromArgb(150, color)) { DashStyle = DashStyle.Dash })
            {
                graphics.DrawLine(pen, plot.Left, y, plot.Right, y);
            }
        }

        private void DrawSeries(Graphics graphics, RectangleF plot, RunningStat[] buckets, float scaleMax, Color color, float width, bool usesMaximum)
        {
            var points = new List<PointF>();
            using (var pen = new Pen(color, width * Font.Height / 15f) { LineJoin = LineJoin.Round })
            {
                for (int index = 0; index <= buckets.Length; index++)
                {
                    float? value = index < buckets.Length ? (usesMaximum ? buckets[index].Max : buckets[index].Average) : null;
                    if (value.HasValue)
                    {
                        points.Add(new PointF(XOfOffset(plot, bucketMinutes * (index + 0.5)), YOf(plot, value.Value, scaleMax)));
                        continue;
                    }
                    DrawPolyline(graphics, pen, points);
                    points.Clear();
                }
            }
        }

        private static void DrawPolyline(Graphics graphics, Pen pen, List<PointF> points)
        {
            if (points.Count == 1)
            {
                PointF point = points[0];
                graphics.DrawLine(pen, point.X - 1.5f, point.Y, point.X + 1.5f, point.Y);
            }
            else if (points.Count > 1)
            {
                graphics.DrawLines(pen, points.ToArray());
            }
        }

        private void DrawOverallAverage(Graphics graphics, RectangleF plot, float average, float scaleMax)
        {
            float y = YOf(plot, average, scaleMax);
            using (var pen = new Pen(Color.FromArgb(170, OverallAverage)) { DashStyle = DashStyle.Dot, Width = 1.5f })
            using (var brush = new SolidBrush(OverallAverage))
            using (var format = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far })
            {
                graphics.DrawLine(pen, plot.Left, y, plot.Right, y);
                string label = (isWeek ? "среднее за неделю " : "среднее ") + MetricCatalog.Format(kind, average);
                SizeF size = graphics.MeasureString(label, Font);
                var labelCell = new RectangleF(plot.Right - size.Width - Font.Height * 0.3f, y - size.Height - Font.Height * 0.15f, size.Width, size.Height);
                using (var backdrop = new SolidBrush(Color.FromArgb(225, Surface)))
                {
                    graphics.FillRectangle(backdrop, labelCell);
                }
                graphics.DrawString(label, Font, brush, labelCell, format);
            }
        }

        private float DrawLegendItem(Graphics graphics, float x, Color color, string text)
        {
            float y = Font.Height * 0.3f;
            float lineLength = Font.Height * 1.4f;
            float middle = y + Font.Height / 2f;
            using (var pen = new Pen(color, 2.5f))
            using (var brush = new SolidBrush(Axis))
            {
                graphics.DrawLine(pen, x, middle, x + lineLength, middle);
                graphics.DrawString(text, Font, brush, x + lineLength + Font.Height * 0.3f, y);
                return x + lineLength + graphics.MeasureString(text, Font).Width + Font.Height * 1.2f;
            }
        }

        private void DrawCentered(Graphics graphics, string text, RectangleF area, Color color)
        {
            using (var brush = new SolidBrush(color))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                graphics.DrawString(text, Font, brush, area, format);
            }
        }

        private static int PickStep(int[] steps, double spanMinutes, int maxCount)
        {
            foreach (int step in steps)
            {
                if (spanMinutes / step <= maxCount) return step;
            }
            return steps[steps.Length - 1];
        }

        private static Color Lighten(Color color, float amount)
        {
            return Color.FromArgb(
                (int)(color.R + (255 - color.R) * amount),
                (int)(color.G + (255 - color.G) * amount),
                (int)(color.B + (255 - color.B) * amount));
        }

        private static float YOf(RectangleF plot, float value, float scaleMax)
        {
            float clamped = Math.Max(0f, Math.Min(scaleMax, value));
            return plot.Bottom - plot.Height * clamped / scaleMax;
        }

        private static RunningStat[] NewStats(int count)
        {
            var stats = new RunningStat[count];
            for (int index = 0; index < count; index++) stats[index] = new RunningStat();
            return stats;
        }
    }
}
