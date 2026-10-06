using System;
using System.IO;

namespace DevMonitor.Setup
{
    internal static class AppIdentity
    {
        public const string Name = "DevMonitor";
        public const string Version = "1.1";
        public const string ExecutableName = "DevMonitor.exe";
        public const string ProcessName = "DevMonitor";
        public const string UninstallerName = "Uninstall.exe";
        public const string ShortcutName = "DevMonitor.lnk";
        public const string PawnIoSetupRelativePath = @"installer\redist\PawnIO_setup.exe";

        public static string DefaultInstallDirectory
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Name); }
        }

        public static string UserDataDirectory
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name); }
        }
    }
}
