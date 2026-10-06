namespace DevMonitor
{
    internal sealed class GpuReading
    {
        public float? Load;
        public float? Temperature;
        public ulong? VramUsedBytes;
        public ulong? VramTotalBytes;
        public bool IsPausedOnBattery;
    }
}
