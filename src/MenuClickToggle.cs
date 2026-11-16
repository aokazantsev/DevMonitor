using System.Windows.Forms;

namespace DevMonitor
{
    internal sealed class MenuClickToggle
    {
        private const int PressWindowMs = 250;
        private string openedKey;
        private string closedKey;
        private int closedAt;
        private string suppressedKey;

        public void Opened(string key)
        {
            openedKey = key;
        }

        public void OnClosed(ToolStripDropDownCloseReason reason, int now)
        {
            closedKey = reason == ToolStripDropDownCloseReason.AppClicked ? openedKey : null;
            closedAt = now;
            openedKey = null;
        }

        public void PressStarted(string key, int now)
        {
            int elapsed = unchecked(now - closedAt);
            suppressedKey = key != null && key == closedKey && elapsed >= 0 && elapsed < PressWindowMs ? key : null;
            closedKey = null;
        }

        public bool ConsumeRelease(string key)
        {
            bool suppressed = key != null && key == suppressedKey;
            suppressedKey = null;
            return suppressed;
        }
    }
}
