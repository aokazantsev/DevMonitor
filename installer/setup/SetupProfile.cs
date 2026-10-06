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
                }
            };
        }

        public static void BeforeExtract(InstallRequest request, Action<int, string> report, List<string> notes)
        {
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
