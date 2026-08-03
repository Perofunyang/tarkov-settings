using System;
using System.Net.Http;
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

        // [핵심] 디스크 과부하 방지를 위한 디바운싱(지연 저장) 타이머
        private System.Windows.Forms.Timer _saveDebounceTimer;

        // [추가] 트레이 메뉴에 추가될 동적 적응형 메뉴 항목
        private ToolStripMenuItem _trayDynamicAdaptiveMenuItem;

        public MainForm()
        {
            InitializeComponent();

            // 지연 저장 타이머 초기화 (1초)
            _saveDebounceTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _saveDebounceTimer.Tick += (s, e) =>
            {
                _saveDebounceTimer.Stop();
                SaveCurrentSettingsToDisk(); // 마우스를 떼고 1초 뒤 디스크에 1번만 저장!
            };

            #region Load App Settings
            // Load Settings
            appSetting = AppSetting.Load();

            Brightness = appSetting.brightness;
            Contrast = appSetting.contrast;
            Gamma = appSetting.gamma;
            DVL = appSetting.saturation;
            BlackStabilizer = appSetting.blackStabilizer;
            WhiteStabilizer = appSetting.whiteStabilizer;
            IsDynamicAdaptive = appSetting.isDynamicAdaptive;

            minimizeOnStart = appSetting.minimizeOnStart;
            this.minimizeStartCheckBox.Checked = minimizeOnStart;

            // ColorController에 초기 설정값 전달
            var cController = ColorController.Instance;
            cController.Brightness = Brightness;
            cController.Contrast = Contrast;
            cController.Gamma = Gamma;
            cController.BlackStabilizer = BlackStabilizer;
            cController.WhiteStabilizer = WhiteStabilizer;
            cController.IsDynamicAdaptive = IsDynamicAdaptive;
            #endregion

            // [추가] 트레이 우클릭 메뉴 상단에 Dynamic Adaptive Boost 토글 메뉴 동적 생성 및 연결
            _trayDynamicAdaptiveMenuItem = new ToolStripMenuItem("Dynamic Adaptive Boost")
            {
                CheckOnClick = true,
                Checked = IsDynamicAdaptive
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
            // Initialize Display Dropdown
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
            foreach (string pTarget in appSetting.pTargets)
            {
                pMonitor.Add(pTarget.ToLower());
            }
            pMonitor.Init();
        }

        /// <summary>
        /// 트레이 메뉴에서 동적 적응형 항목 클릭 시 UI 체크박스 동기화
        /// </summary>
        private void TrayDynamicAdaptiveMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (DynamicAdaptiveCheckBox.Checked != _trayDynamicAdaptiveMenuItem.Checked)
            {
                DynamicAdaptiveCheckBox.Checked = _trayDynamicAdaptiveMenuItem.Checked;
            }
        }

        /// <summary>
        /// 컨트롤 조작 시 타이머를 리셋하여 마우스 조작이 끝난 후 지연 저장하도록 예약
        /// </summary>
        private void ScheduleSave()
        {
            _saveDebounceTimer?.Stop();
            _saveDebounceTimer?.Start();
        }

        /// <summary>
        /// 실제로 settings.json 파일에 설정값을 저장하는 메서드
        /// </summary>
        private void SaveCurrentSettingsToDisk()
        {
            if (appSetting == null) return;

            appSetting.brightness = Brightness;
            appSetting.contrast = Contrast;
            appSetting.gamma = Gamma;
            appSetting.saturation = DVL;
            appSetting.blackStabilizer = BlackStabilizer;
            appSetting.whiteStabilizer = WhiteStabilizer;
            appSetting.isDynamicAdaptive = IsDynamicAdaptive;
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
            {
                BrightnessBar.Value = 50;
            }
            else if (label.Equals(ContrastLabel))
            {
                ContrastBar.Value = 50;
            }
            else if (label.Equals(GammaLabel))
            {
                GammaBar.Value = 100;
            }
            else if (label.Equals(DVLLabel))
            {
                DVLBar.Value = 0;
            }
            else if (label.Equals(BlackStabilizerLabel))
            {
                BlackStabilizerBar.Value = 0;
            }
            else if (label.Equals(WhiteStabilizerLabel))
            {
                WhiteStabilizerBar.Value = 0;
            }

            ScheduleSave(); // 라벨 더블클릭 리셋 시 저장
        }

        private void TrackBar_ValueChanged(object sender, EventArgs e)
        {
            var trackBar = sender as TrackBar;

            if (trackBar.Equals(BrightnessBar))
            {
                BrightnessText.Text = (BrightnessBar.Value / 100.0).ToString("0.00");
            }
            else if (trackBar.Equals(ContrastBar))
            {
                ContrastText.Text = (ContrastBar.Value / 100.0).ToString("0.00");
            }
            else if (trackBar.Equals(GammaBar))
            {
                GammaText.Text = (GammaBar.Value / 100.0).ToString("0.00");
            }
            else if (trackBar.Equals(DVLBar))
            {
                DVLText.Text = DVLBar.Value.ToString();
            }
            else if (trackBar.Equals(BlackStabilizerBar))
            {
                BlackStabilizerText.Text = BlackStabilizerBar.Value.ToString();
                ColorController.Instance.BlackStabilizer = BlackStabilizerBar.Value;
                ColorController.Instance.ApplyColorSettings();
            }
            else if (trackBar.Equals(WhiteStabilizerBar))
            {
                WhiteStabilizerText.Text = WhiteStabilizerBar.Value.ToString();
                ColorController.Instance.WhiteStabilizer = WhiteStabilizerBar.Value;
                ColorController.Instance.ApplyColorSettings();
            }

            ScheduleSave(); // 드래그 완료 후 1초 뒤 디스크 저장
        }

        private void DynamicAdaptiveCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ColorController.Instance.IsDynamicAdaptive = DynamicAdaptiveCheckBox.Checked;
            ColorController.Instance.ApplyColorSettings();

            // 트레이 우클릭 메뉴의 체크 상태 양방향 동기화
            if (_trayDynamicAdaptiveMenuItem != null && _trayDynamicAdaptiveMenuItem.Checked != DynamicAdaptiveCheckBox.Checked)
            {
                _trayDynamicAdaptiveMenuItem.Checked = DynamicAdaptiveCheckBox.Checked;
            }

            ScheduleSave(); // 체크박스 변경 시 저장
        }

        private void DisplayCombo_SelectedValueChanged(object sender, EventArgs e)
        {
            string selectedDisplay = (string)DisplayCombo.SelectedItem;
            Display.Primary = selectedDisplay;

            if (Display.Primary != selectedDisplay)
            {
                DisplayCombo.SelectedIndex = DisplayCombo.FindString(Display.Primary);
            }

            ScheduleSave(); // 디스플레이 변경 시 저장
        }

        private void CheckOnMinimizeToTray(object sender, EventArgs e)
        {
            this.minimizeOnStart = this.minimizeStartCheckBox.Checked;
            ScheduleSave(); // '시작 시 최소화' 변경 시 저장
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
            SaveCurrentSettingsToDisk(); // 종료 시 즉시 저장

            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _saveDebounceTimer?.Stop();
            SaveCurrentSettingsToDisk(); // 종료/재부팅 시 즉시 저장

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

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }
}