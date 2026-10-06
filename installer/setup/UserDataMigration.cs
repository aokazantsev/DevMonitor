using System;
using System.IO;
using Microsoft.Win32;

namespace DevMonitor.Setup
{
    internal static class UserDataMigration
    {
        private const string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + AppIdentity.Name;
        private static readonly string[] Files = { "settings.txt", "position.txt" };
        private const string HistoryFolder = "data";

        public static string PreviousInstallDirectory()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(UninstallKeyPath))
            {
                return key == null ? null : key.GetValue("InstallLocation") as string;
            }
        }

        public static bool MoveFrom(string previousDirectory)
        {
            if (string.IsNullOrEmpty(previousDirectory) || !Directory.Exists(previousDirectory)) return false;
            string target = AppIdentity.UserDataDirectory;
            bool moved = false;
            Directory.CreateDirectory(target);
            foreach (string name in Files)
            {
                string source = Path.Combine(previousDirectory, name);
                string destination = Path.Combine(target, name);
                if (!File.Exists(source) || File.Exists(destination)) continue;
                File.Copy(source, destination);
                moved = true;
            }
            string history = Path.Combine(previousDirectory, HistoryFolder);
            if (Directory.Exists(history))
            {
                string historyTarget = Path.Combine(target, HistoryFolder);
                Directory.CreateDirectory(historyTarget);
                foreach (string file in Directory.GetFiles(history, "*.csv"))
                {
                    string destination = Path.Combine(historyTarget, Path.GetFileName(file));
                    if (File.Exists(destination)) continue;
                    File.Copy(file, destination);
                    moved = true;
                }
            }
            return moved;
        }
    }
}
