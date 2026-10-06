using System;
using System.Runtime.InteropServices;

namespace DevMonitor
{
    internal sealed class NvidiaGpuSensor : IDisposable
    {
        private const int NvmlSuccess = 0;
        private const int NvmlTemperatureGpu = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct NvmlUtilization
        {
            public uint Gpu;
            public uint Memory;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NvmlMemory
        {
            public ulong Total;
            public ulong Free;
            public ulong Used;
        }

        [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
        private static extern int NvmlInit();

        [DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
        private static extern int NvmlShutdown();

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
        private static extern int NvmlGetDevice(uint index, out IntPtr device);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
        private static extern int NvmlGetTemperature(IntPtr device, int sensor, out uint temperature);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetUtilizationRates")]
        private static extern int NvmlGetUtilization(IntPtr device, out NvmlUtilization utilization);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetMemoryInfo")]
        private static extern int NvmlGetMemory(IntPtr device, out NvmlMemory memory);

        private readonly IntPtr device;
        private readonly bool isAvailable;

        public NvidiaGpuSensor()
        {
            try
            {
                isAvailable = NvmlInit() == NvmlSuccess && NvmlGetDevice(0, out device) == NvmlSuccess;
            }
            catch (Exception)
            {
                isAvailable = false;
            }
        }

        public GpuReading Read()
        {
            var reading = new GpuReading();
            if (!isAvailable) return reading;

            uint temperature;
            if (NvmlGetTemperature(device, NvmlTemperatureGpu, out temperature) == NvmlSuccess)
            {
                reading.Temperature = temperature;
            }

            NvmlUtilization utilization;
            if (NvmlGetUtilization(device, out utilization) == NvmlSuccess)
            {
                reading.Load = utilization.Gpu;
            }

            NvmlMemory memory;
            if (NvmlGetMemory(device, out memory) == NvmlSuccess)
            {
                reading.VramUsedBytes = memory.Used;
                reading.VramTotalBytes = memory.Total;
            }
            return reading;
        }

        public void Dispose()
        {
            if (isAvailable) NvmlShutdown();
        }
    }
}
