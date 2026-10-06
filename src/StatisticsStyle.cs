using System;
using System.Drawing;
using System.Windows.Forms;

namespace DevMonitor
{
    internal static class StatisticsStyle
    {
        public const string NoValue = "—";

        public static readonly Color Surface = Color.White;
        public static readonly Color Toolbar = Color.FromArgb(248, 250, 252);
        public static readonly Color HeaderBack = Color.FromArgb(241, 245, 249);
        public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);
        public static readonly Color TextSecondary = Color.FromArgb(71, 85, 105);
        public static readonly Color TextDisabled = Color.FromArgb(148, 163, 184);
        public static readonly Color GridLine = Color.FromArgb(226, 232, 240);
        public static readonly Color Selection = Color.FromArgb(219, 234, 254);

        public static void ConfigureTable(DataGridView table, Font font, bool isSelectable)
        {
            table.Dock = DockStyle.Fill;
            table.ReadOnly = true;
            table.AllowUserToAddRows = false;
            table.AllowUserToDeleteRows = false;
            table.AllowUserToResizeRows = false;
            table.AllowUserToOrderColumns = false;
            table.RowHeadersVisible = false;
            table.MultiSelect = false;
            table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            table.BorderStyle = BorderStyle.None;
            table.BackgroundColor = Surface;
            table.GridColor = GridLine;
            table.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            table.EnableHeadersVisualStyles = false;
            table.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            table.ColumnHeadersDefaultCellStyle.BackColor = HeaderBack;
            table.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
            table.ColumnHeadersDefaultCellStyle.SelectionBackColor = HeaderBack;
            table.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            table.DefaultCellStyle.ForeColor = TextPrimary;
            table.DefaultCellStyle.SelectionBackColor = isSelectable ? Selection : Surface;
            table.DefaultCellStyle.SelectionForeColor = TextPrimary;
            table.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            table.RowTemplate.Height = (int)(font.Height * 1.55f);
        }

        public static void AddColumn(DataGridView table, string header, float weight, DataGridViewContentAlignment alignment)
        {
            var column = new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            column.DefaultCellStyle.Alignment = alignment;
            column.HeaderCell.Style.WrapMode = DataGridViewTriState.True;
            table.Columns.Add(column);
        }

        public static FlowLayoutPanel CreateToolbar(Font font)
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = Toolbar,
                Padding = new Padding(font.Height / 3),
                WrapContents = false
            };
        }

        public static ComboBox CreateMetricBox(Font font)
        {
            var box = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = font.Height * 18,
                Margin = new Padding(0, font.Height / 4, 0, 0)
            };
            foreach (MetricKind kind in MetricCatalog.All) box.Items.Add(MetricCatalog.LongTitle(kind));
            box.SelectedIndex = 0;
            return box;
        }

        public static Label CreateCaption(string text, Font font)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = TextSecondary,
                Margin = new Padding(font.Height, font.Height / 2, font.Height / 4, 0)
            };
        }

        public static Label CreateChartTitle(Font font)
        {
            return new Label
            {
                AutoSize = false,
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                Height = (int)(font.Height * 1.9f),
                TextAlign = ContentAlignment.BottomLeft,
                ForeColor = TextPrimary,
                Font = new Font(font.FontFamily, font.Size * 1.1f, FontStyle.Bold),
                Margin = new Padding(font.Height / 4, font.Height / 4, 0, font.Height / 4)
            };
        }

        public static TableLayoutPanel CreatePageLayout(Font font, float tableHeightInLines)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Surface,
                Padding = new Padding(font.Height / 2)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, font.Height * tableHeightInLines));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, font.Height * 2.4f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            return layout;
        }

        public static string SummaryCell(MetricKind kind, float? average, float? maximum)
        {
            return average.HasValue && maximum.HasValue
                ? MetricCatalog.Format(kind, average.Value) + " / " + MetricCatalog.Format(kind, maximum.Value)
                : NoValue;
        }

        public static string ValueCell(MetricKind kind, float? value)
        {
            return value.HasValue ? MetricCatalog.Format(kind, value.Value) : NoValue;
        }

        public static string Duration(int minutes)
        {
            int hours = minutes / 60;
            int rest = minutes % 60;
            return hours == 0 ? rest + " мин" : hours + " ч " + rest.ToString("00") + " мин";
        }
    }
}
