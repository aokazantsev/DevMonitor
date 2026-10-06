using System;
using System.Diagnostics;

namespace DevMonitor
{
    internal sealed class ProcessGroupReader
    {
        private const string StudioName = "studio64";
        private const string JavaName = "java";

        private readonly JavaProcessClassifier classifier = new JavaProcessClassifier();

        public ProcessUsageReport Read()
        {
            var counts = new int[4];
            var bytes = new long[4];
            classifier.BeginPass();
            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    int slot = SlotOf(process);
                    if (slot < 0) continue;
                    counts[slot]++;
                    bytes[slot] += process.WorkingSet64;
                }
                catch (InvalidOperationException)
                {
                }
                finally
                {
                    process.Dispose();
                }
            }
            classifier.EndPass();
            return new ProcessUsageReport(
                new ProcessGroupUsage(counts[0], bytes[0]),
                new ProcessGroupUsage(counts[1], bytes[1]),
                new ProcessGroupUsage(counts[2], bytes[2]),
                new ProcessGroupUsage(counts[3], bytes[3]));
        }

        private int SlotOf(Process process)
        {
            string name = process.ProcessName;
            if (string.Equals(name, StudioName, StringComparison.OrdinalIgnoreCase)) return 0;
            if (!string.Equals(name, JavaName, StringComparison.OrdinalIgnoreCase)) return -1;
            switch (classifier.Classify(process.Id))
            {
                case JavaProcessKind.GradleDaemon:
                    return 1;
                case JavaProcessKind.KotlinDaemon:
                    return 2;
                default:
                    return 3;
            }
        }
    }
}
