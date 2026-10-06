using System.Runtime.InteropServices;

namespace DevMonitor
{
    internal sealed class CpuLoadSensor
    {
        [DllImport("kernel32.dll")]
        private static extern bool GetSystemTimes(out long idleTime, out long kernelTime, out long userTime);

        private long previousIdle;
        private long previousTotal;

        public CpuLoadSensor()
        {
            Read();
        }

        public float? Read()
        {
            long idle, kernel, user;
            if (!GetSystemTimes(out idle, out kernel, out user)) return null;
            long total = kernel + user;
            long idleDelta = idle - previousIdle;
            long totalDelta = total - previousTotal;
            previousIdle = idle;
            previousTotal = total;
            if (totalDelta <= 0) return null;
            return 100f * (totalDelta - idleDelta) / totalDelta;
        }
    }
}
