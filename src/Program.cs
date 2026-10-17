using System;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace DevMonitor
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            bool isFirstInstance;
            AppSettings settings;
            Mutex singleInstance;
            try
            {
                singleInstance = new Mutex(true, AppIdentity.SingleInstanceMutex, out isFirstInstance);
            }
            catch (UnauthorizedAccessException)
            {
                AppLog.Append("already running with administrator rights, second copy without rights exits");
                return;
            }
            using (singleInstance)
            {
                if (!isFirstInstance)
                {
                    AppLog.Append("already running, second copy exits, elevated=" + IsElevated());
                    return;
                }
                AppLog.Append("start " + AppIdentity.Version + ", user=" + Environment.UserDomainName + "\\" + Environment.UserName
                    + ", elevated=" + IsElevated() + ", os=" + Environment.OSVersion.VersionString
                    + ", 64bit=" + Environment.Is64BitProcess + ", exe=" + Application.ExecutablePath);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                settings = AppSettings.LoadPatiently(AppLog.Append);
                if (settings.ReadError == null)
                {
                    ActiveHardware.Select(settings);
                    Application.Run(new OverlayForm());
                }
                else
                {
                    AppLog.Append("settings not read, exit: " + settings.ReadError);
                }
            }
            if (settings.ReadError == null) return;
            MessageBox.Show("Не удалось прочитать настройки " + AppSettings.FilePath + ": " + settings.ReadError + Environment.NewLine + Environment.NewLine
                + settings.ReadAdvice + " Программа закрыта, после этого запусти " + AppIdentity.Name + " снова.",
                AppIdentity.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static bool IsElevated()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            CrashReport.Write("ui thread", e.Exception, false);
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            CrashReport.Write("unhandled", e.ExceptionObject as Exception, e.IsTerminating);
        }
    }
}
