using System;

namespace DevMonitor
{
    internal static class DriverZones
    {
        private const float CriticalMargin = 3f;
        private const float WarningGap = 4f;
        private const float DefaultTarget = 83f;

        public static TemperatureZones Resolve(out string description)
        {
            float maximum;
            float? target;
            if (!NvidiaTemperatureLimits.TryRead(out maximum, out target))
            {
                description = "пределы из драйвера NVIDIA не прочитаны — пороги по умолчанию";
                return ActiveHardware.DefaultGpuZones;
            }
            float critical = maximum - CriticalMargin;
            float warning = Math.Min(target ?? DefaultTarget, critical - WarningGap);
            description = "максимум " + maximum.ToString("0") + " °C"
                + (target.HasValue ? ", целевая " + target.Value.ToString("0") + " °C" : string.Empty);
            return new TemperatureZones(warning, critical);
        }
    }
}
