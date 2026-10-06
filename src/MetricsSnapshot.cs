namespace DevMonitor
{
    internal sealed class MetricsSnapshot
    {
        public float? CpuLoad;
        public float? CpuTemperature;
        public GpuReading Gpu;
        public bool HasRam;
        public ulong RamUsedBytes;
        public ulong RamTotalBytes;
        public ProcessUsageReport Processes;
    }
}
