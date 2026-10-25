using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace DevMonitor
{
    internal static class SetupProfile
    {
        private const string ShortcutKey = "shortcut";
        private const string DriverKey = "driver";
        private const string ResetSettingsKey = "resetSettings";
        private const int DeleteAttempts = 5;
        private const int DeleteRetryMs = 300;
        private static readonly string[] StaleSensorMarkers = { "sensor-gpu.pending", "sensor-gpu-limits.pending" };

        public const string Intro =
            "Оверлей поверх всех окон: загрузка и температура CPU и GPU, ОЗУ, видеопамять, память Android Studio, "
            + "Gradle- и Kotlin-демонов. История по минутам и статистика за день и неделю.";

        public static string Notice()
        {
            return null;
        }

        public static List<SetupField> Fields()
        {
            return new List<SetupField>();
        }

        public static List<SetupOption> Options()
        {
            bool hasDriver = PawnIoDriver.IsInstalled();
            string installed = UninstallRegistration.InstalledLocation();
            bool isUpdate = InstallTarget.IsUpdate(installed);
            return new List<SetupOption>
            {
                new SetupOption
                {
                    Key = ShortcutKey,
                    Text = "Ярлык на рабочем столе",
                    Hint = "Ярлык запускает DevMonitor от администратора — иначе нет температуры CPU.",
                    Checked = !isUpdate || File.Exists(DesktopShortcut.ShortcutPath())
                },
                new SetupOption
                {
                    Key = DriverKey,
                    Text = hasDriver ? "Драйвер PawnIO уже установлен" : "Установить драйвер PawnIO",
                    Hint = "Драйвер читает температуру процессора. Откроется его собственный установщик.",
                    Checked = !hasDriver && !isUpdate,
                    Enabled = !hasDriver
                },
                new SetupOption
                {
                    Key = ResetSettingsKey,
                    Text = "Сбросить настройки",
                    Hint = "Выбор процессора и видеокарты и положение окна вернутся к стандартным. История показателей и журнал "
                        + "сохранятся. Помогает, если сбой вызван настройками.",
                    Checked = false
                },
                new SetupOption
                {
                    Key = CrashReportConsent.OptionKey,
                    Text = "Отправлять автору отчёты о сбоях",
                    Hint = "В отчёт попадает журнал программы, а в нём — имя пользователя и домена (или компьютера) Windows и пути к программе, её данным и скачанному обновлению. "
                        + "Не включай, если это запрещают правила твоей компании.",
                    DetailsTitle = "Что уходит в отчёте",
                    Details = "Отчёт уходит один раз — при падении программы — на aokazantsev.ru (сервер в России), "
                        + "без повторных попыток:\n"
                        + "• версия программы, Windows и .NET;\n"
                        + "• время и текст ошибки со стеком вызовов;\n"
                        + "• последние 100 КБ журнала %LOCALAPPDATA%\\DevMonitor\\log.txt: "
                        + "имя пользователя и домена (или компьютера) Windows, пути к программе и её данным, выбранные процессор и видеокарта, "
                        + "состояние датчиков, проверки, загрузки и запуск обновлений с путём к скачанному файлу;\n"
                        + "• IP-адрес, с которого пришёл отчёт.\n"
                        + "История показателей и файл настроек не отправляются. Отчёты видит только автор, хранятся последние 50 МБ. "
                        + "Изменить выбор — переустановить программу.",
                    Checked = CrashReportConsent.IsGiven
                },
                new SetupOption
                {
                    Key = UsageConsent.OptionKey,
                    Text = "Отправлять автору анонимную статистику использования",
                    Hint = "Помогает понять, сколько людей пользуется программой и какие функции нужны. Без имён, путей и адресов. "
                        + "Выключить можно и потом — в настройках программы.",
                    DetailsTitle = "Что уходит в статистике",
                    Details = "Раз в час, пока программа запущена, на aokazantsev.ru (сервер в России) уходят счётчики за текущие и неотправленные прошлые сутки (до недели):\n"
                        + "• случайный номер установки — он не связан с тобой и компьютером;\n"
                        + "• версия программы и Windows;\n"
                        + "• сколько раз за сутки программа запускалась и сама находила новую версию, сколько раз ты пользовался пунктами меню, оверлеем, статистикой, настройками и обновлением;\n"
                        + "• текущее состояние: включены ли автозапуск, проверка обновлений и отчёты о сбоях, запущена ли программа "
                        + "с правами администратора, показан ли оверлей, выбрано ли железо вручную, есть ли процессор и видеокарта "
                        + "в справочнике, читаются ли температура процессора и видеокарта.\n"
                        + "Имена пользователя и компьютера, пути, модели железа, показатели и история в статистику не попадают, IP-адрес сервер не хранит. "
                        + "Статистика хранится 400 суток, дальше — только итоги по дням.",
                    Checked = UsageConsent.InstallerDefault
                }
            };
        }

        public static void BeforeExtract(InstallRequest request, Action<int, string> report, List<string> notes)
        {
            try
            {
                CrashReportConsent.Set(request.Has(CrashReportConsent.OptionKey));
            }
            catch (IOException error)
            {
                throw new InvalidOperationException("Не удалось записать выбор об отчётах о сбоях в папку " + AppIdentity.DataDirectory + " (" + error.Message.TrimEnd('.') + ").");
            }
            catch (UnauthorizedAccessException error)
            {
                throw new InvalidOperationException("Не удалось записать выбор об отчётах о сбоях в папку " + AppIdentity.DataDirectory + " (" + error.Message.TrimEnd('.') + ").");
            }
            SetupLog.Append("crash reports: " + request.Has(CrashReportConsent.OptionKey));
            try
            {
                UsageConsent.Set(request.Has(UsageConsent.OptionKey));
            }
            catch (IOException error)
            {
                throw new InvalidOperationException("Не удалось записать выбор о статистике использования в папку " + AppIdentity.DataDirectory + " (" + error.Message.TrimEnd('.') + ").");
            }
            catch (UnauthorizedAccessException error)
            {
                throw new InvalidOperationException("Не удалось записать выбор о статистике использования в папку " + AppIdentity.DataDirectory + " (" + error.Message.TrimEnd('.') + ").");
            }
            SetupLog.Append("usage statistics: " + request.Has(UsageConsent.OptionKey));
        }

        public static void AfterExtract(InstallRequest request, Action<int, string> report, List<string> notes)
        {
            string target = request.TargetDirectory;
            foreach (string marker in StaleSensorMarkers)
            {
                string markerProblem = DeleteSetting(Path.Combine(AppIdentity.DataDirectory, marker));
                if (markerProblem != null) SetupLog.Append(marker + " not removed: " + markerProblem);
            }
            SetupLog.Append("reset settings: " + request.Has(ResetSettingsKey));
            if (request.Has(ResetSettingsKey))
            {
                report(78, "Сброс настроек…");
                string settingsPath = Path.Combine(AppIdentity.DataDirectory, "settings.txt");
                string settingsProblem = DeleteSetting(settingsPath);
                SetupLog.Append("settings.txt reset: " + (settingsProblem ?? "done"));
                if (settingsProblem != null) throw new InvalidOperationException("Не удалось сбросить настройки — файл " + settingsPath + " не удалился (" + settingsProblem + ").");
                string positionPath = Path.Combine(AppIdentity.DataDirectory, "position.txt");
                string positionProblem = DeleteSetting(positionPath);
                SetupLog.Append("position.txt reset: " + (positionProblem ?? "done"));
                if (positionProblem != null) notes.Add("Положение окна не сброшено — файл " + positionPath + " не удалился (" + positionProblem + ").");
            }
            if (request.Has(ShortcutKey))
            {
                report(80, "Ярлык на рабочем столе…");
                string problem = DesktopShortcut.Create(target);
                SetupLog.Append("desktop shortcut: " + (problem ?? "created"));
                if (problem != null) notes.Add("Ярлык на рабочем столе не создан (" + problem + "). Без него DevMonitor запускается без прав — не будет температуры CPU.");
            }
            else if (File.Exists(DesktopShortcut.ShortcutPath()))
            {
                string problem = DesktopShortcut.Delete();
                SetupLog.Append("desktop shortcut unchecked: " + (problem ?? "removed"));
                if (problem != null) notes.Add("Ярлык на рабочем столе не удалён (" + problem + ") — удали его вручную: он может вести в прежнюю папку программы.");
            }
            if (request.Has(DriverKey) && !PawnIoDriver.IsInstalled())
            {
                report(84, "Установка драйвера PawnIO — пройди его установщик…");
                string setupPath = Path.Combine(target, AppIdentity.PawnIoSetupRelativePath);
                string problem = PawnIoDriver.RunSetup(setupPath);
                SetupLog.Append("pawnio driver: " + (problem ?? "installed"));
                if (problem != null)
                {
                    notes.Add("Драйвер PawnIO: " + problem + ". Без него не будет температуры CPU. Поставить позже: " + setupPath);
                }
            }
        }

        public static void AfterLaunch(InstallRequest request, Action<int, string> report, List<string> notes)
        {
        }

        private static string DeleteSetting(string path)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    if (!File.Exists(path)) return null;
                    FileAttributes attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.ReadOnly) != 0) File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
                    File.Delete(path);
                    return null;
                }
                catch (DirectoryNotFoundException)
                {
                    return null;
                }
                catch (FileNotFoundException)
                {
                    return null;
                }
                catch (IOException error)
                {
                    if (attempt >= DeleteAttempts) return "файл занят другой программой: " + error.Message.TrimEnd('.');
                }
                catch (UnauthorizedAccessException error)
                {
                    return "нет доступа: " + error.Message.TrimEnd('.');
                }
                Thread.Sleep(DeleteRetryMs);
            }
        }

        public static void Launch(string executable)
        {
            Process.Start(new ProcessStartInfo(executable) { WorkingDirectory = Path.GetDirectoryName(executable), UseShellExecute = false });
        }
    }
}
