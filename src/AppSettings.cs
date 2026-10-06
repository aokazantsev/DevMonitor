using System;
using System.IO;
using System.Text;

namespace DevMonitor
{
    internal sealed class AppSettings
    {
        private const string CpuKey = "cpu";
        private const string GpuKey = "gpu";
        private const char KeyValueSeparator = '=';

        private static readonly string FilePath = UserDataPaths.Settings;

        public string CpuProfileName;
        public string GpuProfileName;

        public static AppSettings Load()
        {
            var settings = new AppSettings();
            if (!File.Exists(FilePath)) return settings;
            try
            {
                foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    int separator = line.IndexOf(KeyValueSeparator);
                    if (separator <= 0) continue;
                    string key = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();
                    if (string.Equals(key, CpuKey, StringComparison.OrdinalIgnoreCase)) settings.CpuProfileName = value;
                    else if (string.Equals(key, GpuKey, StringComparison.OrdinalIgnoreCase)) settings.GpuProfileName = value;
                }
            }
            catch (IOException)
            {
            }
            return settings;
        }

        public bool TrySave()
        {
            try
            {
                string content = CpuKey + KeyValueSeparator + CpuProfileName + Environment.NewLine
                    + GpuKey + KeyValueSeparator + GpuProfileName + Environment.NewLine;
                File.WriteAllText(FilePath, content, new UTF8Encoding(false));
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
