using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using tarkov_settings.Setting;
using tarkov_settings.GPU;

namespace tarkov_settings
{
    public partial class MainForm : Form
    {
        private ProcessMonitor pMonitor = ProcessMonitor.Instance;
        private IGPU gpu = GPUDevice.Instance;
        private AppSetting appSetting;

        private bool minimizeOnStart = false;
        private bool _isLoadingProfile = false; // UI 값 로드 중 중복 이벤트 방지 플래그

        // 디스크 과부하 방지 지연 저장 타이머 (1초)
        private System.Windows.Forms.Timer _saveDebounceTimer;

        // 트레이 메뉴에 추가될 동적 적응형 메뉴 항목
        private ToolStripMenuItem _trayDynamicAdaptiveMenuItem;

        public MainForm()
        {
            InitializeComponent();

            // 1초 지연 저장 타이머 초기화
            _saveDebounceTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _saveDebounceTimer.Tick += (s, e) =>
            {
                _saveDebounceTimer.Stop();
                SaveCurrentSettingsToDisk(); // 조작 1초 뒤 디스크에 1번만 안전 저장
            };

            #region Load App Settings (다중 프로필 로드)
            appSetting = AppSetting.Load();
            minimizeOnStart = appSetting.minimizeOnStart;
            this.minimizeStartCheckBox.Checked = minimizeOnStart;

            // 프로필 드롭다운 초기화 및 첫 프로필 UI 로드
            PopulateProfileComboBox();
            #endregion

            // 트레이 우클릭 메뉴 상단에 Dynamic Adaptive Boost 메뉴 추가
            _trayDynamicAdaptiveMenuItem = new ToolStripMenuItem("Dynamic Adaptive Boost")
            {
                CheckOnClick = true,
                Checked = CurrentProfile != null && CurrentProfile.IsDynamicAdaptive
            };
            _trayDynamicAdaptiveMenuItem.CheckedChanged += TrayDynamicAdaptiveMenuItem_CheckedChanged;

            if (this.trayMenuStrip != null)
            {
                this.trayMenuStrip.Items.Insert(0, _trayDynamicAdaptiveMenuItem);
                this.trayMenuStrip.Items.Insert(1, new ToolStripSeparator()); // 구분선
            }

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = String.Format("Tarkov Settings {0}", version);
            _ = new UpdateNotifier(version);

            // Saturation Initialize
            if (gpu.Vendor != GPUVendor.NVIDIA)
                DVLGroupBox.Enabled = false;

            #region Initialize Display
            foreach (string display in Display.displays)
            {
                DisplayCombo.Items.Add(display);
            }

            if (DisplayCombo.FindString(appSetting.display) != -1)
                DisplayCombo.SelectedIndex = DisplayCombo.FindString(appSetting.display);

            Display.Primary = (string)DisplayCombo.SelectedItem;
            #endregion

            // Initialize Process Monitor
            pMonitor.Parent = this;
            pMonitor.Init();
        }

        /// <summary>
        /// 현재 UI에서 선택/편집 중인 Profile 객체
        /// </summary>
        public Profile CurrentProfile
        {
            get
            {
                if (appSetting?.Profiles == null || appSetting.Profiles.Count == 0)
                    return null;

                string selectedName = ProfileComboBox.SelectedItem as string ?? appSetting.SelectedProfile;
                var profile = appSetting.Profiles.FirstOrDefault(p => p.Name == selectedName);
                return profile ?? appSetting.Profiles[0];
            }
        }

