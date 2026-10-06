using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;

namespace DevMonitor
{
    internal sealed class NvidiaGpuSensor : IDisposable
    {
        public const string GuardName = "gpu";

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

        private IntPtr device;
        private bool isAvailable;
        private bool firstReadPending;

        public string Problem { get; private set; }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public void Open()
        {
            string blockedStep = SensorGuard.BlockedStep(GuardName);
            if (blockedStep != null)
            {
                Problem = "отключена: прошлый запуск упал на шаге «" + blockedStep + "»";
                AppLog.Append("nvidia gpu: " + Problem);
                return;
            }
            SensorGuard.Enter(GuardName, "запуск NVML");
            try
            {
                int init = NvmlInit();
                int handle = init == NvmlSuccess ? NvmlGetDevice(0, out device) : -1;
                isAvailable = init == NvmlSuccess && handle == NvmlSuccess;
                firstReadPending = isAvailable;
                AppLog.Append("nvidia gpu: nvmlInit " + init + ", device " + handle + (isAvailable ? ", available" : ", not available"));
                if (!isAvailable) SensorGuard.Leave(GuardName);
            }
            catch (DllNotFoundException)
            {
                isAvailable = false;
                AppLog.Append("nvidia gpu: nvml.dll not found — no NVIDIA driver");
                SensorGuard.Leave(GuardName);
            }
            catch (Exception error)
            {
                isAvailable = false;
                Problem = "недоступна: " + error.GetType().Name + ": " + error.Message;
                AppLog.Append("nvidia gpu: " + Problem);
                SensorGuard.Leave(GuardName);
            }
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public GpuReading Read()
        {
            var reading = new GpuReading();
            if (!isAvailable) return reading;
            if (firstReadPending) SensorGuard.Enter(GuardName, "первое чтение видеокарты NVIDIA");
            try
            {
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
                if (firstReadPending)
                {
                    firstReadPending = false;
                    SensorGuard.Leave(GuardName);
                    AppLog.Append("nvidia gpu: first read, temperature " + reading.Temperature + ", load " + reading.Load);
                }
            }
            catch (Exception error)
            {
                isAvailable = false;
                Problem = "отключена после ошибки: " + error.GetType().Name + ": " + error.Message;
                AppLog.Append("nvidia gpu: " + Problem);
                if (firstReadPending)
                {
                    firstReadPending = false;
                    SensorGuard.Leave(GuardName);
                }
                return new GpuReading();
            }
            return reading;
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public void Dispose()
        {
            if (!isAvailable) return;
            isAvailable = false;
            try
            {
                NvmlShutdown();
            }
            catch (Exception)
            {
            }
        }
    }
}
