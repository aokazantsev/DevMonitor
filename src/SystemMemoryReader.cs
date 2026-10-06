using System.Runtime.InteropServices;

namespace DevMonitor
{
    internal static class SystemMemoryReader
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

        public static bool TryRead(out ulong usedBytes, out ulong totalBytes)
        {
            var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx)) };
            if (!GlobalMemoryStatusEx(ref status))
            {
                usedBytes = 0;
                totalBytes = 0;
                return false;
            }
            totalBytes = status.TotalPhys;
            usedBytes = status.TotalPhys - status.AvailPhys;
            return true;
        }
    }
}
