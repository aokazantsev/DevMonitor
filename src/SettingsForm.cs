using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DevMonitor
{
    internal sealed class SettingsForm : Form
    {
        private readonly List<HardwareProfile> catalog = HardwareCatalog.Load();
        private readonly ComboBox cpuBox = new ComboBox();
        private readonly ComboBox gpuBox = new ComboBox();
        private readonly Label cpuZones = new Label();
        private readonly Label gpuZones = new Label();
        private readonly Label status = new Label();
        private readonly Button saveButton = new Button();

        public SettingsForm()
        {
            Text = "DevMonitor — настройки";
            Font = new Font("Segoe UI", 9.5f);
            BackColor = StatisticsStyle.Surface;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (ArgumentException)
            {
            }

            Controls.Add(BuildLayout());
            AppSettings saved = AppSettings.Load();
            FillBox(cpuBox, HardwareKind.Cpu, saved.CpuProfileName ?? ActiveHardware.Cpu.Name);
            FillBox(gpuBox, HardwareKind.Gpu, saved.GpuProfileName ?? ActiveHardware.Gpu.Name);
            cpuBox.SelectedIndexChanged += delegate { OnSelectionChanged(); };
            gpuBox.SelectedIndexChanged += delegate { OnSelectionChanged(); };
            OnSelectionChanged();
        }

        private Control BuildLayout()
        {
            int unit = Font.Height;
            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Padding = new Padding(unit),
                BackColor = StatisticsStyle.Surface
            };

            layout.Controls.Add(Caption("Железо, под которое красятся температуры", true));
            layout.Controls.Add(Caption("Список — только модели, которые DevMonitor умеет читать (справочник hardware.csv).", false));

            layout.Controls.Add(Section("Процессор"));
            layout.Controls.Add(ConfigureBox(cpuBox));
            layout.Controls.Add(Detail(cpuZones));
            layout.Controls.Add(Caption("В этом ПК: " + (InstalledHardware.ProcessorName() ?? "не определён"), false));

            layout.Controls.Add(Section("Видеокарта"));
            layout.Controls.Add(ConfigureBox(gpuBox));
            layout.Controls.Add(Detail(gpuZones));
            List<string> adapters = InstalledHardware.DisplayAdapterNames();
            layout.Controls.Add(Caption("В этом ПК: " + (adapters.Count == 0 ? "не определена" : string.Join(", ", adapters.ToArray())), false));

            status.AutoSize = true;
            status.MaximumSize = new Size(unit * 34, 0);
            status.ForeColor = StatisticsStyle.TextPrimary;
            status.Margin = new Padding(0, unit, 0, unit / 2);
            layout.Controls.Add(status);

            var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, WrapContents = false };
            var closeButton = new Button { Text = "Закрыть", AutoSize = true, FlatStyle = FlatStyle.System };
            closeButton.Click += delegate { Close(); };
            saveButton.Text = "Сохранить";
            saveButton.AutoSize = true;
            saveButton.FlatStyle = FlatStyle.System;
            saveButton.Click += delegate { Save(); };
            buttons.Controls.Add(closeButton);
            buttons.Controls.Add(saveButton);
            layout.Controls.Add(buttons);
            AcceptButton = saveButton;
            CancelButton = closeButton;
            return layout;
        }

        private ComboBox ConfigureBox(ComboBox box)
        {
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.Width = Font.Height * 24;
            box.MaxDropDownItems = 20;
            box.Margin = new Padding(0, Font.Height / 4, 0, Font.Height / 4);
            return box;
        }

        private Label Detail(Label label)
        {
            label.AutoSize = true;
            label.MaximumSize = new Size(Font.Height * 34, 0);
            label.ForeColor = StatisticsStyle.TextPrimary;
            return label;
        }

        private Label Section(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                ForeColor = StatisticsStyle.TextPrimary,
                Margin = new Padding(0, Font.Height, 0, 0)
            };
        }

        private Label Caption(string text, bool isTitle)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(Font.Height * 34, 0),
                Font = isTitle ? new Font(Font.FontFamily, Font.Size * 1.15f, FontStyle.Bold) : Font,
                ForeColor = isTitle ? StatisticsStyle.TextPrimary : StatisticsStyle.TextSecondary,
                Margin = new Padding(0, isTitle ? 0 : Font.Height / 6, 0, 0)
            };
        }

        private void FillBox(ComboBox box, HardwareKind kind, string selectedName)
        {
            foreach (HardwareProfile profile in HardwareCatalog.OfKind(catalog, kind)) box.Items.Add(profile);
            HardwareProfile selected = HardwareCatalog.Find(catalog, kind, selectedName);
            if (selected != null) box.SelectedItem = selected;
            else if (box.Items.Count > 0) box.SelectedIndex = 0;
        }

        private void OnSelectionChanged()
        {
            cpuZones.Text = ZonesText(cpuBox.SelectedItem as HardwareProfile);
            gpuZones.Text = ZonesText(gpuBox.SelectedItem as HardwareProfile);
            saveButton.Enabled = cpuBox.SelectedItem != null && gpuBox.SelectedItem != null;
            status.Text = catalog.Count == 0
                ? "Справочник hardware.csv не найден рядом с DevMonitor.exe — выбирать не из чего."
                : "Сейчас действует: " + ActiveHardware.Cpu.Name + ", " + ActiveHardware.Gpu.Name
                  + ". Новый выбор применится после перезапуска DevMonitor.";
        }

        private static string ZonesText(HardwareProfile profile)
        {
            if (profile == null) return StatisticsStyle.NoValue;
            if (!profile.UsesDriverLimits) return ZonesLine(profile.Zones) + "  ·  " + profile.Note;
            string description;
            TemperatureZones zones = DriverZones.Resolve(out description);
            return "Пороги из драйвера этого ПК: " + description + " → " + ZonesLine(zones).ToLowerInvariant();
        }

        private static string ZonesLine(TemperatureZones zones)
        {
            return "Янтарный от " + zones.Warning.ToString("0") + "°, красный от " + zones.Critical.ToString("0") + "°";
        }

        private void Save()
        {
            var cpu = (HardwareProfile)cpuBox.SelectedItem;
            var gpu = (HardwareProfile)gpuBox.SelectedItem;
            var settings = new AppSettings { CpuProfileName = cpu.Name, GpuProfileName = gpu.Name };
            if (!settings.TrySave())
            {
                status.Text = "Не удалось записать settings.txt рядом с DevMonitor.exe.";
                return;
            }
            bool isAlreadyActive = cpu.Name == ActiveHardware.Cpu.Name && gpu.Name == ActiveHardware.Gpu.Name;
            status.Text = isAlreadyActive
                ? "Сохранено. Этот выбор уже действует."
                : "Сохранено. Применится после перезапуска: трей → Выход, затем ярлык DevMonitor.";
        }
    }
}
