using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
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
        private bool _isLoadingProfile = false;

        // [접이식 창 너비 상수]
        private const int COLLAPSED_WIDTH = 665;
        private const int EXPANDED_WIDTH = 1393;
        private bool _isPreviewExpanded = false;

        // 디스크 과부하 방지 지연 저장 타이머 (1초)
        private System.Windows.Forms.Timer _saveDebounceTimer;
        private ToolStripMenuItem _trayDynamicAdaptiveMenuItem;

        // [미리보기 & 그래프 전용] 재사용 비트맵 버퍼
        private Dictionary<string, Bitmap> _sampleImages = new Dictionary<string, Bitmap>();
        private Bitmap _cachedRawSample = null;
        private Bitmap _cachedProcessedBitmap = null;
        private Bitmap _graphBitmap = null; // LUT 그래프 전용 비트맵
        private byte[] _rawBuffer = null;
        private byte[] _workBuffer = null;
        private int _bufferStride = 0;
        private int _bufferWidth = 0;
        private int _bufferHeight = 0;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED (깜빡임 완전 제거)
                return cp;
            }
        }

        public MainForm()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.UpdateStyles();

            _saveDebounceTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _saveDebounceTimer.Tick += (s, e) =>
            {
                _saveDebounceTimer.Stop();
                SaveCurrentSettingsToDisk();
            };

            #region Load App Settings
            appSetting = AppSetting.Load();
            minimizeOnStart = appSetting.minimizeOnStart;
            this.minimizeStartCheckBox.Checked = minimizeOnStart;

            PopulateProfileComboBox();
            #endregion

            // 이벤트 연결
            if (this.AddProfileButton != null) this.AddProfileButton.Click += AddProfileButton_Click;
            if (this.DeleteProfileButton != null) this.DeleteProfileButton.Click += DeleteProfileButton_Click;
            if (this.RenameProfileButton != null) this.RenameProfileButton.Click += RenameProfileButton_Click;
            if (this.ProfileComboBox != null) this.ProfileComboBox.SelectedIndexChanged += ProfileComboBox_SelectedIndexChanged;
            if (this.TargetProcessTextBox != null) this.TargetProcessTextBox.TextChanged += TargetProcessTextBox_TextChanged;
            if (this.ProfileEnabledCheckBox != null) this.ProfileEnabledCheckBox.CheckedChanged += ProfileEnabledCheckBox_CheckedChanged;
            if (this.SampleImageComboBox != null) this.SampleImageComboBox.SelectedIndexChanged += SampleImageComboBox_SelectedIndexChanged;
            if (this.CompareOriginalCheckBox != null) this.CompareOriginalCheckBox.CheckedChanged += CompareOriginalCheckBox_CheckedChanged;
            if (this.TogglePreviewButton != null) this.TogglePreviewButton.Click += TogglePreviewButton_Click;

            // 트레이 메뉴
            _trayDynamicAdaptiveMenuItem = new ToolStripMenuItem("Dynamic Adaptive Boost")
            {
                CheckOnClick = true,
                Checked = CurrentProfile != null && CurrentProfile.IsDynamicAdaptive
            };
            _trayDynamicAdaptiveMenuItem.CheckedChanged += TrayDynamicAdaptiveMenuItem_CheckedChanged;

            if (this.trayMenuStrip != null)
            {
                this.trayMenuStrip.Items.Insert(0, _trayDynamicAdaptiveMenuItem);
                this.trayMenuStrip.Items.Insert(1, new ToolStripSeparator());
            }

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = String.Format("Tarkov Settings {0}", version);
            _ = new UpdateNotifier(version);

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

            InitSampleImages();

            // 시작 시 접힌 상태로 시작
            SetPreviewExpanded(false);

            pMonitor.Parent = this;
            pMonitor.Init();
        }

        #region Expand / Collapse Preview Logic
        private void SetPreviewExpanded(bool expand)
        {
            _isPreviewExpanded = expand;

            this.SuspendLayout();
            if (this.layoutTablePanel != null) this.layoutTablePanel.SuspendLayout();

            if (this.PreviewGroupBox != null)
            {
                this.PreviewGroupBox.Visible = _isPreviewExpanded;
            }

            this.ClientSize = new Size(_isPreviewExpanded ? EXPANDED_WIDTH : COLLAPSED_WIDTH, this.ClientSize.Height);

            if (this.TogglePreviewButton != null)
            {
                this.TogglePreviewButton.Text = _isPreviewExpanded ? "◀ 접기" : "▶ 미리보기";
            }

            if (this.layoutTablePanel != null) this.layoutTablePanel.ResumeLayout(false);
            this.ResumeLayout(true);

            if (_isPreviewExpanded)
            {
                RenderAndRefreshPreview();
            }
        }

        private void TogglePreviewButton_Click(object sender, EventArgs e)
        {
            SetPreviewExpanded(!_isPreviewExpanded);
        }
        #endregion

        #region Built-in Sample Images & Real-time Image/Graph Renderer
        private void InitSampleImages()
        {
            _sampleImages.Clear();

            try
            {
                ResourceSet resourceSet = Properties.Resources.ResourceManager.GetResourceSet(
                    CultureInfo.InvariantCulture, true, true);

                if (resourceSet != null)
                {
                    foreach (DictionaryEntry entry in resourceSet)
                    {
                        if (entry.Value is Bitmap bmp)
                        {
                            string rawName = entry.Key.ToString();
                            if (rawName.Equals("trayIcon", StringComparison.OrdinalIgnoreCase) ||
                                rawName.Equals("ScreenSample", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            string displayName = rawName.Replace('_', ' ');
                            _sampleImages[displayName] = bmp;
                        }
                    }
                }
            }
            catch { }

            if (_sampleImages.Count == 0)
            {
                _sampleImages["1. 주간 수풀 맵 (Daylight)"] = CreateProceduralDaySample();
                _sampleImages["2. 야간 NVG + 플래시 (Night NVG)"] = CreateProceduralNvgSample();
                _sampleImages["3. 인터체인지 실내 (Dark Interior)"] = CreateProceduralDarkSample();
            }

            if (this.SampleImageComboBox != null)
            {
                this.SampleImageComboBox.Items.Clear();
                foreach (var key in _sampleImages.Keys)
                {
                    this.SampleImageComboBox.Items.Add(key);
                }

                if (this.SampleImageComboBox.Items.Count > 0)
                    this.SampleImageComboBox.SelectedIndex = 0;
            }
        }

        private void SampleImageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedName = SampleImageComboBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedName) && _sampleImages.ContainsKey(selectedName))
            {
                SetCurrentSampleImage(_sampleImages[selectedName]);
            }
        }

        private void SetCurrentSampleImage(Bitmap src)
        {
            if (src == null) return;

            int w = 640;
            int h = 360;

            _cachedRawSample?.Dispose();
            _cachedProcessedBitmap?.Dispose();

            _cachedRawSample = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(_cachedRawSample))
            {
                g.InterpolationMode = InterpolationMode.Bilinear;
                g.DrawImage(src, 0, 0, w, h);
            }

            _cachedProcessedBitmap = new Bitmap(w, h, PixelFormat.Format24bppRgb);

            BitmapData data = _cachedRawSample.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            _bufferStride = Math.Abs(data.Stride);
            _bufferWidth = w;
            _bufferHeight = h;
            int bytes = _bufferStride * h;

            _rawBuffer = new byte[bytes];
            _workBuffer = new byte[bytes];

            Marshal.Copy(data.Scan0, _rawBuffer, 0, bytes);
            _cachedRawSample.UnlockBits(data);

            if (_isPreviewExpanded)
            {
                RenderAndRefreshPreview();
            }
        }

        private void CompareOriginalCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDisplayedImage();
        }

        /// <summary>
        /// 슬라이더 조작 시 0.2ms 만에 사진과 256 LUT 곡선 그래프를 동시에 갱신
        /// </summary>
        private void RenderAndRefreshPreview()
        {
            if (!_isPreviewExpanded || _cachedRawSample == null || _rawBuffer == null) return;

            // 1. 256 LUT 연산
            double b = Brightness;
            double c = Contrast;
            double g = Gamma;
            double black = BlackStabilizer;
            double white = WhiteStabilizer;
            float dynamicBoost = ColorController.Instance.CurrentDynamicBoost;

            ushort[] lut16 = ColorController.CalculateLUT(b, c, g, black, white, dynamicBoost);
            byte[] lut8 = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                lut8[i] = (byte)(lut16[i] >> 8);
            }

            // 2. [그래프 렌더링] 라벨 없이 순수 256 LUT 커브 선 그래프만 그리기
            DrawLutGraph(lut8);

            // '원본' 체크 시 보정 사진 연산 건너뛰기
            if (this.CompareOriginalCheckBox != null && this.CompareOriginalCheckBox.Checked)
            {
                UpdateDisplayedImage();
                return;
            }

            // 3. 제로 가비지 버퍼 기반 고속 픽셀 변환
            int len = _rawBuffer.Length;
            double satScale = 1.0 + (DVL / 50.0);
            bool hasDVL = (DVL != 0);

            for (int i = 0; i < len; i += 3)
            {
                byte blue = lut8[_rawBuffer[i]];
                byte green = lut8[_rawBuffer[i + 1]];
                byte red = lut8[_rawBuffer[i + 2]];

                if (hasDVL)
                {
                    double gray = (0.299 * red) + (0.587 * green) + (0.114 * blue);
                    red = (byte)Math.Min(Math.Max(gray + (red - gray) * satScale, 0), 255);
                    green = (byte)Math.Min(Math.Max(gray + (green - gray) * satScale, 0), 255);
                    blue = (byte)Math.Min(Math.Max(gray + (blue - gray) * satScale, 0), 255);
                }

                _workBuffer[i] = blue;
                _workBuffer[i + 1] = green;
                _workBuffer[i + 2] = red;
            }

            BitmapData dstData = _cachedProcessedBitmap.LockBits(
                new Rectangle(0, 0, _bufferWidth, _bufferHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb
            );

            Marshal.Copy(_workBuffer, 0, dstData.Scan0, len);
            _cachedProcessedBitmap.UnlockBits(dstData);

            UpdateDisplayedImage();
        }

        /// <summary>
        /// 256 LUT 밝기 커브 그래프 그리기 (라벨 없이 깔끔한 순수 곡선 차트)
        /// </summary>
        private void DrawLutGraph(byte[] lut8)
        {
            if (this.LutGraphPictureBox == null || !_isPreviewExpanded) return;

            int w = this.LutGraphPictureBox.Width;
            int h = this.LutGraphPictureBox.Height;
            if (w <= 10 || h <= 10) return;

            if (_graphBitmap == null || _graphBitmap.Width != w || _graphBitmap.Height != h)
            {
                _graphBitmap?.Dispose();
                _graphBitmap = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            }

            using (Graphics g = Graphics.FromImage(_graphBitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 1. 어두운 배경
                g.Clear(Color.FromArgb(18, 19, 24));

                int pad = 8;
                int plotW = w - 2 * pad;
                int plotH = h - 2 * pad;

                // 2. 대각선 1:1 중립 기준선 (점선)
                using (Pen dashedPen = new Pen(Color.FromArgb(60, 65, 75), 1))
                {
                    dashedPen.DashStyle = DashStyle.Dot;
                    g.DrawLine(dashedPen, pad, h - pad, w - pad, pad);
                }

                // 3. 25%, 50%, 75% 그리드 격자선
                using (Pen gridPen = new Pen(Color.FromArgb(32, 35, 42), 1))
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        float pos = pad + (plotW * (i / 4f));
                        g.DrawLine(gridPen, pos, pad, pos, h - pad); // 수직선
                        float posY = pad + (plotH * (i / 4f));
                        g.DrawLine(gridPen, pad, posY, w - pad, posY); // 수평선
                    }
                }

                // 4. 외곽 테두리
                using (Pen borderPen = new Pen(Color.FromArgb(48, 52, 62), 1))
                {
                    g.DrawRectangle(borderPen, pad, pad, plotW, plotH);
                }

                // 5. 256개 지점 LUT 변환 곡선 그리기 (에메랄드 그린)
                PointF[] points = new PointF[256];
                for (int i = 0; i < 256; i++)
                {
                    float x = pad + ((float)i / 255f) * plotW;
                    float yVal = lut8[i];
                    float y = (h - pad) - (yVal / 255f) * plotH;
                    points[i] = new PointF(x, y);
                }

                using (Pen curvePen = new Pen(Color.FromArgb(0, 255, 136), 2f))
                {
                    g.DrawLines(curvePen, points);
                }
            }

            this.LutGraphPictureBox.Image = _graphBitmap;
        }

        private void UpdateDisplayedImage()
        {
            if (this.PreviewPictureBox == null) return;

            if (this.CompareOriginalCheckBox != null && this.CompareOriginalCheckBox.Checked)
            {
                this.PreviewPictureBox.Image = _cachedRawSample;
            }
            else
            {
                this.PreviewPictureBox.Image = _cachedProcessedBitmap ?? _cachedRawSample;
            }
        }

        #region 기본 테스트용 샘플 사진 생성기
        private Bitmap CreateProceduralDaySample()
        {
            Bitmap bmp = new Bitmap(640, 360, PixelFormat.Format24bppRgb);
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                gfx.Clear(Color.FromArgb(10, 15, 10));
                using (Brush b = new SolidBrush(Color.FromArgb(25, 35, 20)))
                    gfx.FillRectangle(b, 50, 150, 180, 150);
                using (Brush b = new SolidBrush(Color.FromArgb(45, 55, 35)))
                    gfx.DrawString("수풀 속 적 (PMC)", new Font("Consolas", 12, FontStyle.Bold), b, 60, 220);
                using (Brush b = new SolidBrush(Color.FromArgb(180, 190, 150)))
                    gfx.FillEllipse(b, 350, 80, 200, 200);
            }
            return bmp;
        }

        private Bitmap CreateProceduralNvgSample()
        {
            Bitmap bmp = new Bitmap(640, 360, PixelFormat.Format24bppRgb);
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                gfx.Clear(Color.FromArgb(5, 12, 8));
                using (Brush b = new SolidBrush(Color.FromArgb(15, 30, 20)))
                    gfx.FillRectangle(b, 40, 180, 150, 140);
                using (Brush b = new SolidBrush(Color.FromArgb(30, 50, 35)))
                    gfx.DrawString("어두운 모퉁이", new Font("Consolas", 11, FontStyle.Bold), b, 50, 240);

                using (Brush b = new SolidBrush(Color.FromArgb(230, 255, 240)))
                    gfx.FillEllipse(b, 460, 120, 150, 150);
            }
            return bmp;
        }

        private Bitmap CreateProceduralDarkSample()
        {
            Bitmap bmp = new Bitmap(640, 360, PixelFormat.Format24bppRgb);
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                gfx.Clear(Color.FromArgb(4, 4, 6));
                using (Brush b = new SolidBrush(Color.FromArgb(18, 18, 24)))
                    gfx.FillRectangle(b, 40, 160, 200, 160);
                using (Brush b = new SolidBrush(Color.FromArgb(28, 28, 38)))
                    gfx.DrawString("숨은 적 (RGB 25)", new Font("Consolas", 11, FontStyle.Bold), b, 55, 230);
            }
            return bmp;
        }
        #endregion
        #endregion

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

        #region Profile Management Logic
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

            Brightness = profile.Brightness;
            Contrast = profile.Contrast;
            Gamma = profile.Gamma;
            DVL = profile.Saturation;
            BlackStabilizer = profile.BlackStabilizer;
            WhiteStabilizer = profile.WhiteStabilizer;
            IsDynamicAdaptive = profile.IsDynamicAdaptive;

            TargetProcessTextBox.Text = string.Join(", ", profile.TargetProcesses);
            ProfileEnabledCheckBox.Checked = profile.IsEnabled;

            BrightnessText.Text = profile.Brightness.ToString("0.00");
            ContrastText.Text = profile.Contrast.ToString("0.00");
            GammaText.Text = profile.Gamma.ToString("0.00");
            DVLText.Text = profile.Saturation.ToString();
            BlackStabilizerText.Text = profile.BlackStabilizer.ToString();
            WhiteStabilizerText.Text = profile.WhiteStabilizer.ToString();

            var cController = ColorController.Instance;
            cController.Brightness = profile.Brightness;
            cController.Contrast = profile.Contrast;
            cController.Gamma = profile.Gamma;
            cController.BlackStabilizer = profile.BlackStabilizer;
            cController.WhiteStabilizer = profile.WhiteStabilizer;
            cController.IsDynamicAdaptive = profile.IsDynamicAdaptive;

            _isLoadingProfile = false;

            if (_isPreviewExpanded)
            {
                RenderAndRefreshPreview();
            }
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
            int count = appSetting.Profiles.Count + 1;
            string defaultName = $"Custom Game {count}";

            while (appSetting.Profiles.Any(p => p.Name.Equals(defaultName, StringComparison.OrdinalIgnoreCase)))
            {
                count++;
                defaultName = $"Custom Game {count}";
            }

            string newName = ShowInputDialog("새 프로필의 이름을 입력하세요:", "새 프로필 생성", defaultName);

            if (string.IsNullOrWhiteSpace(newName)) return;

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
            if (_isPreviewExpanded) RenderAndRefreshPreview();
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
                if (_isPreviewExpanded) RenderAndRefreshPreview();
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
            if (_isPreviewExpanded) RenderAndRefreshPreview();
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
        private void layoutTablePanel_Paint(object sender, PaintEventArgs e) { }
    }
}