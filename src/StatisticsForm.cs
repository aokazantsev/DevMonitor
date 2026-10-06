using System;
using System.Drawing;
using System.Windows.Forms;

namespace DevMonitor
{
    internal sealed class StatisticsForm : Form
    {
        private const int LiveRefreshMs = 1000;
        private const int WeekRefreshTicks = 60;

        private readonly TabControl tabs = new TabControl();
        private readonly TabPage todayPage = new TabPage("Сегодня");
        private readonly TabPage weekPage = new TabPage("Неделя");
        private readonly TodayView todayView;
        private readonly WeekView weekView;
        private readonly Timer refreshTimer = new Timer();
        private int ticksSinceWeekReload;

        public StatisticsForm(HistoryRecorder recorder, WeekDayVisibility weekDayVisibility)
        {
            Text = "DevMonitor — статистика";
            Font = new Font("Segoe UI", 9.5f);
            BackColor = StatisticsStyle.Surface;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(Font.Height * 76, Font.Height * 44);
            MinimumSize = new Size(Font.Height * 55, Font.Height * 32);
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (ArgumentException)
            {
            }

            todayView = new TodayView(recorder, Font);
            weekView = new WeekView(recorder, weekDayVisibility, Font);
            todayPage.BackColor = StatisticsStyle.Surface;
            weekPage.BackColor = StatisticsStyle.Surface;
            todayPage.Controls.Add(todayView);
            weekPage.Controls.Add(weekView);
            tabs.Dock = DockStyle.Fill;
            tabs.Padding = new Point(Font.Height, Font.Height / 4);
            tabs.TabPages.Add(todayPage);
            tabs.TabPages.Add(weekPage);
            tabs.SelectedIndexChanged += delegate { RefreshSelected(true); };
            Controls.Add(tabs);

            refreshTimer.Interval = LiveRefreshMs;
            refreshTimer.Tick += delegate { RefreshSelected(false); };
            RefreshSelected(true);
            refreshTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) refreshTimer.Dispose();
            base.Dispose(disposing);
        }

        private void RefreshSelected(bool isForced)
        {
            if (WindowState == FormWindowState.Minimized) return;
            if (tabs.SelectedTab == todayPage)
            {
                todayView.RefreshLive();
                return;
            }
            if (isForced || ++ticksSinceWeekReload >= WeekRefreshTicks)
            {
                ticksSinceWeekReload = 0;
                weekView.Reload();
            }
        }
    }
}
