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

        // 실시간 동적 보상 수치(0.0 ~ 1.0) 공개 프로퍼티
        public float CurrentDynamicBoost => _currentDynamicBoost;

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
            // 리셋이 아니고, 타르코프 게임이 포커스되어 있지 않다면 모니터 색상 변경 안 함
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

                    // 비대칭 보간 (Fast Attack 60% / Smooth Release 20%)
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
        /// 단조 증가 블랙 발굴 + 소프트 니 상한선 캡(Soft Knee Cap) 화이트 압축이 통합된 256 LUT 연산 함수
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

            double effectiveBlack = Math.Min(baseBlack + (powerBoost * 0.85), 1.0);
            double effectiveWhite = Math.Min(baseWhite + (powerBoost * 0.60), 1.0);

            // [소프트 니 상한선 캡 파라미터 계산]
            // Y_cap  : 눈뽕 최대 상한선 (100 설정 시 1.0 -> 0.70, 즉 RGB 255 -> 178)
            // T_knee : 미드톤 원본 보존 문턱값 (100 설정 시 1.0 -> 0.50, 즉 RGB 128 이하 100% 원본 보존)
            double Y_cap = 1.0 - (effectiveWhite * 0.30);
            double T_knee = 1.0 - (effectiveWhite * 0.50);

            var result = new ushort[dataPoints];
            for (var i = 0; i < result.Length; i++)
            {
                var factor = (i + offset) / range;
                factor = Math.Pow(factor, 1 / gamma);
                factor = Math.Min(Math.Max(factor, 0), 1);

                // 1. 단조 증가 블랙 스태빌라이저 (암부 발굴, 밝기 역전 현상 차단)
                if (effectiveBlack > 0)
                {
                    double blackWeight = Math.Pow(1.0 - factor, 2.0);
                    factor += (effectiveBlack * 0.35 * blackWeight);
                }

                // 2. [소프트 니 상한선 캡] 화이트 스태빌라이저
                if (effectiveWhite > 0)
                {
                    if (factor > T_knee)
                    {
                        // T_knee 초과 영역만 2차 곡선(2t - t^2)으로 부드럽게 상한선 캡 아래로 억제
                        double t = (factor - T_knee) / (1.0 - T_knee);
                        double compressedPart = (2.0 * t) - (t * t);
                        factor = T_knee + (Y_cap - T_knee) * compressedPart;
                    }
                    // factor <= T_knee 지점은 factor 변환율 0.00% (미드톤 및 어둠 영역 100% 원본 완전 보존!)
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