using System;
using System.IO;

namespace DevMonitor.Setup
{
    internal static class InstallTarget
    {
        public static string DefaultDirectory()
        {
            return AppIdentity.DefaultInstallDirectory;
        }

        public static string Validate(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) return "Укажи папку установки.";
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(directory);
            }
            catch (Exception)
            {
                return "Путь к папке некорректный.";
            }
            if (string.Equals(fullPath.TrimEnd('\\'), Path.GetPathRoot(fullPath).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                return "Нельзя ставить в корень диска — выбери папку, например " + AppIdentity.DefaultInstallDirectory + ".";
            }
            if (!Directory.Exists(fullPath)) return null;
            if (File.Exists(Path.Combine(fullPath, AppIdentity.ExecutableName))) return null;
            if (Directory.GetFileSystemEntries(fullPath).Length == 0) return null;
            return "Папка не пустая и в ней нет DevMonitor. Удаление стирает папку целиком, поэтому выбери пустую или новую папку.";
        }

        public static bool IsExistingInstall(string directory)
        {
            return File.Exists(Path.Combine(directory, AppIdentity.ExecutableName));
        }
    }
}
