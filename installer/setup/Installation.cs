using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace DevMonitor.Setup
{
    internal sealed class Installation
    {
        public const string PayloadResource = "DevMonitor.Payload.zip";

        private readonly InstallOptions options;
        private readonly Action<int, string> report;
        private readonly List<string> warnings = new List<string>();

        public Installation(InstallOptions options, Action<int, string> report)
        {
            this.options = options;
            this.report = report;
        }

        public List<string> Warnings
        {
            get { return warnings; }
        }

        public void Run()
        {
            string target = Path.GetFullPath(options.TargetDirectory);
            if (RunningApp.IsRunning())
            {
                throw new InvalidOperationException("DevMonitor сейчас запущен. Закрой его (значок в трее → «Выход») и нажми «Установить» ещё раз.");
            }

            report(0, "Перенос истории и настроек прежней версии…");
            string previous = UserDataMigration.PreviousInstallDirectory();
            UserDataMigration.MoveFrom(target);
            if (previous != null && !string.Equals(Path.GetFullPath(previous).TrimEnd('\\'), target.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase)
                && UserDataMigration.MoveFrom(previous))
            {
                warnings.Add("История и настройки перенесены из " + previous + " в " + AppIdentity.UserDataDirectory
                    + ". Прежнюю папку можно удалить вручную.");
            }

            report(2, "Распаковка файлов…");
            long size = ExtractPayload(target);

            if (options.CreatesShortcut || File.Exists(DesktopShortcut.ShortcutPath()))
            {
                report(88, "Ярлык на рабочем столе…");
                DesktopShortcut.Create(target);
            }
            if (options.RegistersUninstall)
            {
                report(92, "Запись в «Приложения» Windows…");
                UninstallRegistration.Register(target, size);
            }
            if (options.InstallsDriver && !PawnIoDriver.IsInstalled())
            {
                report(95, "Установка драйвера PawnIO — подтверди окно UAC и пройди установщик PawnIO…");
                string problem = PawnIoDriver.RunSetup(Path.Combine(target, AppIdentity.PawnIoSetupRelativePath));
                if (problem != null)
                {
                    warnings.Add("Драйвер PawnIO: " + problem + ". Без него не будет температуры CPU. Поставить позже: "
                        + Path.Combine(target, AppIdentity.PawnIoSetupRelativePath));
                }
            }
            if (StartupTask.IsEnabled())
            {
                report(98, "Автозапуск — на новую папку…");
                string startupProblem = StartupTask.Enable(Path.Combine(target, AppIdentity.ExecutableName));
                if (startupProblem != null) warnings.Add(startupProblem);
            }
            report(100, "Готово.");
        }

        private long ExtractPayload(string target)
        {
            Directory.CreateDirectory(target);
            string root = target.TrimEnd('\\') + "\\";
            long totalBytes = 0;
            using (Stream payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadResource))
            {
                if (payload == null) throw new InvalidOperationException("В установщике нет архива программы — он собран неправильно.");
                using (var archive = new ZipArchive(payload, ZipArchiveMode.Read))
                {
                    int index = 0;
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        index++;
                        string relative = entry.FullName.Replace('/', '\\');
                        string destination = Path.GetFullPath(Path.Combine(target, relative));
                        if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;
                        if (relative.EndsWith("\\"))
                        {
                            Directory.CreateDirectory(destination);
                            continue;
                        }
                        Directory.CreateDirectory(Path.GetDirectoryName(destination));
                        entry.ExtractToFile(destination, true);
                        totalBytes += entry.Length;
                        report(index * 85 / archive.Entries.Count, "Распаковка: " + relative);
                    }
                }
            }
            return totalBytes;
        }
    }
}
