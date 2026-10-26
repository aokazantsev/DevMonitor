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
        private readonly CheckBox autoUpdate = new CheckBox();
        private readonly CheckBox usageStatistics = new CheckBox();
        private readonly Action optionsChanged;
        private string readProblem;
        private bool hardwareNeedsSave;

        public SettingsForm(Action optionsChanged)
        {
            this.optionsChanged = optionsChanged;
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
            if (saved.ReadError != null) readProblem = "Не удалось прочитать " + AppSettings.FilePath + ": " + saved.ReadError + " В списках — то, что действует сейчас.";
            FillBox(cpuBox, HardwareKind.Cpu, saved.CpuProfileName ?? ActiveHardware.Cpu.Name);
            FillBox(gpuBox, HardwareKind.Gpu, saved.GpuProfileName ?? ActiveHardware.Gpu.Name);
            hardwareNeedsSave = readProblem != null || IsMissing(HardwareKind.Cpu, saved.CpuProfileName) || IsMissing(HardwareKind.Gpu, saved.GpuProfileName);
            cpuBox.SelectedIndexChanged += delegate { OnHardwareChosen(); };
            gpuBox.SelectedIndexChanged += delegate { OnHardwareChosen(); };
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

            layout.Controls.Add(Section("Программа"));
            layout.Controls.Add(Option(autoUpdate, "Раз в сутки проверять обновление и сообщать о новой версии", AutoUpdateCheck.IsEnabled));
            layout.Controls.Add(Option(usageStatistics, "Отправлять автору анонимную статистику использования", UsageConsent.IsGiven));

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

        private CheckBox Option(CheckBox box, string text, bool isChecked)
        {
            box.Text = text;
            box.AutoSize = true;
            box.Checked = isChecked;
            box.ForeColor = StatisticsStyle.TextPrimary;
            box.Margin = new Padding(0, Font.Height / 4, 0, 0);
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
        }

        private bool IsMissing(HardwareKind kind, string savedName)
        {
            return savedName != null && catalog.Count > 0 && HardwareCatalog.Find(catalog, kind, savedName) == null;
        }

        private void OnHardwareChosen()
        {
            hardwareNeedsSave = true;
            OnSelectionChanged();
        }

        private void OnSelectionChanged()
        {
            cpuZones.Text = ZonesText(cpuBox.SelectedItem as HardwareProfile);
            gpuZones.Text = ZonesText(gpuBox.SelectedItem as HardwareProfile);
            status.Text = catalog.Count == 0
                ? "Справочник hardware.csv не найден рядом с DevMonitor.exe — выбирать не из чего."
                : "Сейчас действует: " + ActiveHardware.Cpu.Name + ", " + ActiveHardware.Gpu.Name
                  + ". Новый выбор применится после перезапуска DevMonitor.";
            if (readProblem != null) status.Text += Environment.NewLine + readProblem;
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
            var problems = new List<string>();
            string hardwareResult = SaveHardware(problems);
            string optionsProblem = SaveOptions();
            if (optionsProblem != null)
            {
                AppLog.Append("options not saved: " + optionsProblem);
                problems.Add("Не удалось сохранить выбор проверки обновлений и статистики: " + optionsProblem);
            }
            Usage.Hit("settings.save");
            optionsChanged();
            if (problems.Count == 0)
            {
                status.Text = hardwareResult;
                return;
            }
            string message = string.Join(Environment.NewLine, problems.ToArray());
            status.Text = message;
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private string SaveHardware(List<string> problems)
        {
            var cpu = cpuBox.SelectedItem as HardwareProfile;
            var gpu = gpuBox.SelectedItem as HardwareProfile;
            if (!hardwareNeedsSave) return "Сохранено.";
            if (catalog.Count == 0)
            {
                problems.Add("Железо не сохранено: справочник hardware.csv рядом с DevMonitor.exe не найден или пуст."
                    + (readProblem != null ? " " + readProblem : ""));
                return null;
            }
            if (cpu == null || gpu == null)
            {
                problems.Add("Железо не сохранено: выбери в списках и процессор, и видеокарту.");
                return null;
            }
            var settings = new AppSettings { CpuProfileName = cpu.Name, GpuProfileName = gpu.Name };
            string saveProblem = settings.Save();
            if (saveProblem != null)
            {
                AppLog.Append("settings not saved: " + saveProblem);
                problems.Add("Не удалось записать " + AppSettings.FilePath + ": " + saveProblem);
                return null;
            }
            hardwareNeedsSave = false;
            readProblem = null;
            bool isAlreadyActive = cpu.Name == ActiveHardware.Cpu.Name && gpu.Name == ActiveHardware.Gpu.Name;
            return isAlreadyActive
                ? "Сохранено. Этот выбор уже действует."
                : "Сохранено. Применится после перезапуска: трей → Выход, затем ярлык DevMonitor.";
        }

        private string SaveOptions()
        {
            try
            {
                if (autoUpdate.Checked != AutoUpdateCheck.IsEnabled) AutoUpdateCheck.Set(autoUpdate.Checked);
                if (usageStatistics.Checked != UsageConsent.IsGiven) Usage.SetConsent(usageStatistics.Checked);
                return null;
            }
            catch (System.IO.IOException error)
            {
                return error.Message;
            }
            catch (UnauthorizedAccessException error)
            {
                return error.Message;
            }
        }
    }
}
