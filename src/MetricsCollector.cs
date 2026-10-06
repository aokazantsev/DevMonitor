using System;

namespace DevMonitor
{
    internal sealed class MetricsCollector : IDisposable
    {
        private const int ProcessRefreshTicks = 3;

        private readonly CpuLoadSensor cpuLoad = new CpuLoadSensor();
        private CpuTemperatureSensor cpuTemperature = new CpuTemperatureSensor();
        private NvidiaGpuSensor gpu = new NvidiaGpuSensor();
        private readonly ProcessGroupReader processReader = new ProcessGroupReader();
        private int tick;
        private ProcessUsageReport processes;

        public MetricsCollector()
        {
            cpuTemperature.Open();
            gpu.Open();
        }

        public string CpuTemperatureProblem
        {
            get { return cpuTemperature.Problem; }
        }

        public string GpuProblem
        {
            get { return gpu.Problem; }
        }

        public void RetrySensors()
        {
            AppLog.Append("sensors: retry requested");
            cpuTemperature.Dispose();
            gpu.Dispose();
            SensorGuard.Leave(CpuTemperatureSensor.GuardName);
            SensorGuard.Leave(NvidiaGpuSensor.GuardName);
            SensorGuard.Leave(NvidiaTemperatureLimits.GuardName);
            cpuTemperature = new CpuTemperatureSensor();
            gpu = new NvidiaGpuSensor();
            cpuTemperature.Open();
            gpu.Open();
        }

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
