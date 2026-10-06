using System.Diagnostics;

namespace DevMonitor.Setup
{
    internal static class RunningApp
    {
        public static bool IsRunning()
        {
            Process[] processes = Process.GetProcessesByName(AppIdentity.ProcessName);
            foreach (Process process in processes) process.Dispose();
            return processes.Length > 0;
        }
    }
}
