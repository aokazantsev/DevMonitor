using System;
using System.Runtime.InteropServices;

namespace DevMonitor
{
    internal static class ProcessCommandLineReader
    {
        private const int ProcessQueryLimitedInformation = 0x1000;
        private const int ProcessCommandLineInformation = 60;
        private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);
        private const int StatusSuccess = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct UnicodeString
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int access, bool inheritHandle, int processId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr process, int informationClass, IntPtr buffer, int length, out int returnLength);

        public static string Read(int processId)
        {
            IntPtr process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
            if (process == IntPtr.Zero) return null;
            try
            {
                int length;
                int status = NtQueryInformationProcess(process, ProcessCommandLineInformation, IntPtr.Zero, 0, out length);
                if (status != StatusInfoLengthMismatch || length <= 0) return null;
                IntPtr buffer = Marshal.AllocHGlobal(length);
                try
                {
                    if (NtQueryInformationProcess(process, ProcessCommandLineInformation, buffer, length, out length) != StatusSuccess) return null;
                    var text = (UnicodeString)Marshal.PtrToStructure(buffer, typeof(UnicodeString));
                    return text.Buffer == IntPtr.Zero ? null : Marshal.PtrToStringUni(text.Buffer, text.Length / 2);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            finally
            {
                CloseHandle(process);
            }
        }
    }
}
