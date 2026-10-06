using System;
using System.Collections.Generic;

namespace DevMonitor
{
    internal sealed class DaySummary
    {
        public readonly int Minutes;
        public readonly RunningStat[] Averages;
        public readonly RunningStat[] Maximums;

        private DaySummary(int minutes, RunningStat[] averages, RunningStat[] maximums)
        {
            Minutes = minutes;
            Averages = averages;
            Maximums = maximums;
        }

        public static DaySummary Of(IEnumerable<MinuteRecord> records, DateTime from, DateTime to)
        {
            var averages = NewStats();
            var maximums = NewStats();
            int minutes = 0;
            foreach (MinuteRecord record in records)
            {
                if (record.Time < from || record.Time >= to) continue;
                minutes++;
                for (int index = 0; index < MetricCatalog.Count; index++)
                {
                    averages[index].Add(record.Average[index]);
                    maximums[index].Add(record.Maximum[index]);
                }
            }
            return new DaySummary(minutes, averages, maximums);
        }

        private static RunningStat[] NewStats()
        {
            var stats = new RunningStat[MetricCatalog.Count];
            for (int index = 0; index < stats.Length; index++) stats[index] = new RunningStat();
            return stats;
        }
    }
}
