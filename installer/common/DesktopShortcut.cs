using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace DevMonitor
{
    internal static class DesktopShortcut
    {
        private const int LinkFlagsOffset = 0x15;
        private const byte RunAsAdministratorFlag = 0x20;
        private const int FlagAttempts = 5;
        private const int FlagRetryMs = 300;

        public static string ShortcutPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppIdentity.ShortcutName);
        }

        public static string Create(string installDirectory)
        {
            string path = ShortcutPath();
            string executable = Path.Combine(installDirectory, AppIdentity.ExecutableName);
            try
            {
                ClearReadOnly(path);
                Type shellType = Type.GetTypeFromProgID("WScript.Shell", true);
                object shell = Activator.CreateInstance(shellType);
                object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
                Type shortcutType = shortcut.GetType();
                SetProperty(shortcutType, shortcut, "TargetPath", executable);
                SetProperty(shortcutType, shortcut, "WorkingDirectory", installDirectory);
                SetProperty(shortcutType, shortcut, "IconLocation", executable + ",0");
                SetProperty(shortcutType, shortcut, "Description", "Оверлей CPU / GPU / ОЗУ");
                shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            }
            catch (TargetInvocationException error)
            {
                return (error.InnerException ?? error).Message;
            }
            catch (COMException error)
            {
                return error.Message;
            }
            catch (IOException error)
            {
                return error.Message;
            }
            catch (UnauthorizedAccessException error)
            {
                return error.Message;
            }
            string flagProblem = MarkRunAsAdministrator(path);
            if (flagProblem == null) return null;
            string deleteProblem = Delete();
            if (deleteProblem == null) return "не удалось пометить его «Запуск от имени администратора»: " + flagProblem;
            return "ярлык " + path + " остался без пометки «Запуск от имени администратора» (" + flagProblem + ") и не удалился (" + deleteProblem + ") — удали его вручную";
        }

        private static string MarkRunAsAdministrator(string path)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    bytes[LinkFlagsOffset] |= RunAsAdministratorFlag;
                    File.WriteAllBytes(path, bytes);
                    return null;
                }
                catch (IOException error)
                {
                    if (attempt >= FlagAttempts) return error.Message;
                }
                catch (UnauthorizedAccessException error)
                {
                    return error.Message;
                }
                Thread.Sleep(FlagRetryMs);
            }
        }

        public static string Delete()
        {
            string path = ShortcutPath();
            try
            {
                if (!File.Exists(path)) return null;
                ClearReadOnly(path);
                File.Delete(path);
                return null;
            }
            catch (IOException error)
            {
                return error.Message;
            }
            catch (UnauthorizedAccessException error)
            {
                return error.Message;
            }
        }

        private static void ClearReadOnly(string path)
        {
            if (!File.Exists(path)) return;
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReadOnly) != 0) File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
        }

        private static void SetProperty(Type type, object target, string name, string value)
        {
            type.InvokeMember(name, BindingFlags.SetProperty, null, target, new object[] { value });
        }
    }
}
