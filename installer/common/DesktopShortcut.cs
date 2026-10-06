using System;
using System.IO;
using System.Reflection;

namespace DevMonitor.Setup
{
    internal static class DesktopShortcut
    {
        private const int LinkFlagsOffset = 0x15;
        private const byte RunAsAdministratorFlag = 0x20;

        public static string ShortcutPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppIdentity.ShortcutName);
        }

        public static void Create(string installDirectory)
        {
            string executable = Path.Combine(installDirectory, AppIdentity.ExecutableName);
            Type shellType = Type.GetTypeFromProgID("WScript.Shell", true);
            object shell = Activator.CreateInstance(shellType);
            object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { ShortcutPath() });
            Type shortcutType = shortcut.GetType();
            SetProperty(shortcutType, shortcut, "TargetPath", executable);
            SetProperty(shortcutType, shortcut, "WorkingDirectory", installDirectory);
            SetProperty(shortcutType, shortcut, "IconLocation", executable + ",0");
            SetProperty(shortcutType, shortcut, "Description", "Оверлей CPU / GPU / ОЗУ");
            shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            MarkRunAsAdministrator(ShortcutPath());
        }

        public static void Delete()
        {
            string path = ShortcutPath();
            if (File.Exists(path)) File.Delete(path);
        }

        private static void SetProperty(Type type, object target, string name, string value)
        {
            type.InvokeMember(name, BindingFlags.SetProperty, null, target, new object[] { value });
        }

        private static void MarkRunAsAdministrator(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            bytes[LinkFlagsOffset] |= RunAsAdministratorFlag;
            File.WriteAllBytes(path, bytes);
        }
    }
}
