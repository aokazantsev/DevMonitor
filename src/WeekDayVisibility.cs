using System;
using System.Collections.Generic;

namespace DevMonitor
{
    internal sealed class WeekDayVisibility
    {
        private readonly HashSet<int> hiddenWeekDays = new HashSet<int>();

        public bool IsVisible(DateTime day)
        {
            return !hiddenWeekDays.Contains(WeekDays.IndexOf(day));
        }

        public void Toggle(DateTime day)
        {
            int index = WeekDays.IndexOf(day);
            if (!hiddenWeekDays.Remove(index)) hiddenWeekDays.Add(index);
        }
    }
}
