using System;
using System.Runtime.InteropServices;

namespace DevMonitor
{
    internal static class NvidiaTemperatureLimits
    {
        private const int NvmlSuccess = 0;
        private const int ThresholdGpuMax = 3;
        private const int ThresholdTarget = 5;

        [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
        private static extern int NvmlInit();

        [DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
        private static extern int NvmlShutdown();

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
        private static extern int NvmlGetDevice(uint index, out IntPtr device);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperatureThreshold")]
        private static extern int NvmlGetThreshold(IntPtr device, int threshold, out uint temperature);

        public static bool TryRead(out float maximum, out float? target)
        {
            maximum = 0;
            target = null;
            try
            {
                if (NvmlInit() != NvmlSuccess) return false;
                try
                {
                    IntPtr device;
                    if (NvmlGetDevice(0, out device) != NvmlSuccess) return false;
                    uint value;
                    if (NvmlGetThreshold(device, ThresholdGpuMax, out value) != NvmlSuccess || value == 0) return false;
                    maximum = value;
                    if (NvmlGetThreshold(device, ThresholdTarget, out value) == NvmlSuccess && value > 0) target = value;
                    return true;
                }
                finally
                {
                    NvmlShutdown();
                }
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }
    }
}
