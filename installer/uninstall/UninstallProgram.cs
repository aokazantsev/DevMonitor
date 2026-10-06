using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace DevMonitor.Setup
{
    internal static class UninstallProgram
    {
        private const string RemoveCommand = "/remove";
        private const int DeleteAttempts = 20;
        private const int DeleteRetryMs = 500;

        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            if (args.Length == 2 && args[0] == RemoveCommand) return Remove(args[1]);
            return Confirm(Path.GetDirectoryName(Application.ExecutablePath));
        }

        private static int Confirm(string installDirectory)
        {
            DialogResult answer = MessageBox.Show(
                "Удалить " + AppIdentity.Name + " из " + installDirectory + "?" + Environment.NewLine + Environment.NewLine
                + "Папка программы удалится целиком, вместе с ней — история и настройки (" + AppIdentity.UserDataDirectory + "). Удалятся ярлык на рабочем столе, запись в «Приложениях» и автозапуск, если он включён."
                + Environment.NewLine + Environment.NewLine
                + "Драйвер PawnIO останется — это отдельная программа, удаляется в «Параметры → Приложения».",
                "Удаление " + AppIdentity.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) return 1;

            while (RunningApp.IsRunning())
            {
                DialogResult retry = MessageBox.Show(
                    AppIdentity.Name + " сейчас запущен. Закрой его: значок в трее → «Выход», затем нажми «Повтор».",
                    "Удаление " + AppIdentity.Name, MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                if (retry != DialogResult.Retry) return 1;
            }

            string worker = Path.Combine(Path.GetTempPath(), AppIdentity.Name + "Uninstall-" + Guid.NewGuid().ToString("N") + ".exe");
            File.Copy(Application.ExecutablePath, worker, true);
            Process.Start(new ProcessStartInfo(worker, RemoveCommand + " \"" + installDirectory + "\"") { UseShellExecute = false });
            return 0;
        }

        private static int Remove(string installDirectory)
        {
            string problem = null;
            if (Directory.Exists(installDirectory))
            {
                if (File.Exists(Path.Combine(installDirectory, AppIdentity.ExecutableName)))
                {
                    problem = DeleteWithRetries(installDirectory);
                }
                else
                {
                    problem = "в папке нет " + AppIdentity.ExecutableName + " — не похоже на папку DevMonitor, она не тронута";
                }
            }
            if (Directory.Exists(AppIdentity.UserDataDirectory))
            {
                string dataProblem = DeleteWithRetries(AppIdentity.UserDataDirectory);
                if (dataProblem != null) problem = problem == null ? "история и настройки " + dataProblem : problem + "; история и настройки " + dataProblem;
            }
            DesktopShortcut.Delete();
            UninstallRegistration.Unregister();
            if (StartupTask.IsEnabled())
            {
                string startupProblem = StartupTask.Disable();
                if (startupProblem != null)
                {
                    string leftover = "задача автозапуска «" + StartupTask.TaskName + "» осталась в Планировщике заданий — удали её там вручную";
                    problem = problem == null ? leftover : problem + "; " + leftover;
                }
            }

            if (problem == null)
            {
                MessageBox.Show(AppIdentity.Name + " удалён.", "Удаление " + AppIdentity.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Удаление закончено не полностью: " + problem + ". Папка: " + installDirectory + ".",
                    "Удаление " + AppIdentity.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            ScheduleSelfDelete();
            return problem == null ? 0 : 2;
        }

        private static string DeleteWithRetries(string directory)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    Directory.Delete(directory, true);
                    return null;
                }
                catch (IOException error)
                {
                    if (attempt >= DeleteAttempts) return "удалена не полностью (" + error.Message + ")";
                }
                catch (UnauthorizedAccessException error)
                {
                    if (attempt >= DeleteAttempts) return "удалена не полностью (" + error.Message + ")";
                }
                Thread.Sleep(DeleteRetryMs);
            }
        }

        private static void ScheduleSelfDelete()
        {
            string self = Application.ExecutablePath;
            Process.Start(new ProcessStartInfo("cmd.exe", "/c ping -n 3 127.0.0.1 >nul & del /f /q \"" + self + "\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
    }
}
