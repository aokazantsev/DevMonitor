using System.Windows.Forms;

namespace DevMonitor
{
    internal static class PowerSource
    {
        public static bool IsOnBattery()
        {
            return SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline;
        }
    }
}