        /// <summary>
        /// 활성 창 프로세스명과 일치하는 활성화된 프로필 검색 (ProcessMonitor에서 호출)
        /// </summary>
        public Profile GetMatchingProfile(string processName)
        {
            if (string.IsNullOrEmpty(processName) || appSetting?.Profiles == null)
                return null;

            foreach (var profile in appSetting.Profiles)
            {
                if (!profile.IsEnabled || profile.TargetProcesses == null)
                    continue;

                foreach (var target in profile.TargetProcesses)
                {
                    if (string.Equals(target.Trim(), processName.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return profile;
                    }
                }
            }
            return null;
        }

        #region Profile Management Logic (프로필 추가/삭제/이름수정/편집)
        private void PopulateProfileComboBox()
        {
            _isLoadingProfile = true;
            ProfileComboBox.Items.Clear();

            foreach (var profile in appSetting.Profiles)
            {
                ProfileComboBox.Items.Add(profile.Name);
            }

            if (ProfileComboBox.FindStringExact(appSetting.SelectedProfile) != -1)
                ProfileComboBox.SelectedIndex = ProfileComboBox.FindStringExact(appSetting.SelectedProfile);
            else if (ProfileComboBox.Items.Count > 0)
                ProfileComboBox.SelectedIndex = 0;

            _isLoadingProfile = false;
            LoadProfileToUI(CurrentProfile);
        }

        private void LoadProfileToUI(Profile profile)
        {
            if (profile == null) return;

            _isLoadingProfile = true;

            // 슬라이더 및 체크박스 수치 로드
            Brightness = profile.Brightness;
            Contrast = profile.Contrast;
            Gamma = profile.Gamma;
            DVL = profile.Saturation;
            BlackStabilizer = profile.BlackStabilizer;
            WhiteStabilizer = profile.WhiteStabilizer;
            IsDynamicAdaptive = profile.IsDynamicAdaptive;

            // 텍스트박스 및 프로필 활성화 여부 로드
            TargetProcessTextBox.Text = string.Join(", ", profile.TargetProcesses);
            ProfileEnabledCheckBox.Checked = profile.IsEnabled;

            // 라벨 텍스트 수치 갱신
            BrightnessText.Text = profile.Brightness.ToString("0.00");
            ContrastText.Text = profile.Contrast.ToString("0.00");
            GammaText.Text = profile.Gamma.ToString("0.00");
            DVLText.Text = profile.Saturation.ToString();
            BlackStabilizerText.Text = profile.BlackStabilizer.ToString();
            WhiteStabilizerText.Text = profile.WhiteStabilizer.ToString();

            // ColorController에 현재 프로필 값 전달
            var cController = ColorController.Instance;
            cController.Brightness = profile.Brightness;
            cController.Contrast = profile.Contrast;
            cController.Gamma = profile.Gamma;
            cController.BlackStabilizer = profile.BlackStabilizer;
            cController.WhiteStabilizer = profile.WhiteStabilizer;
            cController.IsDynamicAdaptive = profile.IsDynamicAdaptive;

            _isLoadingProfile = false;
        }

        private void ProfileComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isLoadingProfile) return;

            appSetting.SelectedProfile = (string)ProfileComboBox.SelectedItem;
            LoadProfileToUI(CurrentProfile);
            ScheduleSave();
        }

