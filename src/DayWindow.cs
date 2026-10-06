using System;

namespace DevMonitor
{
    internal sealed class DayWindow
    {
        public static readonly DayWindow WholeDay = new DayWindow(TimeSpan.Zero, TimeSpan.FromDays(1));

        public readonly TimeSpan Start;
        public readonly TimeSpan End;

        public DayWindow(TimeSpan start, TimeSpan end)
        {
            Start = start;
            End = end;
        }
    }
}
