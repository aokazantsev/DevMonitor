namespace DevMonitor
{
    internal sealed class ProcessGroupUsage
    {
        public readonly int Count;
        public readonly long Bytes;

        public ProcessGroupUsage(int count, long bytes)
        {
            Count = count;
            Bytes = bytes;
        }
    }
}
