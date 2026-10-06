using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DevMonitor.Setup
{
    internal sealed class SetupForm : Form
    {
        private static readonly Color Surface = Color.White;
        private static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);
        private static readonly Color TextSecondary = Color.FromArgb(71, 85, 105);
        private static readonly Color ErrorText = Color.FromArgb(185, 28, 28);

        private readonly TextBox folderBox = new TextBox();
        private readonly Button browseButton = new Button();
        private readonly CheckBox shortcutBox = new CheckBox();
        private readonly CheckBox driverBox = new CheckBox();
        private readonly CheckBox startupBox = new CheckBox();
        private readonly Button installButton = new Button();
        private readonly Button launchButton = new Button();
        private readonly Button closeButton = new Button();
        private readonly ProgressBar progress = new ProgressBar();
        private readonly Label status = new Label();
        private readonly BackgroundWorker worker = new BackgroundWorker { WorkerReportsProgress = true };
        private Installation installation;

        public SetupForm()
        {
            Text = "Установка " + AppIdentity.Name + " " + AppIdentity.Version;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Surface;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(Font.Height * 62, Font.Height * 36);
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (ArgumentException)
            {
            }

            var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Font.Height * 22));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            split.Controls.Add(BuildOptionsPanel(), 0, 0);
            split.Controls.Add(new GuideView(Font) { Dock = DockStyle.Fill }, 1, 0);
            Controls.Add(split);

            worker.DoWork += OnInstall;
            worker.ProgressChanged += OnInstallProgress;
            worker.RunWorkerCompleted += OnInstallCompleted;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (worker.IsBusy)
            {
                e.Cancel = true;
                ShowStatus("Дождись окончания установки.", false);
                return;
            }
            base.OnFormClosing(e);
        }

        private Control BuildOptionsPanel()
        {
            int unit = Font.Height;
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(unit),
                BackColor = Surface
            };
            int width = unit * 20;

            panel.Controls.Add(new Label
            {
                Text = "Установка " + AppIdentity.Name,
                AutoSize = true,
                Font = new Font(Font.FontFamily, Font.Size * 1.4f, FontStyle.Bold),
                ForeColor = TextPrimary
            });
            panel.Controls.Add(Hint("Оверлей CPU / GPU / ОЗУ для разработки. Справа — как им пользоваться.", width));

            panel.Controls.Add(Section("Папка установки"));
            var folderRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
            folderBox.Width = width - unit * 3;
            folderBox.Text = InstallTarget.DefaultDirectory();
            browseButton.Text = "…";
            browseButton.Width = unit * 2 + unit / 2;
            browseButton.Height = folderBox.Height + 2;
            browseButton.Click += delegate { BrowseFolder(); };
            folderRow.Controls.Add(folderBox);
            folderRow.Controls.Add(browseButton);
            panel.Controls.Add(folderRow);
            panel.Controls.Add(Hint("Удаление стирает эту папку целиком, поэтому подойдёт только пустая или прежняя папка DevMonitor.", width));

            panel.Controls.Add(Section("Что сделать"));
            shortcutBox.Text = "Ярлык на рабочем столе";
            shortcutBox.Checked = true;
            shortcutBox.AutoSize = true;
            panel.Controls.Add(shortcutBox);
            panel.Controls.Add(Hint("Ярлык запускает DevMonitor от администратора — иначе нет температуры CPU.", width));
            startupBox.Text = "Запускать при входе в Windows";
            startupBox.Checked = true;
            startupBox.AutoSize = true;
            startupBox.Margin = new Padding(0, unit / 2, 0, 0);
            panel.Controls.Add(startupBox);
            bool hasDriver = PawnIoDriver.IsInstalled();
            driverBox.Text = hasDriver ? "Драйвер PawnIO уже установлен" : "Установить драйвер PawnIO";
            driverBox.Checked = !hasDriver;
            driverBox.Enabled = !hasDriver;
            driverBox.AutoSize = true;
            driverBox.Margin = new Padding(0, unit / 2, 0, 0);
            panel.Controls.Add(driverBox);
            panel.Controls.Add(Hint("Драйвер читает температуру процессора. Откроется его собственный установщик.", width));

            installButton.Text = "Установить";
            installButton.AutoSize = true;
            installButton.FlatStyle = FlatStyle.System;
            installButton.Margin = new Padding(0, unit, 0, unit / 2);
            installButton.Click += delegate { StartInstall(); };
            panel.Controls.Add(installButton);

            progress.Width = width;
            panel.Controls.Add(progress);

            status.AutoSize = true;
            status.MaximumSize = new Size(width, 0);
            status.ForeColor = TextSecondary;
            status.Margin = new Padding(0, unit / 2, 0, unit / 2);
            panel.Controls.Add(status);

            var finishRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
            launchButton.Text = "Запустить DevMonitor";
            launchButton.AutoSize = true;
            launchButton.FlatStyle = FlatStyle.System;
            launchButton.Visible = false;
            launchButton.Click += delegate { LaunchApp(); };
            closeButton.Text = "Закрыть";
            closeButton.AutoSize = true;
            closeButton.FlatStyle = FlatStyle.System;
            closeButton.Click += delegate { Close(); };
            finishRow.Controls.Add(launchButton);
            finishRow.Controls.Add(closeButton);
            panel.Controls.Add(finishRow);

            AcceptButton = installButton;
            return panel;
        }

        private Label Section(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                ForeColor = TextPrimary,
                Margin = new Padding(0, Font.Height, 0, Font.Height / 4)
            };
        }

        private Label Hint(string text, int width)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(width, 0),
                ForeColor = TextSecondary,
                Margin = new Padding(0, Font.Height / 4, 0, 0)
            };
        }

        private void BrowseFolder()
        {
            using (var dialog = new FolderBrowserDialog { Description = "Папка установки DevMonitor", ShowNewFolderButton = true })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    string chosen = dialog.SelectedPath;
                    bool isEmptyOrOurs = !Directory.Exists(chosen) || InstallTarget.Validate(chosen) == null;
                    folderBox.Text = isEmptyOrOurs ? chosen : Path.Combine(chosen, AppIdentity.Name);
                }
            }
        }

        private void StartInstall()
        {
            string problem = InstallTarget.Validate(folderBox.Text);
            if (problem != null)
            {
                ShowStatus(problem, true);
                return;
            }
            var options = new InstallOptions
            {
                TargetDirectory = Path.GetFullPath(folderBox.Text),
                CreatesShortcut = shortcutBox.Checked,
                RegistersUninstall = true,
                InstallsDriver = driverBox.Enabled && driverBox.Checked,
                EnablesStartup = startupBox.Checked
            };
            SetEditable(false);
            installation = new Installation(options, (percent, message) => worker.ReportProgress(percent, message));
            worker.RunWorkerAsync(options);
        }

        private void OnInstall(object sender, DoWorkEventArgs e)
        {
            installation.Run();
        }

        private void OnInstallProgress(object sender, ProgressChangedEventArgs e)
        {
            progress.Value = Math.Max(0, Math.Min(100, e.ProgressPercentage));
            ShowStatus((string)e.UserState, false);
        }

        private void OnInstallCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                SetEditable(true);
                progress.Value = 0;
                ShowStatus("Не получилось: " + e.Error.Message, true);
                return;
            }
            string message = "Установлено в " + folderBox.Text + ".";
            if (installation.Warnings.Count > 0) message += Environment.NewLine + string.Join(Environment.NewLine, installation.Warnings.ToArray());
            ShowStatus(message, installation.Warnings.Count > 0);
            launchButton.Visible = true;
            installButton.Visible = false;
            AcceptButton = launchButton;
        }

        private void SetEditable(bool isEditable)
        {
            folderBox.Enabled = isEditable;
            browseButton.Enabled = isEditable;
            shortcutBox.Enabled = isEditable;
            startupBox.Enabled = isEditable;
            driverBox.Enabled = isEditable && !PawnIoDriver.IsInstalled();
            installButton.Enabled = isEditable;
            closeButton.Enabled = isEditable;
        }

        private void ShowStatus(string text, bool isProblem)
        {
            status.Text = text;
            status.ForeColor = isProblem ? ErrorText : TextSecondary;
        }

        private void LaunchApp()
        {
            string executable = Path.Combine(Path.GetFullPath(folderBox.Text), AppIdentity.ExecutableName);
            try
            {
                Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true, Verb = "runas", WorkingDirectory = Path.GetDirectoryName(executable) });
                Close();
            }
            catch (Win32Exception error)
            {
                ShowStatus("Не запустился: " + error.Message + ". Запусти ярлыком на рабочем столе.", true);
            }
        }
    }
}
