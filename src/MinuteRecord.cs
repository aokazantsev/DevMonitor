using System;

namespace DevMonitor
{
    internal sealed class MinuteRecord
    {
        public readonly DateTime Time;
        public readonly float?[] Average;
        public readonly float?[] Maximum;

        public MinuteRecord(DateTime time, float?[] average, float?[] maximum)
        {
            Time = time;
            Average = average;
            Maximum = maximum;
        }
    }
}
