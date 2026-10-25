using System;
using System.IO;
using System.Text;
using System.Threading;

namespace DevMonitor
{
    internal static class AppLog
    {
        private const long MaxBytes = 512 * 1024;
        private const int WriteAttempts = 5;
        private const int WriteRetryMs = 20;
        private const int SharingViolation = 32;
        private const int LockViolation = 33;

        public static readonly string FilePath = Path.Combine(UserDataPaths.Directory, "log.txt");
        private static readonly string PreviousFilePath = Path.Combine(UserDataPaths.Directory, "log.old.txt");
        private static readonly object WriteLock = new object();

        public static void Append(string line)
        {
            string text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line + Environment.NewLine;
            lock (WriteLock)
            {
                for (int attempt = 1; attempt <= WriteAttempts; attempt++)
                {
                    try
                    {
                        Write(text);
                        return;
                    }
                    catch (IOException error)
                    {
                        if (!IsSharingViolation(error)) return;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return;
                    }
                    Thread.Sleep(WriteRetryMs);
                }
            }
        }

        private static bool IsSharingViolation(IOException error)
        {
            int code = error.HResult & 0xFFFF;
            return code == SharingViolation || code == LockViolation;
        }

        private static void Write(string text)
        {
            Directory.CreateDirectory(UserDataPaths.Directory);
            var info = new FileInfo(FilePath);
            if (info.Exists && info.Length > MaxBytes)
            {
                File.Copy(FilePath, PreviousFilePath, true);
                File.Delete(FilePath);
            }
            File.AppendAllText(FilePath, text, new UTF8Encoding(false));
        }
    }
}
