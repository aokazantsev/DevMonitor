using System;
using System.Collections.Generic;

namespace DevMonitor
{
    internal static class MetricRowsFormatter
    {
        public static readonly int[] GroupSizes = { 2, 2, 4 };

        private const double BytesInGigabyte = 1024d * 1024d * 1024d;
        private const string NoValue = "—";
        private const string BatteryPause = "пауза: батарея";
        private const string Separator = "  ·  ";
        private const float LoadWarning = 75f;
        private const float LoadCritical = 90f;
        private const int SingleDaemon = 1;

        private static readonly ProcessUsageReport NoProcesses = new ProcessUsageReport(
            new ProcessGroupUsage(0, 0), new ProcessGroupUsage(0, 0), new ProcessGroupUsage(0, 0), new ProcessGroupUsage(0, 0));

        public static List<List<MetricRow>> Format(MetricsSnapshot snapshot)
        {
            ProcessUsageReport processes = snapshot.Processes ?? NoProcesses;
            return new List<List<MetricRow>>
            {
                new List<MetricRow>
                {
                    LoadWithTemperature("CPU", snapshot.CpuLoad, snapshot.CpuTemperature, ActiveHardware.Cpu.Zones),
                    Ram(snapshot)
                },
                new List<MetricRow>
                {
                    Gpu(snapshot.Gpu),
                    Vram(snapshot.Gpu)
                },
                new List<MetricRow>
                {
                    Memory("Studio", processes.Studio),
                    CountWithMemory("Gradle", processes.GradleDaemons, SingleDaemon),
                    CountWithMemory("Kotlin", processes.KotlinDaemons, int.MaxValue),
                    CountWithMemory("Воркеры", processes.Workers, int.MaxValue)
                }
            };
        }

        private static MetricRow LoadWithTemperature(string label, float? load, float? temperature, TemperatureZones zones)
        {
            return new MetricRow(label, LoadSegment(load), SeparatorSegment(), TemperatureSegment(temperature, zones));
        }

        private static MetricRow Gpu(GpuReading gpu)
        {
            if (gpu.IsPausedOnBattery) return new MetricRow("GPU", Plain(BatteryPause));
            return LoadWithTemperature("GPU", gpu.Load, gpu.Temperature, ActiveHardware.Gpu.Zones);
        }

        private static MetricRow Vram(GpuReading gpu)
        {
            if (!gpu.VramUsedBytes.HasValue || !gpu.VramTotalBytes.HasValue || gpu.VramTotalBytes.Value == 0)
            {
                return new MetricRow("VRAM", Plain(NoValue));
            }
            float load = 100f * gpu.VramUsedBytes.Value / gpu.VramTotalBytes.Value;
            return new MetricRow("VRAM", LoadSegment(load), SeparatorSegment(), Plain(UsedOfTotal(gpu.VramUsedBytes.Value, gpu.VramTotalBytes.Value)));
        }

        private static MetricRow Ram(MetricsSnapshot snapshot)
        {
            if (!snapshot.HasRam || snapshot.RamTotalBytes == 0) return new MetricRow("ОЗУ", Plain(NoValue));
            float load = 100f * snapshot.RamUsedBytes / snapshot.RamTotalBytes;
            return new MetricRow("ОЗУ", LoadSegment(load), SeparatorSegment(), Plain(UsedOfTotal(snapshot.RamUsedBytes, snapshot.RamTotalBytes)));
        }

        private static MetricRow Memory(string label, ProcessGroupUsage usage)
        {
            return new MetricRow(label, Plain(usage.Count == 0 ? NoValue : Gigabytes(usage.Bytes)));
        }

        private static MetricRow CountWithMemory(string label, ProcessGroupUsage usage, int expectedMaxCount)
        {
            if (usage.Count == 0) return new MetricRow(label, Plain(NoValue));
            Severity countSeverity = usage.Count > expectedMaxCount ? Severity.Warning : Severity.Normal;
            return new MetricRow(label,
                new ValueSegment("×" + usage.Count, countSeverity),
                SeparatorSegment(),
                Plain(Gigabytes(usage.Bytes)));
        }

        private static ValueSegment LoadSegment(float? load)
        {
            string text = load.HasValue ? string.Format("{0:0} %", load.Value) : NoValue + " %";
            return new ValueSegment(text, ByThreshold(load, LoadWarning, LoadCritical));
        }

        private static ValueSegment TemperatureSegment(float? temperature, TemperatureZones zones)
        {
            string text = temperature.HasValue ? string.Format("{0:0}°", temperature.Value) : NoValue + "°";
            return new ValueSegment(text, ByThreshold(temperature, zones.Warning, zones.Critical));
        }

        private static ValueSegment SeparatorSegment()
        {
            return Plain(Separator);
        }

        private static ValueSegment Plain(string text)
        {
            return new ValueSegment(text, Severity.Normal);
        }

        private static string Gigabytes(long bytes)
        {
            return string.Format("{0:0.0} ГБ", bytes / BytesInGigabyte);
        }

        private static string UsedOfTotal(ulong usedBytes, ulong totalBytes)
        {
            return string.Format("{0:0.0} / {1:0} ГБ", usedBytes / BytesInGigabyte, Math.Round(totalBytes / BytesInGigabyte));
        }

        private static Severity ByThreshold(float? value, float warning, float critical)
        {
            if (!value.HasValue) return Severity.Normal;
            if (value.Value >= critical) return Severity.Critical;
            return value.Value >= warning ? Severity.Warning : Severity.Normal;
        }
    }
}
