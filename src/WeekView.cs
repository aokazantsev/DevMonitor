using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DevMonitor
{
    internal sealed class WeekView : UserControl
    {
        private const int DaysInWeek = 7;
        private const int TotalRowIndex = DaysInWeek;

        private readonly HistoryRecorder recorder;
        private readonly Button previousWeekButton = new Button();
        private readonly Button nextWeekButton = new Button();
        private readonly Label rangeLabel = new Label();
        private readonly ComboBox metricBox;
        private readonly Label chartTitle;
        private readonly DataGridView table = new DataGridView();
        private readonly HistoryChart chart = new HistoryChart();
        private DateTime weekStart = WeekDays.WeekStart(DateTime.Today);
        private int selectedRow = TotalRowIndex;
        private List<MinuteRecord> records = new List<MinuteRecord>();
        private bool isFillingTable;

        public WeekView(HistoryRecorder recorder, WeekDayVisibility visibility, Font font)
        {
            this.recorder = recorder;
            chart.Visibility = visibility;
            Font = font;
            Dock = DockStyle.Fill;
            BackColor = StatisticsStyle.Surface;
            metricBox = StatisticsStyle.CreateMetricBox(font);
            chartTitle = StatisticsStyle.CreateChartTitle(font);

            TableLayoutPanel layout = StatisticsStyle.CreatePageLayout(font, 16f);
            layout.Controls.Add(BuildToolbar(), 0, 0);
            layout.Controls.Add(BuildTable(), 0, 1);
            layout.Controls.Add(chartTitle, 0, 2);
            chart.Dock = DockStyle.Fill;
            chart.Font = font;
            layout.Controls.Add(chart, 0, 3);
            Controls.Add(layout);
            metricBox.SelectedIndexChanged += delegate { UpdateChart(); };
        }

        public void Reload()
        {
            records = recorder.Load(weekStart, DaysInWeek);
            DateTime currentWeek = WeekDays.WeekStart(DateTime.Today);
            DateTime oldestWeek = HistoryStore.OldestKeptDay(DateTime.Today);
            previousWeekButton.Enabled = weekStart > oldestWeek;
            nextWeekButton.Enabled = weekStart < currentWeek;
            rangeLabel.Text = weekStart.ToString("dd.MM") + " – " + weekStart.AddDays(DaysInWeek - 1).ToString("dd.MM.yyyy");
            FillTable();
            UpdateChart();
        }

        private Control BuildToolbar()
        {
            FlowLayoutPanel toolbar = StatisticsStyle.CreateToolbar(Font);
            ConfigureButton(previousWeekButton, "‹ Прошлая неделя");
            previousWeekButton.Click += delegate { ChangeWeek(-DaysInWeek); };
            ConfigureButton(nextWeekButton, "Следующая неделя ›");
            nextWeekButton.Click += delegate { ChangeWeek(DaysInWeek); };
            rangeLabel.AutoSize = true;
            rangeLabel.ForeColor = StatisticsStyle.TextPrimary;
            rangeLabel.Font = new Font(Font, FontStyle.Bold);
            rangeLabel.Margin = new Padding(Font.Height, Font.Height / 2, Font.Height, 0);

            toolbar.Controls.Add(previousWeekButton);
            toolbar.Controls.Add(rangeLabel);
            toolbar.Controls.Add(nextWeekButton);
            toolbar.Controls.Add(StatisticsStyle.CreateCaption("График:", Font));
            toolbar.Controls.Add(metricBox);
            return toolbar;
        }

        private static void ConfigureButton(Button button, string text)
        {
            button.Text = text;
            button.AutoSize = true;
            button.FlatStyle = FlatStyle.System;
        }

        private Control BuildTable()
        {
            StatisticsStyle.ConfigureTable(table, Font, true);
            StatisticsStyle.AddColumn(table, "День", 14, DataGridViewContentAlignment.MiddleLeft);
            StatisticsStyle.AddColumn(table, "Работал", 10, DataGridViewContentAlignment.MiddleCenter);
            foreach (MetricKind kind in MetricCatalog.All)
            {
                StatisticsStyle.AddColumn(table, MetricCatalog.Title(kind) + Environment.NewLine + "ср / макс", 11, DataGridViewContentAlignment.MiddleCenter);
            }
            for (int row = 0; row <= TotalRowIndex; row++) table.Rows.Add();
            table.Rows[TotalRowIndex].DefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
            table.Rows[TotalRowIndex].DefaultCellStyle.BackColor = StatisticsStyle.Toolbar;
            table.SelectionChanged += OnTableSelectionChanged;
            return table;
        }

        private void ChangeWeek(int dayOffset)
        {
            weekStart = weekStart.AddDays(dayOffset);
            selectedRow = TotalRowIndex;
            Reload();
        }

        private void FillTable()
        {
            isFillingTable = true;
            for (int day = 0; day < DaysInWeek; day++)
            {
                DateTime date = weekStart.AddDays(day);
                string label = WeekDays.Label(date) + (date == DateTime.Today ? "  (сегодня)" : string.Empty);
                FillRow(table.Rows[day], label, DaySummary.Of(records, date, date.AddDays(1)), date > DateTime.Today);
            }
            FillRow(table.Rows[TotalRowIndex], "Итого за неделю", DaySummary.Of(records, weekStart, weekStart.AddDays(DaysInWeek)), false);
            table.ClearSelection();
            table.CurrentCell = table.Rows[selectedRow].Cells[0];
            table.Rows[selectedRow].Selected = true;
            isFillingTable = false;
        }

        private static void FillRow(DataGridViewRow row, string label, DaySummary summary, bool isFuture)
        {
            row.Cells[0].Value = label;
            row.Cells[1].Value = isFuture || summary.Minutes == 0 ? StatisticsStyle.NoValue : StatisticsStyle.Duration(summary.Minutes);
            for (int index = 0; index < MetricCatalog.Count; index++)
            {
                row.Cells[2 + index].Value = StatisticsStyle.SummaryCell(MetricCatalog.All[index],
                    summary.Averages[index].Average, summary.Maximums[index].Max);
            }
            row.DefaultCellStyle.ForeColor = isFuture ? StatisticsStyle.TextDisabled : StatisticsStyle.TextPrimary;
        }

        private void OnTableSelectionChanged(object sender, EventArgs e)
        {
            if (isFillingTable || table.SelectedRows.Count == 0) return;
            selectedRow = table.SelectedRows[0].Index;
            UpdateChart();
        }

        private string WindowText()
        {
            DayWindow window = recorder.CommonDayWindow();
            return FormatTime(window.Start) + "–" + FormatTime(window.End);
        }

        private static string FormatTime(TimeSpan time)
        {
            return ((int)time.TotalHours).ToString("00") + ":" + time.Minutes.ToString("00");
        }

        private void UpdateChart()
        {
            MetricKind kind = MetricCatalog.All[Math.Max(0, metricBox.SelectedIndex)];
            if (selectedRow == TotalRowIndex)
            {
                chartTitle.Text = MetricCatalog.LongTitle(kind) + " — дни недели, среднее за минуту, " + WindowText();
                chart.DisplayWeek(records, weekStart, kind, recorder.PeakOf(kind), recorder.CommonDayWindow());
                return;
            }
            DateTime day = weekStart.AddDays(selectedRow);
            chartTitle.Text = MetricCatalog.LongTitle(kind) + " — " + WeekDays.Label(day) + " (строка «Итого» — вся неделя)";
            chart.DisplayDay(records, day, kind, recorder.PeakOf(kind), recorder.CommonDayWindow());
        }
    }
}
