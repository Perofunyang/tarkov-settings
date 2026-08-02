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
        /// 단조 증가(Monotonic Increase)가 보장되어 밝기 역전 현상이 완전히 차단된 256 LUT 연산 함수
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

            var result = new ushort[dataPoints];
            for (var i = 0; i < result.Length; i++)
            {
                var factor = (i + offset) / range;
                factor = Math.Pow(factor, 1 / gamma);
                factor = Math.Min(Math.Max(factor, 0), 1);

                // [개선 1] 토 앵커 제거 및 단조 증가 블랙 곡선 적용 (f'(x) >= 0.3 보장으로 밝기 역전 차단)
                if (effectiveBlack > 0)
                {
                    double blackWeight = Math.Pow(1.0 - factor, 2.0);
                    factor += (effectiveBlack * 0.35 * blackWeight);
                }

                // [개선 2] 단조 감소 화이트 곡선 적용 (밝기 역전 차단)
                if (effectiveWhite > 0)
                {
                    double whiteWeight = Math.Pow(factor, 2.0);
                    factor -= (effectiveWhite * 0.30 * whiteWeight);
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