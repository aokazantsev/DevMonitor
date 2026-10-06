using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DevMonitor
{
    internal static class SetupProfile
    {
        private const string ShortcutKey = "shortcut";
        private const string DriverKey = "driver";
        private const string ResetSettingsKey = "resetSettings";

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
            return new List<SetupOption>
            {
                new SetupOption
                {
                    Key = ShortcutKey,
                    Text = "Ярлык на рабочем столе",
                    Hint = "Ярлык запускает DevMonitor от администратора — иначе нет температуры CPU.",
                    Checked = true
                },
                new SetupOption
                {
                    Key = DriverKey,
                    Text = hasDriver ? "Драйвер PawnIO уже установлен" : "Установить драйвер PawnIO",
                    Hint = "Драйвер читает температуру процессора. Откроется его собственный установщик.",
                    Checked = !hasDriver,
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
                    Hint = "В отчёт попадает журнал программы, а в нём — имя пользователя и компьютера Windows и путь к программе. "
                        + "Не включай, если это запрещают правила твоей компании.",
                    DetailsTitle = "Что уходит в отчёте",
                    Details = "Отчёт уходит один раз — при падении программы — на aokazantsev.ru (сервер в России), "
                        + "без повторных попыток:\n"
                        + "• версия программы, Windows и .NET;\n"
                        + "• текст ошибки со стеком вызовов и последние 100 КБ журнала %LOCALAPPDATA%\\DevMonitor\\log.txt: "
                        + "имя пользователя и компьютера Windows, путь к программе, выбранные процессор и видеокарта, состояние датчиков;\n"
                        + "• IP-адрес, с которого пришёл отчёт.\n"
                        + "История показателей и настройки не отправляются. Отчёты видит только автор, хранятся последние 50 МБ. "
                        + "Изменить выбор — переустановить программу.",
                    Checked = CrashReportConsent.IsGiven
                }
            };
        }

        public static void BeforeExtract(InstallRequest request, Action<int, string> report, List<string> notes)
        {
            if (request.Has(ResetSettingsKey))
            {
                DeleteSetting("settings.txt");
                DeleteSetting("position.txt");
                SetupLog.Append("settings reset");
            }
            CrashReportConsent.Set(request.Has(CrashReportConsent.OptionKey));
            SetupLog.Append("crash reports: " + request.Has(CrashReportConsent.OptionKey));
        }

        private static void DeleteSetting(string name)
        {
            string path = Path.Combine(AppIdentity.DataDirectory, name);
            if (File.Exists(path)) File.Delete(path);
        }

        public static void AfterExtract(InstallRequest request, Action<int, string> report, List<string> notes)
        {
            string target = request.TargetDirectory;
            if (request.Has(ShortcutKey) || File.Exists(DesktopShortcut.ShortcutPath()))
            {
                report(80, "Ярлык на рабочем столе…");
                DesktopShortcut.Create(target);
            }
            if (request.Has(DriverKey) && !PawnIoDriver.IsInstalled())
            {
                report(84, "Установка драйвера PawnIO — пройди его установщик…");
                string setupPath = Path.Combine(target, AppIdentity.PawnIoSetupRelativePath);
                string problem = PawnIoDriver.RunSetup(setupPath);
                if (problem != null)
                {
                    notes.Add("Драйвер PawnIO: " + problem + ". Без него не будет температуры CPU. Поставить позже: " + setupPath);
                }
            }
        }

        public static void Launch(string executable)
        {
            Process.Start(new ProcessStartInfo(executable) { WorkingDirectory = Path.GetDirectoryName(executable), UseShellExecute = false });
        }
    }
}
