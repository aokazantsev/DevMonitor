using System;
using System.IO;
using System.Text;

namespace DevMonitor
{
    internal static class AppLog
    {
        private const long MaxBytes = 512 * 1024;

        public static readonly string FilePath = Path.Combine(UserDataPaths.Directory, "log.txt");
        private static readonly string PreviousFilePath = Path.Combine(UserDataPaths.Directory, "log.old.txt");
        private static readonly object WriteLock = new object();

        public static void Append(string line)
        {
            lock (WriteLock)
            {
                try
                {
                    Directory.CreateDirectory(UserDataPaths.Directory);
                    var info = new FileInfo(FilePath);
                    if (info.Exists && info.Length > MaxBytes)
                    {
                        File.Copy(FilePath, PreviousFilePath, true);
                        File.Delete(FilePath);
                    }
                    File.AppendAllText(FilePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line + Environment.NewLine,
                        new UTF8Encoding(false));
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }
}
