using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;

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

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public static bool TryRead(out float maximum, out float? target)
        {
            maximum = 0;
            target = null;
            string blockedStep = SensorGuard.BlockedStep(NvidiaGpuSensor.GuardName);
            if (blockedStep != null)
            {
                AppLog.Append("nvidia limits: skipped, previous run stopped at " + blockedStep);
                return false;
            }
            SensorGuard.Enter(NvidiaGpuSensor.GuardName, "чтение порогов температуры NVIDIA");
            try
            {
                int init = NvmlInit();
                if (init != NvmlSuccess)
                {
                    AppLog.Append("nvidia limits: nvmlInit " + init);
                    return false;
                }
                try
                {
                    IntPtr device;
                    int handle = NvmlGetDevice(0, out device);
                    if (handle != NvmlSuccess)
                    {
                        AppLog.Append("nvidia limits: device " + handle);
                        return false;
                    }
                    uint value;
                    int threshold = NvmlGetThreshold(device, ThresholdGpuMax, out value);
                    if (threshold != NvmlSuccess || value == 0)
                    {
                        AppLog.Append("nvidia limits: max threshold " + threshold + ", value " + value);
                        return false;
                    }
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
                AppLog.Append("nvidia limits: nvml.dll not found");
                return false;
            }
            catch (Exception error)
            {
                AppLog.Append("nvidia limits: " + error.GetType().Name + ": " + error.Message);
                return false;
            }
            finally
            {
                SensorGuard.Leave(NvidiaGpuSensor.GuardName);
            }
        }
    }
}
