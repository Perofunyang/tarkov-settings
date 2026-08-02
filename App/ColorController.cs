using System;
using System.Runtime.InteropServices;
using System.Threading;
using tarkov_settings.GPU;

namespace tarkov_settings
{
    class ColorController
    {
        IGPU gpu = GPUDevice.Instance;

        private RAMP currentRamps;
        private RAMP originalRamps;

        private readonly Timer _gammaTimer;
        private readonly object _lockObject = new object();

        private bool _isRampActive = false;
        private bool _isInitialized = false;

        private ScreenAnalyzer _screenAnalyzer;
        private float _currentDynamicBoost = 0f;

        public double Brightness { get; set; } = 0.5;
        public double Contrast { get; set; } = 0.5;
        public double Gamma { get; set; } = 1.0;
        public double BlackStabilizer { get; set; } = 0;
        public double WhiteStabilizer { get; set; } = 0;
        public bool IsDynamicAdaptive { get; set; } = false;

        #region Singleton Pattern implement
        private static readonly Lazy<ColorController> instance =
            new Lazy<ColorController>(() => new ColorController());

        public static ColorController Instance => instance.Value;
        #endregion

        #region Win32 API Calls
        [DllImport("gdi32")]
        private static extern bool GetDeviceGammaRamp(IntPtr hDc, ref RAMP lpRamp);

        [DllImport("gdi32")]
        private static extern bool SetDeviceGammaRamp(IntPtr hDc, ref RAMP lpRamp);
        #endregion

        public int DVL
        {
            get => gpu.Saturation;
            set => gpu.Saturation = value;
        }

