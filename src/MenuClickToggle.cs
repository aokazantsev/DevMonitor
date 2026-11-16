using System;
using System.Windows.Forms;

namespace DevMonitor
{
    internal sealed class MenuClickToggle
    {
        private bool closedByClick;
        private int closedAt;
        private string closedKey;

        public void OnClosed(ToolStripDropDownCloseReason reason, string key, int now)
        {
            closedByClick = reason == ToolStripDropDownCloseReason.AppClicked;
            closedAt = now;
            closedKey = key;
        }

        public bool ConsumeJustClosed(string key, int now, int window)
        {
            bool result = closedByClick && closedKey == key && unchecked(now - closedAt) >= 0 && unchecked(now - closedAt) < window;
            closedByClick = false;
            return result;
        }
    }
}
