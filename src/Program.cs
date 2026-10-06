using System;
using System.Diagnostics;
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
            using (new Mutex(true, AppIdentity.SingleInstanceMutex, out isFirstInstance))
            {
                if (!isFirstInstance) return;
                AppLog.Append("start " + AppIdentity.Version + ", user=" + Environment.UserDomainName + "\\" + Environment.UserName
                    + ", elevated=" + IsElevated() + ", os=" + Environment.OSVersion.VersionString
                    + ", 64bit=" + Environment.Is64BitProcess + ", exe=" + Application.ExecutablePath);
                using (Process current = Process.GetCurrentProcess())
                {
                    current.PriorityClass = ProcessPriorityClass.BelowNormal;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new OverlayForm());
            }
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