        private ColorController()
        {
            _gammaTimer = new Timer(OnGammaTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
            _screenAnalyzer = new ScreenAnalyzer();
        }

        public void Init()
        {
            var hdc = IntPtr.Zero;
            try
            {
                hdc = Display.CreateDC(null, Display.Primary, null, IntPtr.Zero);
                currentRamps = new RAMP();
                originalRamps = new RAMP();

                if (GetDeviceGammaRamp(hdc, ref originalRamps))
                {
                    _isInitialized = true;
                }
            }
            finally
            {
                if (!IntPtr.Zero.Equals(hdc))
                    Display.DeleteDC(hdc);
            }
        }

        public void ChangeColorRamp(double brightness = 0.5, double contrast = 0.5, double gamma = 1.0, bool reset = true)
        {
            this.Brightness = brightness;
            this.Contrast = contrast;
            this.Gamma = gamma;

            ApplyColorSettings(reset);
        }

        public void ApplyColorSettings(bool reset = false)
        {
            if (!reset && !ProcessMonitor.Instance.IsTargetFocused)
            {
                return;
            }

            lock (_lockObject)
            {
                var hdc = IntPtr.Zero;
                try
                {
                    hdc = Display.CreateDC(null, Display.Primary, null, IntPtr.Zero);

                    if (reset)
                    {
                        _isRampActive = false;
                        _gammaTimer.Change(Timeout.Infinite, Timeout.Infinite);

                        if (_isInitialized && !IntPtr.Zero.Equals(hdc))
                        {
                            SetDeviceGammaRamp(hdc, ref originalRamps);
                        }
                    }
                    else
                    {
                        ushort[] iArrayValue = CalculateLUT(
                            this.Brightness,
                            this.Contrast,
                            this.Gamma,
                            this.BlackStabilizer,
                            this.WhiteStabilizer,
                            this._currentDynamicBoost
                        );

                        currentRamps.Red = currentRamps.Blue = currentRamps.Green = iArrayValue;
                        _isRampActive = true;

                        if (!IntPtr.Zero.Equals(hdc))
                        {
                            SetDeviceGammaRamp(hdc, ref currentRamps);
                        }

                        int timerInterval = IsDynamicAdaptive ? 50 : 250;
                        _gammaTimer.Change(timerInterval, timerInterval);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ColorController Exception] {ex.Message}");
                }
                finally
                {
                    if (!IntPtr.Zero.Equals(hdc))
                        Display.DeleteDC(hdc);
                }
            }
        }

        private void OnGammaTimerCallback(object state)
        {
            lock (_lockObject)
            {
                if (!_isRampActive) return;

                if (IsDynamicAdaptive && _screenAnalyzer != null)
                {
                    float targetBoost = _screenAnalyzer.AnalyzeLightRatio(this.WhiteStabilizer);

                    if (targetBoost > _currentDynamicBoost)
                    {
                        _currentDynamicBoost = (_currentDynamicBoost * 0.4f) + (targetBoost * 0.6f);
                    }
                    else
                    {
                        _currentDynamicBoost = (_currentDynamicBoost * 0.8f) + (targetBoost * 0.2f);
                    }

                    ushort[] iArrayValue = CalculateLUT(
                        this.Brightness,
                        this.Contrast,
                        this.Gamma,
                        this.BlackStabilizer,
                        this.WhiteStabilizer,
                        this._currentDynamicBoost
                    );
                    currentRamps.Red = currentRamps.Blue = currentRamps.Green = iArrayValue;
                }
                else
                {
                    _currentDynamicBoost = 0f;
                }

                var hdc = IntPtr.Zero;
                try
                {
                    hdc = Display.CreateDC(null, Display.Primary, null, IntPtr.Zero);
                    if (!IntPtr.Zero.Equals(hdc))
                    {
                        SetDeviceGammaRamp(hdc, ref currentRamps);
                    }
                }
                catch { }
                finally
                {
                    if (!IntPtr.Zero.Equals(hdc))
                        Display.DeleteDC(hdc);
                }
            }
        }

        /// <summary>
        /// 삼중 동적 제어 및 6차 고차 곡선 연산 함수
        /// </summary>
        private static ushort[] CalculateLUT(
            double brightness,
            double contrast,
            double gamma,
            double blackStabilizer,
            double whiteStabilizer,
            float dynamicBoost)
        {
            const int dataPoints = 256;

            // [개선 1] 반응 곡선을 1.2승으로 변경하여 빠른 동적 상승 반응 유도
            float powerBoost = (float)Math.Pow(dynamicBoost, 1.2);

            double effectiveContrast = contrast - (powerBoost * 0.15);
            effectiveContrast = Math.Max(effectiveContrast, 0.1);

            effectiveContrast = (Math.Min(Math.Max(effectiveContrast, 0), 1) - 0.5) * 2;
            brightness = (Math.Min(Math.Max(brightness, 0), 1) - 0.5) * 2;
            gamma = Math.Min(Math.Max(gamma, 0.4), 2.8);

            var offset = effectiveContrast > 0 ? effectiveContrast * -25.4 : effectiveContrast * -32;
            var range = (dataPoints - 1) + offset * 2;
            offset += brightness * (range / 5);

            double baseBlack = Math.Min(Math.Max(blackStabilizer, 0), 100) / 100.0;
            double baseWhite = Math.Min(Math.Max(whiteStabilizer, 0), 100) / 100.0;

            // [개선 2] 동적 블랙 상승 상한 확대 (+0.75 -> +0.85)
            double effectiveBlack = Math.Min(baseBlack + (powerBoost * 0.85), 1.0);

            double effectiveWhite = Math.Min(baseWhite + (powerBoost * 0.60), 1.0);

            var result = new ushort[dataPoints];
            for (var i = 0; i < result.Length; i++)
            {
                var factor = (i + offset) / range;
                factor = Math.Pow(factor, 1 / gamma);
                factor = Math.Min(Math.Max(factor, 0), 1);

                // 블랙 스태빌라이저 (토 앵커 삼차 곡선)
                if (effectiveBlack > 0)
                {
                    double blackWeight = 4.0 * factor * Math.Pow(1.0 - factor, 3);
                    // [개선 3] 암부 끌어올리기 곱셈 계수 상향 (0.45 -> 0.70)
                    factor += (effectiveBlack * 0.70 * blackWeight);
                }

                // 화이트 스태빌라이저 (선택성 극대화: factor^6 고차 곡선 적용!)
                if (effectiveWhite > 0)
                {
                    // [핵심 개선 4] factor^6 고차 곡선으로 중간 톤은 100% 보존하고 오직 극단적 광원(210~255)만 핀포인트 억제!
                    double whiteWeight = Math.Pow(factor, 6.0);
                    factor -= (effectiveWhite * 0.50 * whiteWeight);
                }

                factor = Math.Min(Math.Max(factor, 0), 1);
                result[i] = (ushort)Math.Round(factor * ushort.MaxValue);
            }
            return result;
        }

        public void ResetDVL()
        {
            try
            {
                gpu.ResetSaturation();
            }
            catch (NotImplementedException) { }
        }

        internal void Close()
        {
            ResetDVL();
            ChangeColorRamp(reset: true);

            lock (_lockObject)
            {
                _gammaTimer?.Dispose();
                _screenAnalyzer?.Dispose();
            }
        }
    }
}