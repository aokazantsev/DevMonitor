using System;
using System.IO;
using System.Text;
using System.Threading;

namespace DevMonitor
{
    internal sealed class AppSettings
    {
        private const string CpuKey = "cpu";
        private const string GpuKey = "gpu";
        private const char KeyValueSeparator = '=';
        private const int ReadAttempts = 5;
        private const int ReadRetryMs = 300;
        private const int PatientReadRetries = 10;
        private const int PatientReadRetryMs = 1500;

        public static readonly string FilePath = UserDataPaths.Settings;

        public string CpuProfileName;
        public string GpuProfileName;
        public string ReadError;
        public bool ReadDenied;

        public static AppSettings Load()
        {
            var settings = new AppSettings();
            if (!File.Exists(FilePath)) return settings;
            string[] lines = null;
            for (int attempt = 1; lines == null; attempt++)
            {
                try
                {
                    lines = File.ReadAllLines(FilePath, Encoding.UTF8);
                }
                catch (FileNotFoundException)
                {
                    return settings;
                }
                catch (DirectoryNotFoundException)
                {
                    return settings;
                }
                catch (IOException error)
                {
                    if (attempt >= ReadAttempts) settings.ReadError = error.Message;
                }
                catch (UnauthorizedAccessException error)
                {
                    settings.ReadError = error.Message;
                    settings.ReadDenied = true;
                }
                if (settings.ReadError != null) return settings;
                if (lines == null) Thread.Sleep(ReadRetryMs);
            }
            foreach (string line in lines)
            {
                int separator = line.IndexOf(KeyValueSeparator);
                if (separator <= 0) continue;
                string key = line.Substring(0, separator).Trim();
                string value = line.Substring(separator + 1).Trim();
                if (string.Equals(key, CpuKey, StringComparison.OrdinalIgnoreCase)) settings.CpuProfileName = value;
                else if (string.Equals(key, GpuKey, StringComparison.OrdinalIgnoreCase)) settings.GpuProfileName = value;
            }
            return settings;
        }

        public static AppSettings LoadPatiently(Action<string> log)
        {
            AppSettings settings = Load();
            for (int attempt = 1; settings.ReadError != null && !settings.ReadDenied && attempt <= PatientReadRetries; attempt++)
            {
                log("settings not read: " + settings.ReadError + ", retry " + attempt);
                Thread.Sleep(PatientReadRetryMs);
                settings = Load();
            }
            return settings;
        }

        public string ReadAdvice
        {
            get { return ReadDenied ? "Нет доступа к файлу: верни права на него или удали его." : "Файл занят другой программой: подожди, пока он освободится."; }
        }

        public string Save()
        {
            string content = CpuKey + KeyValueSeparator + CpuProfileName + Environment.NewLine
                + GpuKey + KeyValueSeparator + GpuProfileName + Environment.NewLine;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                if (File.Exists(FilePath))
                {
                    FileAttributes attributes = File.GetAttributes(FilePath);
                    if ((attributes & FileAttributes.ReadOnly) != 0) File.SetAttributes(FilePath, attributes & ~FileAttributes.ReadOnly);
                }
                File.WriteAllText(FilePath, content, new UTF8Encoding(false));
                return null;
            }
            catch (IOException error)
            {
                return error.Message;
            }
            catch (UnauthorizedAccessException error)
            {
                return error.Message;
            }
        }
    }
}
