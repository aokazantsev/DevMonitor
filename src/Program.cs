using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace DevMonitor
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool isFirstInstance;
            using (new Mutex(true, AppIdentity.SingleInstanceMutex, out isFirstInstance))
            {
                if (!isFirstInstance) return;
                using (Process current = Process.GetCurrentProcess())
                {
                    current.PriorityClass = ProcessPriorityClass.BelowNormal;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new OverlayForm());
            }
        }
    }
}
