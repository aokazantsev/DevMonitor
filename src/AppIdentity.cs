using System;
using System.IO;

namespace DevMonitor
{
    internal static class AppIdentity
    {
        public const string Name = "DevMonitor";
        public const string Version = "1.16";
        public const string UpdateSetupArguments = "";
        public const string UpdateKeeps = "настройки и история сохранятся";
        public const string ExecutableName = "DevMonitor.exe";
        public const string ProcessName = "DevMonitor";
        public const string UninstallerName = "Uninstall.exe";
        public const string SingleInstanceMutex = "DevMonitor.SingleInstance";
        public const string GitHubUrl = "https://github.com/aokazantsev/DevMonitor";
        public const string SiteUrl = "https://aokazantsev.ru/pets/devmonitor/";
        public const string AutostartDelay = "PT20S";
        public const string ShortcutName = "DevMonitor.lnk";
        public const string PawnIoSetupRelativePath = @"installer\redist\PawnIO_setup.exe";
        public static readonly string[] PayloadFiles = { ExecutableName, ExecutableName + ".config", UninstallerName, "README.md", "LICENSE", "CHANGELOG.md", "THIRD_PARTY_NOTICES.md", "hardware.csv", "lib", "installer" };

        public static string DefaultInstallDirectory
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Name); }
        }

        public static string DataDirectory
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name); }
        }
    }
}
