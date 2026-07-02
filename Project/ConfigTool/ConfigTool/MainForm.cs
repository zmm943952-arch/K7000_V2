using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ConfigTool
{
    public sealed class MainForm : Form
    {
        private readonly TextBox _configPathTextBox = new TextBox();
        private readonly TextBox _rfp1MesTextBox = new TextBox();
        private readonly TextBox _rfp2MesTextBox = new TextBox();
        private readonly TextBox _rfp1FilesTextBox = new TextBox();
        private readonly TextBox _rfp2FilesTextBox = new TextBox();
        private readonly TextBox _redCaseMesTextBox = new TextBox();
        private readonly TextBox _redCaseBinTextBox = new TextBox();
        private readonly Label _statusLabel = new Label();
        private readonly List<string> _rfp1Files = new List<string>();
        private readonly List<string> _rfp2Files = new List<string>();

        public MainForm()
        {
            Text = "烧录配置工具";
            var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (appIcon != null)
            {
                Icon = appIcon;
            }
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(760, 430);
            Size = new Size(860, 470);
            Font = new Font("Microsoft YaHei UI", 9F);

            BuildLayout();
            _configPathTextBox.Text = ConfigService.FindDefaultConfigPath();
            LoadConfig();
        }

        private void BuildLayout()
        {
            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 5
            };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(main);

            var configPanel = CreateRowPanel();
            configPanel.Controls.Add(CreateLabel("配置文件", 80));
            _configPathTextBox.ReadOnly = true;
            configPanel.Controls.Add(Fill(_configPathTextBox));
            configPanel.Controls.Add(CreateButton("浏览...", BrowseConfig));
            configPanel.Controls.Add(CreateButton("重新读取", LoadConfig));
            main.Controls.Add(configPanel, 0, 0);

            var rfpGroup = new GroupBox { Text = "RFP 烧录配置", Dock = DockStyle.Fill };
            var rfpPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, Padding = new Padding(8) };
            rfpPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            rfpPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            rfpPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            rfpPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            rfpPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            rfpPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            rfpPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            rfpPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            rfpPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            rfpGroup.Controls.Add(rfpPanel);
            AddRfpRow(rfpPanel, 0, "k7000_1 MES", _rfp1MesTextBox, BrowseRfp1Mes, () => BrowseRfpFiles(_rfp1MesTextBox, _rfp1Files, "k7000_1"), () => ClearRfpFiles(_rfp1Files, "k7000_1"));
            AddFilesDisplayRow(rfpPanel, 1, "k7000_1 文件", _rfp1FilesTextBox);
            AddRfpRow(rfpPanel, 2, "k7000_2 MES", _rfp2MesTextBox, BrowseRfp2Mes, () => BrowseRfpFiles(_rfp2MesTextBox, _rfp2Files, "k7000_2"), () => ClearRfpFiles(_rfp2Files, "k7000_2"));
            AddFilesDisplayRow(rfpPanel, 3, "k7000_2 文件", _rfp2FilesTextBox);
            main.Controls.Add(rfpGroup, 0, 1);

            var redCaseGroup = new GroupBox { Text = "RedCase / TCON 烧录配置", Dock = DockStyle.Fill };
            var redCasePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(8) };
            redCasePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            redCasePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            redCasePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            redCasePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            redCasePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            redCaseGroup.Controls.Add(redCasePanel);
            AddPathRow(redCasePanel, 0, "MES 路径", _redCaseMesTextBox, BrowseRedCaseMes);
            AddPathRow(redCasePanel, 1, "BIN 文件", _redCaseBinTextBox, BrowseRedCaseBin);
            main.Controls.Add(redCaseGroup, 0, 2);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            actions.Controls.Add(CreateButton("保存配置", SaveConfig));
            actions.Controls.Add(CreateButton("退出", Close));
            main.Controls.Add(actions, 0, 3);

            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.ForeColor = Color.DimGray;
            _statusLabel.Text = "请选择路径后保存。";
            main.Controls.Add(_statusLabel, 0, 4);
        }

        private static FlowLayoutPanel CreateRowPanel()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
        }

        private static Label CreateLabel(string text, int width)
        {
            return new Label
            {
                Text = text,
                Width = width,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static Control Fill(TextBox textBox)
        {
            textBox.Width = 500;
            textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            return textBox;
        }

        private static Button CreateButton(string text, Action action)
        {
            var button = new Button { Text = text, Width = 82, Height = 28 };
            button.Click += (sender, args) => action();
            return button;
        }

        private static void AddPathRow(TableLayoutPanel panel, int row, string label, TextBox textBox, Action browseAction)
        {
            panel.Controls.Add(CreateLabel(label, 100), 0, row);
            textBox.Dock = DockStyle.Fill;
            panel.Controls.Add(textBox, 1, row);
            panel.Controls.Add(CreateButton("选择...", browseAction), 2, row);
        }

        private static void AddRfpRow(TableLayoutPanel panel, int row, string label, TextBox textBox, Action browseFolderAction, Action browseFilesAction, Action clearFilesAction)
        {
            panel.Controls.Add(CreateLabel(label, 100), 0, row);
            textBox.Dock = DockStyle.Fill;
            panel.Controls.Add(textBox, 1, row);
            panel.Controls.Add(CreateButton("选路径", browseFolderAction), 2, row);
            panel.Controls.Add(CreateButton("选文件", browseFilesAction), 3, row);
            panel.Controls.Add(CreateButton("清文件", clearFilesAction), 4, row);
        }

        private static void AddFilesDisplayRow(TableLayoutPanel panel, int row, string label, TextBox textBox)
        {
            panel.Controls.Add(CreateLabel(label, 100), 0, row);
            textBox.Dock = DockStyle.Fill;
            textBox.ReadOnly = true;
            textBox.BackColor = SystemColors.Window;
            textBox.ScrollBars = ScrollBars.Horizontal;
            panel.Controls.Add(textBox, 1, row);
            panel.SetColumnSpan(textBox, 4);
        }

        private void BrowseConfig()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Config.json|Config.json|JSON files (*.json)|*.json|All files (*.*)|*.*";
                dialog.FileName = "Config.json";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _configPathTextBox.Text = dialog.FileName;
                    LoadConfig();
                }
            }
        }

        private void BrowseRfp1Mes()
        {
            SelectFolderInto(_rfp1MesTextBox);
        }

        private void BrowseRfp2Mes()
        {
            SelectFolderInto(_rfp2MesTextBox);
        }

        private void BrowseRfpFiles(TextBox mesTextBox, List<string> selectedFiles, string projectLabel)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Firmware files (*.mot;*.srec;*.hex;*.bin)|*.mot;*.srec;*.hex;*.bin|All files (*.*)|*.*";
                dialog.Multiselect = true;
                dialog.Title = "选择 " + projectLabel + " 烧录文件";
                if (Directory.Exists(mesTextBox.Text))
                {
                    dialog.InitialDirectory = mesTextBox.Text;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    selectedFiles.Clear();
                    selectedFiles.AddRange(dialog.FileNames);
                    var firstDir = Path.GetDirectoryName(dialog.FileNames[0]);
                    if (!string.IsNullOrWhiteSpace(firstDir))
                    {
                        mesTextBox.Text = firstDir;
                    }
                    RefreshRfpFileDisplay();
                    SetStatus(projectLabel + " 已选择 " + selectedFiles.Count + " 个烧录文件：" + string.Join(", ", selectedFiles.Select(Path.GetFileName)), false);
                }
            }
        }

        private void ClearRfpFiles(List<string> selectedFiles, string projectLabel)
        {
            selectedFiles.Clear();
            RefreshRfpFileDisplay();
            SetStatus(projectLabel + " 已清除显式文件选择，保存后将按 MES 路径同名 fallback。", false);
        }

        private void BrowseRedCaseMes()
        {
            SelectFolderInto(_redCaseMesTextBox);
        }

        private void BrowseRedCaseBin()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "BIN files (*.bin)|*.bin|All files (*.*)|*.*";
                dialog.Title = "选择 RedCase BIN 文件";
                if (Directory.Exists(_redCaseMesTextBox.Text))
                {
                    dialog.InitialDirectory = _redCaseMesTextBox.Text;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _redCaseBinTextBox.Text = dialog.FileName;
                    _redCaseMesTextBox.Text = Path.GetDirectoryName(dialog.FileName) ?? _redCaseMesTextBox.Text;
                }
            }
        }

        private void SelectFolderInto(TextBox textBox)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.SelectedPath = Directory.Exists(textBox.Text) ? textBox.Text : "";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    textBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void LoadConfig()
        {
            try
            {
                var service = new ConfigService(_configPathTextBox.Text);
                var model = service.Load();
                _rfp1MesTextBox.Text = model.RfpProject1MesPath;
                _rfp2MesTextBox.Text = model.RfpProject2MesPath;
                _redCaseMesTextBox.Text = model.RedCaseMesPath;
                _redCaseBinTextBox.Text = model.RedCaseBinPath;
                _rfp1Files.Clear();
                _rfp2Files.Clear();
                _rfp1Files.AddRange(model.RfpProject1FirmwareFiles);
                _rfp2Files.AddRange(model.RfpProject2FirmwareFiles);
                RefreshRfpFileDisplay();
                SetStatus("配置已读取。", false);
            }
            catch (Exception ex)
            {
                SetStatus("读取配置失败：" + ex.Message, true);
            }
        }

        private void SaveConfig()
        {
            try
            {
                var model = new ConfigModel
                {
                    RfpProject1MesPath = _rfp1MesTextBox.Text.Trim(),
                    RfpProject2MesPath = _rfp2MesTextBox.Text.Trim(),
                    RedCaseMesPath = _redCaseMesTextBox.Text.Trim(),
                    RedCaseBinPath = _redCaseBinTextBox.Text.Trim()
                };
                model.RfpProject1FirmwareFiles.AddRange(_rfp1Files);
                model.RfpProject2FirmwareFiles.AddRange(_rfp2Files);

                var service = new ConfigService(_configPathTextBox.Text);
                service.Save(model);
                SetStatus("配置已保存：" + _configPathTextBox.Text, false);
                MessageBox.Show(this, "配置已保存。", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatus("保存配置失败：" + ex.Message, true);
                MessageBox.Show(this, ex.Message, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetStatus(string text, bool isError)
        {
            _statusLabel.ForeColor = isError ? Color.Firebrick : Color.DimGray;
            _statusLabel.Text = text;
        }

        private void RefreshRfpFileDisplay()
        {
            _rfp1FilesTextBox.Text = FormatFileList(_rfp1Files);
            _rfp2FilesTextBox.Text = FormatFileList(_rfp2Files);
        }

        private static string FormatFileList(IReadOnlyCollection<string> files)
        {
            return files.Count == 0 ? "" : string.Join(" ; ", files);
        }
    }
}
