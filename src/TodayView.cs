using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DevMonitor
{
    internal sealed class TodayView : UserControl
    {
        private const int CurrentRow = 0;
        private const int AverageRow = 1;
        private const int MaximumRow = 2;

        private readonly HistoryRecorder recorder;
        private readonly Label uptimeLabel = new Label();
        private readonly ComboBox metricBox;
        private readonly Label chartTitle;
        private readonly DataGridView table = new DataGridView();
        private readonly HistoryChart chart = new HistoryChart();

        public TodayView(HistoryRecorder recorder, Font font)
        {
            this.recorder = recorder;
            Font = font;
            Dock = DockStyle.Fill;
            BackColor = StatisticsStyle.Surface;
            metricBox = StatisticsStyle.CreateMetricBox(font);
            chartTitle = StatisticsStyle.CreateChartTitle(font);

            TableLayoutPanel layout = StatisticsStyle.CreatePageLayout(font, 7.5f);
            layout.Controls.Add(BuildToolbar(), 0, 0);
            layout.Controls.Add(BuildTable(), 0, 1);
            layout.Controls.Add(chartTitle, 0, 2);
            chart.Dock = DockStyle.Fill;
            chart.Font = font;
            layout.Controls.Add(chart, 0, 3);
            Controls.Add(layout);
            metricBox.SelectedIndexChanged += delegate { RefreshLive(); };
        }

        public void RefreshLive()
        {
            DateTime today = DateTime.Today;
            List<MinuteRecord> records = recorder.LoadToday();
            DaySummary summary = DaySummary.Of(records, today, today.AddDays(1));
            float?[] current = recorder.LastValues;

            uptimeLabel.Text = UptimeText(records, summary);
            for (int index = 0; index < MetricCatalog.Count; index++)
            {
                MetricKind kind = MetricCatalog.All[index];
                table.Rows[CurrentRow].Cells[1 + index].Value = StatisticsStyle.ValueCell(kind, current[index]);
                table.Rows[AverageRow].Cells[1 + index].Value = StatisticsStyle.ValueCell(kind, summary.Averages[index].Average);
                table.Rows[MaximumRow].Cells[1 + index].Value = StatisticsStyle.ValueCell(kind, summary.Maximums[index].Max);
            }

            MetricKind selected = MetricCatalog.All[Math.Max(0, metricBox.SelectedIndex)];
            chartTitle.Text = MetricCatalog.LongTitle(selected) + " — " + WeekDays.Label(today) + ", обновление каждую секунду";
            chart.DisplayDay(records, today, selected, recorder.PeakOf(selected), recorder.CommonDayWindow());
        }

        private Control BuildToolbar()
        {
            FlowLayoutPanel toolbar = StatisticsStyle.CreateToolbar(Font);
            uptimeLabel.AutoSize = true;
            uptimeLabel.ForeColor = StatisticsStyle.TextPrimary;
            uptimeLabel.Font = new Font(Font, FontStyle.Bold);
            uptimeLabel.Margin = new Padding(Font.Height / 2, Font.Height / 2, Font.Height, 0);
            toolbar.Controls.Add(uptimeLabel);
            toolbar.Controls.Add(StatisticsStyle.CreateCaption("График:", Font));
            toolbar.Controls.Add(metricBox);
            return toolbar;
        }

        private Control BuildTable()
        {
            StatisticsStyle.ConfigureTable(table, Font, false);
            StatisticsStyle.AddColumn(table, string.Empty, 14, DataGridViewContentAlignment.MiddleLeft);
            foreach (MetricKind kind in MetricCatalog.All)
            {
                StatisticsStyle.AddColumn(table, MetricCatalog.Title(kind), 11, DataGridViewContentAlignment.MiddleCenter);
            }
            table.Rows.Add("Сейчас");
            table.Rows.Add("Среднее за день");
            table.Rows.Add("Максимум за день");
            table.Rows[CurrentRow].DefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
            table.SelectionChanged += delegate { table.ClearSelection(); };
            return table;
        }

        private static string UptimeText(List<MinuteRecord> records, DaySummary summary)
        {
            if (summary.Minutes == 0) return "Сегодня данных пока нет";
            DateTime first = DateTime.MaxValue;
            foreach (MinuteRecord record in records)
            {
                if (record.Time < first) first = record.Time;
            }
            return "Сегодня: " + StatisticsStyle.Duration(summary.Minutes) + " данных, первая запись в " + first.ToString("HH:mm");
        }
    }
}
