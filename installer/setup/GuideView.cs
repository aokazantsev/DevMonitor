using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace DevMonitor.Setup
{
    internal sealed class GuideView : UserControl
    {
        private const int AutoAdvanceMs = 9000;
        private const string ImageResourcePrefix = "DevMonitor.Guide.";

        private static readonly Color Surface = Color.FromArgb(248, 250, 252);
        private static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);
        private static readonly Color TextSecondary = Color.FromArgb(71, 85, 105);

        private readonly Label title = new Label();
        private readonly PictureBox picture = new PictureBox();
        private readonly Label text = new Label();
        private readonly Label counter = new Label();
        private readonly Timer autoAdvance = new Timer();
        private readonly Image fallbackImage;
        private int pageIndex;

        public GuideView(Font font)
        {
            Font = font;
            BackColor = Surface;
            Padding = new Padding(font.Height);
            fallbackImage = LoadImage("icon.png");

            int unit = font.Height;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Surface };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, unit * 2.2f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, unit * 6.5f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, unit * 2.4f));

            title.Dock = DockStyle.Fill;
            title.Font = new Font(font.FontFamily, font.Size * 1.3f, FontStyle.Bold);
            title.ForeColor = TextPrimary;
            title.TextAlign = ContentAlignment.MiddleLeft;

            picture.Dock = DockStyle.Fill;
            picture.SizeMode = PictureBoxSizeMode.Zoom;
            picture.BackColor = Surface;

            text.Dock = DockStyle.Fill;
            text.ForeColor = TextSecondary;
            text.Padding = new Padding(0, unit / 2, 0, 0);

            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(picture, 0, 1);
            layout.Controls.Add(text, 0, 2);
            layout.Controls.Add(BuildNavigation(), 0, 3);
            Controls.Add(layout);

            autoAdvance.Interval = AutoAdvanceMs;
            autoAdvance.Tick += delegate { ShowPage(pageIndex + 1); };
            ShowPage(0);
            autoAdvance.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                autoAdvance.Dispose();
                if (picture.Image != null && picture.Image != fallbackImage) picture.Image.Dispose();
                if (fallbackImage != null) fallbackImage.Dispose();
            }
            base.Dispose(disposing);
        }

        private Control BuildNavigation()
        {
            var navigation = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            var previous = new Button { Text = "‹ Назад", AutoSize = true, FlatStyle = FlatStyle.System };
            var next = new Button { Text = "Дальше ›", AutoSize = true, FlatStyle = FlatStyle.System };
            previous.Click += delegate { ShowPageByUser(pageIndex - 1); };
            next.Click += delegate { ShowPageByUser(pageIndex + 1); };
            counter.AutoSize = true;
            counter.ForeColor = TextSecondary;
            counter.Margin = new Padding(Font.Height, Font.Height / 2, Font.Height, 0);
            navigation.Controls.Add(previous);
            navigation.Controls.Add(counter);
            navigation.Controls.Add(next);
            return navigation;
        }

        private void ShowPageByUser(int index)
        {
            autoAdvance.Stop();
            ShowPage(index);
            autoAdvance.Start();
        }

        private void ShowPage(int index)
        {
            int count = GuidePages.All.Length;
            pageIndex = (index % count + count) % count;
            GuidePage page = GuidePages.All[pageIndex];
            title.Text = page.Title;
            text.Text = page.Text;
            counter.Text = (pageIndex + 1) + " / " + count;

            Image previousImage = picture.Image;
            picture.Image = page.ImageName == null ? fallbackImage : LoadImage(page.ImageName) ?? fallbackImage;
            if (previousImage != null && previousImage != fallbackImage) previousImage.Dispose();
        }

        private static Image LoadImage(string name)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ImageResourcePrefix + name);
            if (stream == null) return null;
            using (stream)
            using (var source = Image.FromStream(stream))
            {
                return new Bitmap(source);
            }
        }
    }
}