        private void AddProfileButton_Click(object sender, EventArgs e)
        {
            // [개선] 이미 존재하는 이름과 겹치지 않는 고유한 기본 이름 자동 탐색 (while 루프)
            int count = appSetting.Profiles.Count + 1;
            string defaultName = $"Custom Game {count}";

            while (appSetting.Profiles.Any(p => p.Name.Equals(defaultName, StringComparison.OrdinalIgnoreCase)))
            {
                count++;
                defaultName = $"Custom Game {count}";
            }

            // 중복 없는 안전한 기본 이름으로 팝업 띄우기
            string newName = ShowInputDialog("새 프로필의 이름을 입력하세요:", "새 프로필 생성", defaultName);

            if (string.IsNullOrWhiteSpace(newName)) return;

            // 사용자가 직접 타이핑한 이름이 중복되는지 최종 검사
            if (appSetting.Profiles.Any(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("이미 존재하는 프로필 이름입니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var newProfile = new Profile
            {
                Name = newName,
                IsEnabled = true,
                TargetProcesses = new List<string> { "GameProcessName" },
                Brightness = 0.50,
                Contrast = 0.50,
                Gamma = 1.00,
                Saturation = 0,
                BlackStabilizer = 0,
                WhiteStabilizer = 0,
                IsDynamicAdaptive = false
            };

            appSetting.Profiles.Add(newProfile);
            appSetting.SelectedProfile = newName;
            PopulateProfileComboBox();
            ScheduleSave();
        }

        private void RenameProfileButton_Click(object sender, EventArgs e)
        {
            if (CurrentProfile == null) return;

            string currentName = CurrentProfile.Name;
            string newName = ShowInputDialog("변경할 새 프로필 이름을 입력하세요:", "프로필 이름 변경", currentName);

            if (string.IsNullOrWhiteSpace(newName) || newName.Equals(currentName))
                return;

            if (appSetting.Profiles.Any(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("이미 존재하는 프로필 이름입니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CurrentProfile.Name = newName;
            appSetting.SelectedProfile = newName;
            PopulateProfileComboBox();
            ScheduleSave();
        }

        private void DeleteProfileButton_Click(object sender, EventArgs e)
        {
            if (appSetting.Profiles.Count <= 1)
            {
                MessageBox.Show("최소 1개의 프로필은 유지되어야 합니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var toDelete = CurrentProfile;
            if (toDelete != null)
            {
                DialogResult result = MessageBox.Show(
                    $"'{toDelete.Name}' 프로필을 정말 삭제하시겠습니까?\n(저장된 게임 설정값이 모두 삭제됩니다.)",
                    "프로필 삭제 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes) return;

                appSetting.Profiles.Remove(toDelete);
                appSetting.SelectedProfile = appSetting.Profiles[0].Name;
                PopulateProfileComboBox();
                ScheduleSave();
            }
        }

        private void TargetProcessTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isLoadingProfile || CurrentProfile == null) return;

            var processes = TargetProcessTextBox.Text
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            CurrentProfile.TargetProcesses = processes;
            ScheduleSave();
        }

        private void ProfileEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoadingProfile || CurrentProfile == null) return;

            CurrentProfile.IsEnabled = ProfileEnabledCheckBox.Checked;
            ScheduleSave();
        }

        private void SyncUIToCurrentProfile()
        {
            if (CurrentProfile == null) return;

            CurrentProfile.Brightness = Brightness;
            CurrentProfile.Contrast = Contrast;
            CurrentProfile.Gamma = Gamma;
            CurrentProfile.Saturation = DVL;
            CurrentProfile.BlackStabilizer = BlackStabilizer;
            CurrentProfile.WhiteStabilizer = WhiteStabilizer;
            CurrentProfile.IsDynamicAdaptive = IsDynamicAdaptive;
        }

        /// <summary>
        /// 프로필 이름 입력용 모달 팝업 대화상자
        /// </summary>
        private static string ShowInputDialog(string prompt, string title, string defaultValue = "")
        {
            using (Form promptForm = new Form())
            {
                promptForm.Width = 360;
                promptForm.Height = 170;
                promptForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                promptForm.Text = title;
                promptForm.StartPosition = FormStartPosition.CenterParent;
                promptForm.MaximizeBox = false;
                promptForm.MinimizeBox = false;

                Label textLabel = new Label() { Left = 20, Top = 18, Text = prompt, AutoSize = true };
                TextBox textBox = new TextBox() { Left = 20, Top = 45, Width = 300, Text = defaultValue };
                Button confirmation = new Button() { Text = "확인", Left = 140, Width = 85, Top = 85, DialogResult = DialogResult.OK };
                Button cancel = new Button() { Text = "취소", Left = 235, Width = 85, Top = 85, DialogResult = DialogResult.Cancel };

                promptForm.Controls.Add(textLabel);
                promptForm.Controls.Add(textBox);
                promptForm.Controls.Add(confirmation);
                promptForm.Controls.Add(cancel);
                promptForm.AcceptButton = confirmation;
                promptForm.CancelButton = cancel;

                return promptForm.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : null;
            }
        }
        #endregion

        private void TrayDynamicAdaptiveMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (DynamicAdaptiveCheckBox.Checked != _trayDynamicAdaptiveMenuItem.Checked)
            {
                DynamicAdaptiveCheckBox.Checked = _trayDynamicAdaptiveMenuItem.Checked;
            }
        }

        private void ScheduleSave()
        {
            _saveDebounceTimer?.Stop();
            _saveDebounceTimer?.Start();
        }

        private void SaveCurrentSettingsToDisk()
        {
            if (appSetting == null) return;

            appSetting.display = (string)DisplayCombo.SelectedItem;
            appSetting.minimizeOnStart = minimizeOnStart;
            appSetting.Save();
        }

        #region BCGS Getter/Setter
        public double Brightness
        {
            get => BrightnessBar.Value / 100.0;
            set => BrightnessBar.Value = (int)(value * 100);
        }

        public double Contrast
        {
            get => ContrastBar.Value / 100.0;
            set => ContrastBar.Value = (int)(value * 100);
        }

        public double Gamma
        {
            get => GammaBar.Value / 100.0;
            set => GammaBar.Value = (int)(value * 100);
        }

        public int DVL
        {
            get => DVLBar.Value;
            set => DVLBar.Value = value;
        }

        public double BlackStabilizer
        {
            get => BlackStabilizerBar.Value;
            set => BlackStabilizerBar.Value = (int)value;
        }

        public double WhiteStabilizer
        {
            get => WhiteStabilizerBar.Value;
            set => WhiteStabilizerBar.Value = (int)value;
        }

        public bool IsDynamicAdaptive
        {
            get => DynamicAdaptiveCheckBox.Checked;
            set => DynamicAdaptiveCheckBox.Checked = value;
        }

        public (double, double, double, int) GetColorValue()
        {
            return (
                BrightnessBar.Value / 100.0,
                ContrastBar.Value / 100.0,
                GammaBar.Value / 100.0,
                DVLBar.Value
                );
        }
        #endregion

        public bool IsEnabled { get => this.enableToolStripMenuItem.Checked; }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (minimizeOnStart)
            {
                this.Visible = false;
                this.ShowInTaskbar = false;
                this.trayIcon.ShowBalloonTip(
                    2500,
                    "Tarkov Settings Initialized!",
                    "Check out tray to modify your color setting",
                    ToolTipIcon.Info
                    );
            }
        }

        #region Control Event Handlers
        private void ColorLabel_DClick(object sender, EventArgs e)
        {
            var label = sender as Label;

            if (label.Equals(BrightnessLabel))
                BrightnessBar.Value = 50;
            else if (label.Equals(ContrastLabel))
                ContrastBar.Value = 50;
            else if (label.Equals(GammaLabel))
                GammaBar.Value = 100;
            else if (label.Equals(DVLLabel))
                DVLBar.Value = 0;
            else if (label.Equals(BlackStabilizerLabel))
                BlackStabilizerBar.Value = 0;
            else if (label.Equals(WhiteStabilizerLabel))
                WhiteStabilizerBar.Value = 0;

            SyncUIToCurrentProfile();
            ScheduleSave();
        }

        private void TrackBar_ValueChanged(object sender, EventArgs e)
        {
            var trackBar = sender as TrackBar;

            if (trackBar.Equals(BrightnessBar))
                BrightnessText.Text = (BrightnessBar.Value / 100.0).ToString("0.00");
            else if (trackBar.Equals(ContrastBar))
                ContrastText.Text = (ContrastBar.Value / 100.0).ToString("0.00");
            else if (trackBar.Equals(GammaBar))
                GammaText.Text = (GammaBar.Value / 100.0).ToString("0.00");
            else if (trackBar.Equals(DVLBar))
                DVLText.Text = DVLBar.Value.ToString();
            else if (trackBar.Equals(BlackStabilizerBar))
            {
                BlackStabilizerText.Text = BlackStabilizerBar.Value.ToString();
                ColorController.Instance.BlackStabilizer = BlackStabilizerBar.Value;
            }
            else if (trackBar.Equals(WhiteStabilizerBar))
            {
                WhiteStabilizerText.Text = WhiteStabilizerBar.Value.ToString();
                ColorController.Instance.WhiteStabilizer = WhiteStabilizerBar.Value;
            }

            if (!_isLoadingProfile)
            {
                SyncUIToCurrentProfile();
                ColorController.Instance.ApplyColorSettings();
                ScheduleSave();
            }
        }

        private void DynamicAdaptiveCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoadingProfile) return;

            ColorController.Instance.IsDynamicAdaptive = DynamicAdaptiveCheckBox.Checked;
            ColorController.Instance.ApplyColorSettings();

            if (_trayDynamicAdaptiveMenuItem != null && _trayDynamicAdaptiveMenuItem.Checked != DynamicAdaptiveCheckBox.Checked)
            {
                _trayDynamicAdaptiveMenuItem.Checked = DynamicAdaptiveCheckBox.Checked;
            }

            SyncUIToCurrentProfile();
            ScheduleSave();
        }

        private void DisplayCombo_SelectedValueChanged(object sender, EventArgs e)
        {
            string selectedDisplay = (string)DisplayCombo.SelectedItem;
            Display.Primary = selectedDisplay;

            if (Display.Primary != selectedDisplay)
            {
                DisplayCombo.SelectedIndex = DisplayCombo.FindString(Display.Primary);
            }

            ScheduleSave();
        }

        private void CheckOnMinimizeToTray(object sender, EventArgs e)
        {
            this.minimizeOnStart = this.minimizeStartCheckBox.Checked;
            ScheduleSave();
        }
        #endregion

        private void ShowForm(object sender, EventArgs e)
        {
            this.Visible = true;
            this.ShowInTaskbar = true;
        }

        private void ExitFormClicked(object sender, EventArgs e)
        {
            _saveDebounceTimer?.Stop();
            SaveCurrentSettingsToDisk();

            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _saveDebounceTimer?.Stop();
            SaveCurrentSettingsToDisk();

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                Console.WriteLine(e.CloseReason);
                this.trayIcon.Dispose();
                Console.WriteLine("[mainForm] Closing pMonitor");
                pMonitor.Close();
            }
        }

        private void label1_Click(object sender, EventArgs e) { }
        private void groupBox1_Enter(object sender, EventArgs e) { }
        private void groupBox1_Enter_1(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void label1_Click_1(object sender, EventArgs e) { }
    }
}