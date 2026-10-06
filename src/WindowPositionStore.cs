using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace DevMonitor
{
    internal sealed class WindowPositionStore
    {
        private const int ScreenMargin = 16;

        private readonly string filePath = UserDataPaths.Position;

        public Point Load(Size windowSize)
        {
            Point saved;
            if (TryReadSaved(out saved) && IsOnAnyScreen(new Rectangle(saved, windowSize))) return saved;
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            return new Point(workingArea.Right - windowSize.Width - ScreenMargin, workingArea.Top + ScreenMargin);
        }

        public void Save(Point location)
        {
            try
            {
                File.WriteAllText(filePath, string.Format(CultureInfo.InvariantCulture, "{0} {1}", location.X, location.Y));
            }
            catch (Exception)
            {
            }
        }

        private bool TryReadSaved(out Point location)
        {
            location = Point.Empty;
            try
            {
                if (!File.Exists(filePath)) return false;
                string[] parts = File.ReadAllText(filePath).Trim().Split(' ');
                int x, y;
                if (parts.Length != 2
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out x)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out y))
                {
                    return false;
                }
                location = new Point(x, y);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsOnAnyScreen(Rectangle bounds)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(bounds)) return true;
            }
            return false;
        }
    }
}
