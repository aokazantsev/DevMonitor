namespace DevMonitor
{
    internal static class MetricSampler
    {
        private const double BytesInGigabyte = 1024d * 1024d * 1024d;

        public static float?[] Extract(MetricsSnapshot snapshot)
        {
            var values = new float?[MetricCatalog.Count];
            values[(int)MetricKind.CpuLoad] = snapshot.CpuLoad;
            values[(int)MetricKind.CpuTemperature] = snapshot.CpuTemperature;
            values[(int)MetricKind.GpuLoad] = snapshot.Gpu.Load;
            values[(int)MetricKind.GpuTemperature] = snapshot.Gpu.Temperature;
            if (snapshot.HasRam && snapshot.RamTotalBytes > 0)
            {
                values[(int)MetricKind.Ram] = 100f * snapshot.RamUsedBytes / snapshot.RamTotalBytes;
            }
            if (snapshot.Gpu.VramUsedBytes.HasValue && snapshot.Gpu.VramTotalBytes.HasValue && snapshot.Gpu.VramTotalBytes.Value > 0)
            {
                values[(int)MetricKind.Vram] = 100f * snapshot.Gpu.VramUsedBytes.Value / snapshot.Gpu.VramTotalBytes.Value;
            }
            if (snapshot.Processes != null)
            {
                long gradleBytes = snapshot.Processes.GradleDaemons.Bytes;
                long kotlinBytes = snapshot.Processes.KotlinDaemons.Bytes;
                long workerBytes = snapshot.Processes.Workers.Bytes;
                values[(int)MetricKind.JvmMemory] = Gigabytes(gradleBytes + kotlinBytes + workerBytes);
                values[(int)MetricKind.GradleMemory] = Gigabytes(gradleBytes);
                values[(int)MetricKind.KotlinMemory] = Gigabytes(kotlinBytes);
                values[(int)MetricKind.WorkerMemory] = Gigabytes(workerBytes);
            }
            return values;
        }

        private static float Gigabytes(long bytes)
        {
            return (float)(bytes / BytesInGigabyte);
        }
    }
}
