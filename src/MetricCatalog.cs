using System;

namespace DevMonitor
{
    internal static class MetricCatalog
    {
        public static readonly MetricKind[] All =
        {
            MetricKind.CpuLoad,
            MetricKind.CpuTemperature,
            MetricKind.GpuLoad,
            MetricKind.GpuTemperature,
            MetricKind.Ram,
            MetricKind.Vram,
            MetricKind.JvmMemory,
            MetricKind.GradleMemory,
            MetricKind.KotlinMemory,
            MetricKind.WorkerMemory
        };

        private const float PercentCeiling = 100f;
        private const float ScaleStep = 20f;
        private const float MemoryScaleStep = 2f;

        public static int Count
        {
            get { return All.Length; }
        }

        public static string CsvKey(MetricKind kind)
        {
            switch (kind)
            {
                case MetricKind.CpuLoad:
                    return "cpu_load";
                case MetricKind.CpuTemperature:
                    return "cpu_temp";
                case MetricKind.GpuLoad:
                    return "gpu_load";
                case MetricKind.GpuTemperature:
                    return "gpu_temp";
                case MetricKind.Ram:
                    return "ram";
                case MetricKind.Vram:
                    return "vram";
                case MetricKind.GradleMemory:
                    return "gradle_gb";
                case MetricKind.KotlinMemory:
                    return "kotlin_gb";
                case MetricKind.WorkerMemory:
                    return "workers_gb";
                default:
                    return "jvm_gb";
            }
        }

        public static string Title(MetricKind kind)
        {
            switch (kind)
            {
                case MetricKind.CpuLoad:
                    return "CPU %";
                case MetricKind.CpuTemperature:
                    return "CPU °";
                case MetricKind.GpuLoad:
                    return "GPU %";
                case MetricKind.GpuTemperature:
                    return "GPU °";
                case MetricKind.Ram:
                    return "ОЗУ %";
                case MetricKind.Vram:
                    return "VRAM %";
                case MetricKind.GradleMemory:
                    return "Gradle ГБ";
                case MetricKind.KotlinMemory:
                    return "Kotlin ГБ";
                case MetricKind.WorkerMemory:
                    return "Воркеры ГБ";
                default:
                    return "JVM ГБ";
            }
        }

        public static string LongTitle(MetricKind kind)
        {
            switch (kind)
            {
                case MetricKind.CpuLoad:
                    return "Загрузка CPU, %";
                case MetricKind.CpuTemperature:
                    return "Температура CPU, °C";
                case MetricKind.GpuLoad:
                    return "Загрузка GPU, %";
                case MetricKind.GpuTemperature:
                    return "Температура GPU, °C";
                case MetricKind.Ram:
                    return "ОЗУ, %";
                case MetricKind.Vram:
                    return "Видеопамять, %";
                case MetricKind.GradleMemory:
                    return "Память Gradle-демонов, ГБ";
                case MetricKind.KotlinMemory:
                    return "Память Kotlin-демонов, ГБ";
                case MetricKind.WorkerMemory:
                    return "Память воркеров (тесты и прочие JVM), ГБ";
                default:
                    return "Память JVM (Gradle + Kotlin + воркеры), ГБ";
            }
        }

        public static string Format(MetricKind kind, float value)
        {
            return IsMemory(kind) ? value.ToString("0.0") : value.ToString("0");
        }

        public static TemperatureZones ZonesOf(MetricKind kind)
        {
            if (kind == MetricKind.CpuTemperature) return ActiveHardware.Cpu.Zones;
            return kind == MetricKind.GpuTemperature ? ActiveHardware.Gpu.Zones : null;
        }

        public static float ScaleMax(MetricKind kind, float peak)
        {
            float step = IsMemory(kind) ? MemoryScaleStep : ScaleStep;
            float rounded = Math.Max(step, (float)Math.Ceiling(peak / step) * step);
            return IsPercent(kind) ? Math.Min(PercentCeiling, rounded) : rounded;
        }

        private static bool IsMemory(MetricKind kind)
        {
            return kind == MetricKind.JvmMemory || kind == MetricKind.GradleMemory || kind == MetricKind.KotlinMemory || kind == MetricKind.WorkerMemory;
        }

        private static bool IsPercent(MetricKind kind)
        {
            return kind == MetricKind.CpuLoad || kind == MetricKind.GpuLoad || kind == MetricKind.Ram || kind == MetricKind.Vram;
        }
    }
}
