using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DevMonitor
{
    internal static class CrashReport
    {
        public static readonly string FilePath = Path.Combine(UserDataPaths.Directory, "crash.log");

        private static int shown;

        public static void Write(string source, Exception error, bool terminating)
        {
            string text = error == null ? "unknown error" : error.ToString();
            Append("CRASH (" + source + (terminating ? ", terminating" : "") + ", " + AppIdentity.Version + "): " + text);
            if (Interlocked.Exchange(ref shown, 1) != 0) return;
            if (CrashReportConsent.IsGiven)
            {
                string problem = CrashUploader.Send(source, text, FilePath);
                Append(problem == null ? "отчёт о сбое отправлен" : "отчёт о сбое не отправлен: " + problem);
            }
            try
            {
                MessageBox.Show(
                    AppIdentity.Name + " упал: " + (error == null ? "неизвестная ошибка" : error.Message)
                    + Environment.NewLine + Environment.NewLine + "Подробности: " + FilePath,
                    AppIdentity.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static void Append(string line)
        {
            try
            {
                Directory.CreateDirectory(UserDataPaths.Directory);
                File.AppendAllText(FilePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line + Environment.NewLine, new UTF8Encoding(false));
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
