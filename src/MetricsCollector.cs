using System;

namespace DevMonitor
{
    internal sealed class MetricsCollector : IDisposable
    {
        private const int ProcessRefreshTicks = 3;

        private readonly CpuLoadSensor cpuLoad = new CpuLoadSensor();
        private readonly CpuTemperatureSensor cpuTemperature = new CpuTemperatureSensor();
        private readonly NvidiaGpuSensor gpu = new NvidiaGpuSensor();
        private readonly ProcessGroupReader processReader = new ProcessGroupReader();
        private int tick;
        private ProcessUsageReport processes;

        public MetricsSnapshot Collect()
        {
            if (tick++ % ProcessRefreshTicks == 0)
            {
                processes = processReader.Read();
            }

            var snapshot = new MetricsSnapshot
            {
                CpuLoad = cpuLoad.Read(),
                CpuTemperature = cpuTemperature.Read(),
                Gpu = PowerSource.IsOnBattery() ? new GpuReading { IsPausedOnBattery = true } : gpu.Read(),
                Processes = processes
            };
            snapshot.HasRam = SystemMemoryReader.TryRead(out snapshot.RamUsedBytes, out snapshot.RamTotalBytes);
            return snapshot;
        }

        public void Dispose()
        {
            cpuTemperature.Dispose();
            gpu.Dispose();
        }
    }
}
