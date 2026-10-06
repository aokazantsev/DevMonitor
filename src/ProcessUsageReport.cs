namespace DevMonitor
{
    internal sealed class ProcessUsageReport
    {
        public readonly ProcessGroupUsage Studio;
        public readonly ProcessGroupUsage GradleDaemons;
        public readonly ProcessGroupUsage KotlinDaemons;
        public readonly ProcessGroupUsage Workers;

        public ProcessUsageReport(ProcessGroupUsage studio, ProcessGroupUsage gradleDaemons, ProcessGroupUsage kotlinDaemons, ProcessGroupUsage workers)
        {
            Studio = studio;
            GradleDaemons = gradleDaemons;
            KotlinDaemons = kotlinDaemons;
            Workers = workers;
        }
    }
}
