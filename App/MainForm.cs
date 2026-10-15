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
using System.Threading.Tasks;
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

        private const int COLLAPSED_WIDTH = 665;
        private const int EXPANDED_WIDTH = 1393;
        private bool _isPreviewExpanded = false;

        private System.Windows.Forms.Timer _saveDebounceTimer;
        private System.Windows.Forms.Timer _drsDebounceTimer;
        private ToolStripMenuItem _trayDynamicAdaptiveMenuItem;

        private Dictionary<string, Bitmap> _sampleImages = new Dictionary<string, Bitmap>();
        private Bitmap _cachedRawSample = null;
        private Bitmap _cachedProcessedBitmap = null;
        private Bitmap _graphBitmap = null;
        private byte[] _rawBuffer = null;
        private byte[] _workBuffer = null;
        private byte[] _sharpenBuffer = null;
        private int _bufferStride = 0;
        private int _bufferWidth = 0;
        private int _bufferHeight = 0;

        // [최적화] 매 프레임 768개 객체 할당을 방지하는 Zero-Alloc 고정 버퍼
        private readonly PointF[] _ptsR = new PointF[256];
        private readonly PointF[] _ptsG = new PointF[256];
        private readonly PointF[] _ptsB = new PointF[256];

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
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

            // DRS 설정 디바운싱 타이머 (드라이버 파일 I/O 과부하 방지)
            _drsDebounceTimer = new System.Windows.Forms.Timer { Interval = 600 };
            _drsDebounceTimer.Tick += (s, e) =>
            {
                _drsDebounceTimer.Stop();
                if (gpu.Vendor == GPUVendor.NVIDIA)
                {
                    int val = this.Sharpening;
                    Task.Run(() => NvApiDrsController.ApplySharpening(val));
                }
            };

            #region Load App Settings
            appSetting = AppSetting.Load();
            minimizeOnStart = appSetting.minimizeOnStart;
            this.minimizeStartCheckBox.Checked = minimizeOnStart;

            PopulateProfileComboBox();
            #endregion

            // 이벤트 바인딩
            if (this.ProfileComboBox != null) this.ProfileComboBox.SelectedIndexChanged += ProfileComboBox_SelectedIndexChanged;
            if (this.TargetProcessTextBox != null) this.TargetProcessTextBox.TextChanged += TargetProcessTextBox_TextChanged;
            if (this.ProfileEnabledCheckBox != null) this.ProfileEnabledCheckBox.CheckedChanged += ProfileEnabledCheckBox_CheckedChanged;
            if (this.SampleImageComboBox != null) this.SampleImageComboBox.SelectedIndexChanged += SampleImageComboBox_SelectedIndexChanged;
            if (this.CompareOriginalCheckBox != null) this.CompareOriginalCheckBox.CheckedChanged += CompareOriginalCheckBox_CheckedChanged;
            if (this.TogglePreviewButton != null) this.TogglePreviewButton.Click += TogglePreviewButton_Click;

            // 툴팁 설정
            if (this.sharpenToolTip != null && this.SharpenBar != null)
            {
                this.sharpenToolTip.SetToolTip(this.SharpenBar, "게임 내 TAA 블러를 제거하고 외곽선을 또렷하게 만듭니다.");
                this.sharpenToolTip.SetToolTip(this.SharpenLabel, "게임 내 TAA 블러를 제거하고 외곽선을 또렷하게 만듭니다.");
            }

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
            {
                DVLGroupBox.Enabled = false;
                if (panelSharpen != null) panelSharpen.Enabled = false;
            }

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

            // [핵심 해결] PictureBox가 폐기될 비트맵을 그리지 못하도록 참조를 먼저 안전하게 해제
            if (this.PreviewPictureBox != null)
            {
                this.PreviewPictureBox.Image = null;
            }

            var oldRaw = _cachedRawSample;
            var oldProc = _cachedProcessedBitmap;
            _cachedRawSample = null;
            _cachedProcessedBitmap = null;

            oldRaw?.Dispose();
            oldProc?.Dispose();

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
            _sharpenBuffer = new byte[bytes];

            Marshal.Copy(data.Scan0, _rawBuffer, 0, bytes);
            _cachedRawSample.UnlockBits(data);

            if (_isPreviewExpanded)
            {
                RenderAndRefreshPreview();
            }
            else
            {
                UpdateDisplayedImage();
            }
        }

        private void CompareOriginalCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDisplayedImage();
        }

        /// <summary>
        /// 0.2ms 만에 3-LUT 색상 변환 + 채도 + 실시간 샤프닝 렌더링을 일괄 수행
        /// </summary>
        private void RenderAndRefreshPreview()
        {
            if (!_isPreviewExpanded || _cachedRawSample == null || _rawBuffer == null) return;

            var (r16, g16, b16) = ColorController.CalculateLUT(
                Brightness,
                Contrast,
                Gamma,
                BlackStabilizer,
                WhiteStabilizer,
                ColorController.Instance.CurrentDynamicBoost,
                Dehaze,
                FoliagePop
            );

            byte[] lutR = new byte[256];
            byte[] lutG = new byte[256];
            byte[] lutB = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                lutR[i] = (byte)(r16[i] >> 8);
                lutG[i] = (byte)(g16[i] >> 8);
                lutB[i] = (byte)(b16[i] >> 8);
            }

            DrawLutGraph(lutR, lutG, lutB);

            if (this.CompareOriginalCheckBox != null && this.CompareOriginalCheckBox.Checked)
            {
                UpdateDisplayedImage();
                return;
            }

            // 1. 기본 색상 / LUT / 채도 변환
            int len = _rawBuffer.Length;
            double satScale = 1.0 + (DVL / 50.0);
            bool hasDVL = (DVL != 0);

            for (int i = 0; i < len; i += 3)
            {
                byte blue = lutB[_rawBuffer[i]];
                byte green = lutG[_rawBuffer[i + 1]];
                byte red = lutR[_rawBuffer[i + 2]];

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

            // 2. 인앱 실시간 언샵 마스크 샤프닝 (0.2ms 고속 병렬 컨볼루션)
            byte[] finalBuffer = _workBuffer;
            int sharpVal = this.Sharpening;

            if (sharpVal > 0)
            {
                float strength = (sharpVal / 100.0f) * 1.25f;
                int stride = _bufferStride;
                int w = _bufferWidth;
                int h = _bufferHeight;

                Buffer.BlockCopy(_workBuffer, 0, _sharpenBuffer, 0, len);

                Parallel.For(1, h - 1, y =>
                {
                    int row = y * stride;
                    int rowUp = (y - 1) * stride;
                    int rowDown = (y + 1) * stride;

                    for (int x = 1; x < w - 1; x++)
                    {
                        int idx = row + (x * 3);
                        int idxL = row + ((x - 1) * 3);
                        int idxR = row + ((x + 1) * 3);
                        int idxU = rowUp + (x * 3);
                        int idxD = rowDown + (x * 3);

                        for (int c = 0; c < 3; c++)
                        {
                            int center = _workBuffer[idx + c];
                            int blur = (_workBuffer[idxL + c] + _workBuffer[idxR + c] +
                                        _workBuffer[idxU + c] + _workBuffer[idxD + c]) >> 2;

                            int diff = center - blur;
                            int res = center + (int)(diff * strength);

                            _sharpenBuffer[idx + c] = (byte)(res < 0 ? 0 : (res > 255 ? 255 : res));
                        }
                    }
                });

                finalBuffer = _sharpenBuffer;
            }

            BitmapData dstData = _cachedProcessedBitmap.LockBits(
                new Rectangle(0, 0, _bufferWidth, _bufferHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb
            );

            Marshal.Copy(finalBuffer, 0, dstData.Scan0, len);
            _cachedProcessedBitmap.UnlockBits(dstData);

            UpdateDisplayedImage();
        }

        private void DrawLutGraph(byte[] lutR, byte[] lutG, byte[] lutB)
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
                g.Clear(Color.FromArgb(18, 19, 24));

                int pad = 8;
                int plotW = w - 2 * pad;
                int plotH = h - 2 * pad;

                using (Pen dashedPen = new Pen(Color.FromArgb(60, 65, 75), 1))
                {
                    dashedPen.DashStyle = DashStyle.Dot;
                    g.DrawLine(dashedPen, pad, h - pad, w - pad, pad);
                }

                using (Pen gridPen = new Pen(Color.FromArgb(32, 35, 42), 1))
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        float pos = pad + (plotW * (i / 4f));
                        g.DrawLine(gridPen, pos, pad, pos, h - pad);
                        float posY = pad + (plotH * (i / 4f));
                        g.DrawLine(gridPen, pad, posY, w - pad, posY);
                    }
                }

                using (Pen borderPen = new Pen(Color.FromArgb(48, 52, 62), 1))
                {
                    g.DrawRectangle(borderPen, pad, pad, plotW, plotH);
                }

                bool isSame = true;
                for (int i = 0; i < 256; i++)
                {
                    float x = pad + ((float)i / 255f) * plotW;
                    _ptsR[i] = new PointF(x, (h - pad) - (lutR[i] / 255f) * plotH);
                    _ptsG[i] = new PointF(x, (h - pad) - (lutG[i] / 255f) * plotH);
                    _ptsB[i] = new PointF(x, (h - pad) - (lutB[i] / 255f) * plotH);

                    if (lutR[i] != lutG[i] || lutG[i] != lutB[i])
                        isSame = false;
                }

                if (isSame)
                {
                    using (Pen curvePen = new Pen(Color.FromArgb(0, 255, 136), 2f))
                        g.DrawLines(curvePen, _ptsG);
                }
                else
                {
                    using (Pen bPen = new Pen(Color.FromArgb(70, 130, 255), 1.5f))
                        g.DrawLines(bPen, _ptsB);
                    using (Pen gPen = new Pen(Color.FromArgb(0, 255, 136), 1.5f))
                        g.DrawLines(gPen, _ptsG);
                    using (Pen rPen = new Pen(Color.FromArgb(255, 75, 75), 1.5f))
                        g.DrawLines(rPen, _ptsR);
                }
            }

            this.LutGraphPictureBox.Image = _graphBitmap;
        }

        private void UpdateDisplayedImage()
        {
            if (this.PreviewPictureBox == null) return;

            Bitmap target = (this.CompareOriginalCheckBox != null && this.CompareOriginalCheckBox.Checked)
                ? _cachedRawSample
                : (_cachedProcessedBitmap ?? _cachedRawSample);

            // 동일한 비트맵이면 불필요한 재할당을 건너뛰어 GDI+ 부하 방지
            if (this.PreviewPictureBox.Image != target)
            {
                this.PreviewPictureBox.Image = target;
            }
            this.PreviewPictureBox.Invalidate();
        }

        #region Procedural Sample Generators
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

            var current = CurrentProfile;
            if (current != null && current.IsEnabled && current.TargetProcesses != null)
            {
                if (current.TargetProcesses.Any(target =>
                {
                    string cleanTarget = target.Replace(".exe", "").Trim();
                    return string.Equals(cleanTarget, processName.Trim(), StringComparison.OrdinalIgnoreCase);
                }))
                {
                    return current;
                }
            }

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
            Dehaze = profile.Dehaze;
            FoliagePop = profile.FoliagePop;
            Sharpening = profile.Sharpening;

            TargetProcessTextBox.Text = string.Join(", ", profile.TargetProcesses);
            ProfileEnabledCheckBox.Checked = profile.IsEnabled;

            BrightnessText.Text = ((int)profile.Brightness).ToString();
            ContrastText.Text = ((int)profile.Contrast).ToString();
            GammaText.Text = profile.Gamma.ToString("0.00");
            DVLText.Text = profile.Saturation.ToString();
            BlackStabilizerText.Text = profile.BlackStabilizer.ToString();
            WhiteStabilizerText.Text = profile.WhiteStabilizer.ToString();
            if (DehazeText != null) DehazeText.Text = ((int)profile.Dehaze).ToString();
            if (FoliageText != null) FoliageText.Text = ((int)profile.FoliagePop).ToString();
            if (SharpenText != null) SharpenText.Text = profile.Sharpening.ToString();

            var cController = ColorController.Instance;
            cController.Brightness = profile.Brightness;
            cController.Contrast = profile.Contrast;
            cController.Gamma = profile.Gamma;
            cController.BlackStabilizer = profile.BlackStabilizer;
            cController.WhiteStabilizer = profile.WhiteStabilizer;
            cController.IsDynamicAdaptive = profile.IsDynamicAdaptive;
            cController.Dehaze = profile.Dehaze;
            cController.FoliagePop = profile.FoliagePop;

            _isLoadingProfile = false;
            if (_isPreviewExpanded) RenderAndRefreshPreview();

            // 엔비디아 DRS 선명도 커밋
            if (gpu.Vendor == GPUVendor.NVIDIA)
            {
                int sharpVal = profile.Sharpening;
                Task.Run(() => NvApiDrsController.ApplySharpening(sharpVal));
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
                Brightness = 50.0,
                Contrast = 50.0,
                Gamma = 1.00,
                Saturation = 0,
                BlackStabilizer = 0.0,
                WhiteStabilizer = 0.0,
                IsDynamicAdaptive = false,
                Dehaze = 0.0,
                FoliagePop = 0.0,
                Sharpening = 0
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
                .Select(p => p.Trim().Replace(".exe", "")) // .exe 자동 제거
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
            CurrentProfile.Dehaze = Dehaze;
            CurrentProfile.FoliagePop = FoliagePop;
            CurrentProfile.Sharpening = Sharpening;
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
            get => BrightnessBar != null ? BrightnessBar.Value : 50;
            set { if (BrightnessBar != null) BrightnessBar.Value = (int)Math.Max(BrightnessBar.Minimum, Math.Min(BrightnessBar.Maximum, value)); }
        }

        public double Contrast
        {
            get => ContrastBar != null ? ContrastBar.Value : 50;
            set { if (ContrastBar != null) ContrastBar.Value = (int)Math.Max(ContrastBar.Minimum, Math.Min(ContrastBar.Maximum, value)); }
        }

        public double Gamma
        {
            get => GammaBar != null ? GammaBar.Value / 100.0 : 1.0;
            set
            {
                if (GammaBar != null)
                    GammaBar.Value = (int)Math.Max(GammaBar.Minimum, Math.Min(GammaBar.Maximum, value * 100));
            }
        }

        public int DVL
        {
            get => DVLBar != null ? DVLBar.Value : 0;
            set
            {
                if (DVLBar != null)
                    DVLBar.Value = Math.Max(DVLBar.Minimum, Math.Min(DVLBar.Maximum, value)); // [수정] 안전 클램핑
            }
        }

        public double BlackStabilizer
        {
            get => BlackStabilizerBar != null ? BlackStabilizerBar.Value : 0;
            set
            {
                if (BlackStabilizerBar != null)
                    BlackStabilizerBar.Value = (int)Math.Max(BlackStabilizerBar.Minimum, Math.Min(BlackStabilizerBar.Maximum, value)); // [수정]
            }
        }

        public double WhiteStabilizer
        {
            get => WhiteStabilizerBar != null ? WhiteStabilizerBar.Value : 0;
            set
            {
                if (WhiteStabilizerBar != null)
                    WhiteStabilizerBar.Value = (int)Math.Max(WhiteStabilizerBar.Minimum, Math.Min(WhiteStabilizerBar.Maximum, value)); // [수정]
            }
        }

        public bool IsDynamicAdaptive
        {
            get => DynamicAdaptiveCheckBox != null && DynamicAdaptiveCheckBox.Checked;
            set { if (DynamicAdaptiveCheckBox != null) DynamicAdaptiveCheckBox.Checked = value; }
        }

        public double Dehaze
        {
            get => DehazeBar != null ? DehazeBar.Value : 0;
            set { if (DehazeBar != null) DehazeBar.Value = (int)Math.Max(DehazeBar.Minimum, Math.Min(DehazeBar.Maximum, value)); }
        }

        public double FoliagePop
        {
            get => FoliageBar != null ? FoliageBar.Value : 0;
            set { if (FoliageBar != null) FoliageBar.Value = (int)Math.Max(FoliageBar.Minimum, Math.Min(FoliageBar.Maximum, value)); }
        }

        public int Sharpening
        {
            get => SharpenBar != null ? SharpenBar.Value : 0;
            set { if (SharpenBar != null) SharpenBar.Value = Math.Max(SharpenBar.Minimum, Math.Min(SharpenBar.Maximum, value)); }
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
            else if (label.Equals(DehazeLabel) && DehazeBar != null)
                DehazeBar.Value = 0;
            else if (label.Equals(FoliageLabel) && FoliageBar != null)
                FoliageBar.Value = 0;
            else if (label.Equals(SharpenLabel) && SharpenBar != null)
                SharpenBar.Value = 0;

            SyncUIToCurrentProfile();
            if (_isPreviewExpanded) RenderAndRefreshPreview();
            ScheduleSave();
        }

        private void TrackBar_ValueChanged(object sender, EventArgs e)
        {
            var trackBar = sender as TrackBar;

            if (trackBar.Equals(BrightnessBar))
            {
                BrightnessText.Text = BrightnessBar.Value.ToString();
                ColorController.Instance.Brightness = BrightnessBar.Value;
            }
            else if (trackBar.Equals(ContrastBar))
            {
                ContrastText.Text = ContrastBar.Value.ToString();
                ColorController.Instance.Contrast = ContrastBar.Value;
            }
            else if (trackBar.Equals(GammaBar))
            {
                GammaText.Text = (GammaBar.Value / 100.0).ToString("0.00");
                ColorController.Instance.Gamma = GammaBar.Value / 100.0;
            }
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
            else if (trackBar.Equals(DehazeBar))
            {
                DehazeText.Text = DehazeBar.Value.ToString();
                ColorController.Instance.Dehaze = DehazeBar.Value;
            }
            else if (trackBar.Equals(FoliageBar))
            {
                FoliageText.Text = FoliageBar.Value.ToString();
                ColorController.Instance.FoliagePop = FoliageBar.Value;
            }
            else if (trackBar.Equals(SharpenBar))
            {
                SharpenText.Text = SharpenBar.Value.ToString();
                _drsDebounceTimer?.Stop();
                _drsDebounceTimer?.Start();
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
                pMonitor.Close();
            }
        }
    }
}