using System;

namespace DevMonitor
{
    internal static class WeekDays
    {
        private static readonly string[] ShortNames = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };

        public static DateTime WeekStart(DateTime day)
        {
            return day.Date.AddDays(-IndexOf(day));
        }

        public static string ShortName(DateTime day)
        {
            return ShortNames[IndexOf(day)];
        }

        public static string Label(DateTime day)
        {
            return ShortName(day) + " " + day.ToString("dd.MM");
        }

        public static int IndexOf(DateTime day)
        {
            return ((int)day.DayOfWeek + 6) % 7;
        }
    }
}
