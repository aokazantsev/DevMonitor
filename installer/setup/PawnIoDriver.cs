using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;

namespace DevMonitor.Setup
{
    internal static class PawnIoDriver
    {
        private const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO";
        private const int CancelledByUser = 1223;

        public static bool IsInstalled()
        {
            using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(UninstallKey))
            {
                return key != null;
            }
        }

        public static string RunSetup(string setupPath)
        {
            try
            {
                using (Process process = Process.Start(new ProcessStartInfo(setupPath) { UseShellExecute = true }))
                {
                    process.WaitForExit();
                }
            }
            catch (Win32Exception error)
            {
                return error.NativeErrorCode == CancelledByUser
                    ? "установка драйвера отменена в окне UAC"
                    : "установщик драйвера не запустился: " + error.Message;
            }
            return IsInstalled() ? null : "драйвер не установлен — окно установщика PawnIO закрыто без установки";
        }
    }
}
