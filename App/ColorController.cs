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

        // [방안 A 기본값 50]
        public double Brightness { get; set; } = 50.0;
        public double Contrast { get; set; } = 50.0;
        public double Gamma { get; set; } = 1.0;
        public double BlackStabilizer { get; set; } = 0;
        public double WhiteStabilizer { get; set; } = 0;
        public bool IsDynamicAdaptive { get; set; } = false;

        public double Dehaze { get; set; } = 0;
        public double FoliagePop { get; set; } = 0;

        public float CurrentDynamicBoost => _currentDynamicBoost;

        #region Singleton Pattern implement
        private static readonly Lazy<ColorController> instance =
            new Lazy<ColorController>(() => new ColorController());

        public static ColorController Instance => instance.Value;
        #endregion

        [DllImport("gdi32")]
        private static extern bool GetDeviceGammaRamp(IntPtr hDc, ref RAMP lpRamp);

        [DllImport("gdi32")]
        private static extern bool SetDeviceGammaRamp(IntPtr hDc, ref RAMP lpRamp);

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

        public void ChangeColorRamp(double brightness = 50.0, double contrast = 50.0, double gamma = 1.0, bool reset = true)
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
                        var (rampR, rampG, rampB) = CalculateLUT(
                            this.Brightness,
                            this.Contrast,
                            this.Gamma,
                            this.BlackStabilizer,
                            this.WhiteStabilizer,
                            this._currentDynamicBoost,
                            this.Dehaze,
                            this.FoliagePop
                        );

                        currentRamps.Red = rampR;
                        currentRamps.Green = rampG;
                        currentRamps.Blue = rampB;
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
                        _currentDynamicBoost = (_currentDynamicBoost * 0.4f) + (targetBoost * 0.6f);
                    else
                        _currentDynamicBoost = (_currentDynamicBoost * 0.8f) + (targetBoost * 0.2f);

                    var (rampR, rampG, rampB) = CalculateLUT(
                        this.Brightness,
                        this.Contrast,
                        this.Gamma,
                        this.BlackStabilizer,
                        this.WhiteStabilizer,
                        this._currentDynamicBoost,
                        this.Dehaze,
                        this.FoliagePop
                    );
                    currentRamps.Red = rampR;
                    currentRamps.Green = rampG;
                    currentRamps.Blue = rampB;
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

        public static (ushort[] Red, ushort[] Green, ushort[] Blue) CalculateLUT(
            double brightness,
            double contrast,
            double gamma,
            double blackStabilizer,
            double whiteStabilizer,
            float dynamicBoost,
            double dehaze = 0,
            double foliagePop = 0)
        {
            const int dataPoints = 256;

            // [방안 A] 0~100 입력을 -1.0 ~ +1.0으로 정규화 (50이 정확히 0.0 중립 기준점)
            double normB = (Math.Min(Math.Max(brightness, 0.0), 100.0) - 50.0) / 50.0;
            double normC = (Math.Min(Math.Max(contrast, 0.0), 100.0) - 50.0) / 50.0;
            gamma = Math.Min(Math.Max(gamma, 0.4), 2.8);

            float powerBoost = (float)Math.Pow(dynamicBoost, 1.2);
            double effC = normC - (powerBoost * 0.15);
            effC = Math.Max(effC, -0.9);

            var offset = effC > 0 ? effC * -25.4 : effC * -32.0;
            var range = (dataPoints - 1) + offset * 2.0;
            offset += normB * (range / 5.0);

            double baseBlack = Math.Min(Math.Max(blackStabilizer, 0), 100) / 100.0;
            double baseWhite = Math.Min(Math.Max(whiteStabilizer, 0), 100) / 100.0;

            double effectiveBlack = Math.Min(baseBlack + (powerBoost * 0.85), 1.0);
            double effectiveWhite = Math.Min(baseWhite + (powerBoost * 0.60), 1.0);

            double Y_cap = 1.0 - (effectiveWhite * 0.30);
            double T_knee = 1.0 - (effectiveWhite * 0.50);

            double normDehaze = Math.Min(Math.Max(dehaze, 0), 100) / 100.0;
            double normFoliage = Math.Min(Math.Max(foliagePop, 0), 100) / 100.0;

            var rRamp = new ushort[dataPoints];
            var gRamp = new ushort[dataPoints];
            var bRamp = new ushort[dataPoints];

            double prevR = 0.0, prevG = 0.0, prevB = 0.0;

            for (var i = 0; i < dataPoints; i++)
            {
                var factor = (i + offset) / range;

                // [NaN 방지] Math.Pow 전 0.0 ~ 1.0 안전 클램핑
                factor = Math.Min(Math.Max(factor, 0.0), 1.0);
                factor = Math.Pow(factor, 1.0 / gamma);

                // 1. 단조 증가 블랙 스태빌라이저
                if (effectiveBlack > 0)
                {
                    double blackWeight = Math.Pow(1.0 - factor, 2.0);
                    factor += (effectiveBlack * 0.35 * blackWeight);
                }

                // 2. 소프트 니 화이트 스태빌라이저
                if (effectiveWhite > 0 && factor > T_knee)
                {
                    double t = (factor - T_knee) / (1.0 - T_knee);
                    double compressedPart = (2.0 * t) - (t * t);
                    factor = T_knee + (Y_cap - T_knee) * compressedPart;
                }

                factor = Math.Min(Math.Max(factor, 0.0), 1.0);

                double r = factor;
                double g = factor;
                double b = factor;

                // 3. 안개 억제 (Dehaze)
                if (normDehaze > 0.001)
                {
                    double fogMask = 4.0 * factor * Math.Pow(Math.Max(0.0, 1.0 - factor), 1.6);
                    r -= (0.03 * normDehaze * fogMask);
                    g -= (0.11 * normDehaze * fogMask);
                    b -= (0.20 * normDehaze * fogMask);
                }

                // 4. 수풀 적 분리 (Foliage Pop)
                if (normFoliage > 0.001)
                {
                    double foliageMask = Math.Sin(Math.PI * factor);
                    r *= (1.0 + (0.16 * normFoliage * foliageMask));
                    g *= (1.0 - (0.22 * normFoliage * foliageMask));
                    b *= (1.0 + (0.08 * normFoliage * foliageMask));
                }

                // 5. 단조 증가 클램핑
                r = Math.Max(prevR, Math.Min(Math.Max(r, 0.0), 1.0));
                g = Math.Max(prevG, Math.Min(Math.Max(g, 0.0), 1.0));
                b = Math.Max(prevB, Math.Min(Math.Max(b, 0.0), 1.0));

                prevR = r;
                prevG = g;
                prevB = b;

                rRamp[i] = (ushort)Math.Round(r * ushort.MaxValue);
                gRamp[i] = (ushort)Math.Round(g * ushort.MaxValue);
                bRamp[i] = (ushort)Math.Round(b * ushort.MaxValue);
            }

            return (rRamp, gRamp, bRamp);
        }

        public void ResetDVL()
        {
            try { gpu.ResetSaturation(); } catch { }
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