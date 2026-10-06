using System;
using System.Collections.Generic;

namespace DevMonitor
{
    internal sealed class HistoryRecorder
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        private readonly HistoryStore store = new HistoryStore();
        private readonly RunningStat[] stats = new RunningStat[MetricCatalog.Count];
        private DateTime currentMinute = DateTime.MinValue;
        private int sampleCount;
        private DateTime cachedDay = DateTime.MinValue;
        private List<MinuteRecord> todayCache;
        private float?[] lastValues = new float?[MetricCatalog.Count];
        private float[] historyPeaks;
        private TimeSpan earliestTime = TimeSpan.MaxValue;
        private TimeSpan latestTime = TimeSpan.MinValue;

        public HistoryRecorder()
        {
            for (int index = 0; index < stats.Length; index++) stats[index] = new RunningStat();
            store.Purge(DateTime.Today);
        }

        public float?[] LastValues
        {
            get { return lastValues; }
        }

        public void Record(MetricsSnapshot snapshot, DateTime now)
        {
            DateTime minute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
            if (minute != currentMinute)
            {
                Flush();
                currentMinute = minute;
            }
            float?[] values = MetricSampler.Extract(snapshot);
            for (int index = 0; index < values.Length; index++) stats[index].Add(values[index]);
            sampleCount++;
            lastValues = values;
        }

        public void Flush()
        {
            MinuteRecord pending = Pending();
            if (pending == null) return;
            store.Append(pending);
            if (todayCache != null && pending.Time.Date == cachedDay) todayCache.Add(pending);
            if (historyPeaks != null) Absorb(pending);
            foreach (RunningStat stat in stats) stat.Reset();
            sampleCount = 0;
        }

        public List<MinuteRecord> Load(DateTime firstDay, int days)
        {
            List<MinuteRecord> records = store.Load(firstDay, days);
            AddPendingInRange(records, firstDay, firstDay.AddDays(days));
            return records;
        }

        public List<MinuteRecord> LoadToday()
        {
            DateTime today = DateTime.Today;
            if (todayCache == null || cachedDay != today)
            {
                todayCache = store.Load(today, 1);
                cachedDay = today;
            }
            var records = new List<MinuteRecord>(todayCache);
            AddPendingInRange(records, today, today.AddDays(1));
            return records;
        }

        public float PeakOf(MetricKind kind)
        {
            EnsureHistoryScanned();
            float peak = historyPeaks[(int)kind];
            MinuteRecord pending = Pending();
            if (pending != null && pending.Maximum[(int)kind].HasValue) peak = Math.Max(peak, pending.Maximum[(int)kind].Value);
            return peak;
        }

        public DayWindow CommonDayWindow()
        {
            EnsureHistoryScanned();
            TimeSpan earliest = earliestTime;
            TimeSpan latest = latestTime;
            MinuteRecord pending = Pending();
            if (pending != null)
            {
                TimeSpan start = pending.Time.TimeOfDay;
                if (start < earliest) earliest = start;
                if (start + OneMinute > latest) latest = start + OneMinute;
            }
            return earliest == TimeSpan.MaxValue ? DayWindow.WholeDay : new DayWindow(earliest, latest);
        }

        private void EnsureHistoryScanned()
        {
            if (historyPeaks != null) return;
            DateTime oldest = HistoryStore.OldestKeptDay(DateTime.Today);
            historyPeaks = new float[MetricCatalog.Count];
            foreach (MinuteRecord record in store.Load(oldest, (int)(DateTime.Today - oldest).TotalDays + 1))
            {
                Absorb(record);
            }
        }

        private void Absorb(MinuteRecord record)
        {
            for (int index = 0; index < historyPeaks.Length; index++)
            {
                if (record.Maximum[index].HasValue) historyPeaks[index] = Math.Max(historyPeaks[index], record.Maximum[index].Value);
            }
            TimeSpan start = record.Time.TimeOfDay;
            if (start < earliestTime) earliestTime = start;
            if (start + OneMinute > latestTime) latestTime = start + OneMinute;
        }

        private void AddPendingInRange(List<MinuteRecord> records, DateTime from, DateTime to)
        {
            MinuteRecord pending = Pending();
            if (pending != null && pending.Time >= from && pending.Time < to) records.Add(pending);
        }

        private MinuteRecord Pending()
        {
            if (sampleCount == 0) return null;
            var average = new float?[stats.Length];
            var maximum = new float?[stats.Length];
            for (int index = 0; index < stats.Length; index++)
            {
                average[index] = stats[index].Average;
                maximum[index] = stats[index].Max;
            }
            return new MinuteRecord(currentMinute, average, maximum);
        }
    }
}
