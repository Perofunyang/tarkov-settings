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
        private bool _closed = false;       // Close() 완료 후에는 EmergencyReset이 NvAPI를 다시 건드리지 않도록
        private bool _lastRampOk = true;   // SetDeviceGammaRamp 성공 여부 (상태 변화 시에만 로그)

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

                // [수정] 빈 RAMP 대신, Red/Green/Blue에 256개짜리 배열을 미리 1회 할당
                currentRamps = new RAMP
                {
                    Red = new ushort[256],
                    Green = new ushort[256],
                    Blue = new ushort[256]
                };
                // [수정] originalRamps에도 반드시 256개 배열을 1회 할당해야 마샬러가 네이티브 값을 정상 복사해옵니다.
                originalRamps = new RAMP
                {
                    Red = new ushort[256],
                    Green = new ushort[256],
                    Blue = new ushort[256]
                };

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
                        _currentDynamicBoost = 0f; // [추가] 이전 동적 부스팅 잔류 수치 리셋

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
                            TrackRampResult(SetDeviceGammaRamp(hdc, ref currentRamps));
                        }

                        int timerInterval = IsDynamicAdaptive ? 50 : 250;
                        _gammaTimer.Change(timerInterval, timerInterval);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("ColorController 예외", ex);
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
                        TrackRampResult(SetDeviceGammaRamp(hdc, ref currentRamps));
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
        /// 일부 드라이버/Windows 버전은 항등 램프에서 크게 벗어난 램프를 거부합니다.
        /// 20Hz로 호출되므로 상태가 바뀔 때만 한 번 기록합니다.
        /// </summary>
        private void TrackRampResult(bool ok)
        {
            if (ok == _lastRampOk) return;
            _lastRampOk = ok;

            if (!ok)
                Logger.Error($"SetDeviceGammaRamp 실패 (Display={Display.Primary}, B={Brightness}, C={Contrast}, G={Gamma:0.00}) - 드라이버가 램프를 거부했을 수 있습니다.");
            else
                Logger.Info("SetDeviceGammaRamp 정상 복구");
        }

        /// <summary>
        /// 비정상 종료/프로세스 종료 시 호출되는 최후 복구 루틴. 인스턴스가 아직 없으면 아무것도 하지 않습니다.
        /// </summary>
        internal static void EmergencyReset()
        {
            if (!instance.IsValueCreated) return;

            var c = instance.Value;
            if (c._closed) return;

            try { c.ResetDVL(); } catch { }
            try { c.ApplyColorSettings(reset: true); } catch { }
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

                // 1. 단조 증가 블랙 스태빌라이저 (토우 보존 암부 발굴)
                // 완전 칠흑(0)의 회색 들뜸을 막고, 적이 숨은 어두운 음영(0.05~0.25)만 핀포인트로 발굴
                if (effectiveBlack > 0)
                {
                    // factor가 0에 가까울 때는 바닥을 지키고, 어두운 디테일만 볼록하게 띄우는 벨 커브 가중치
                    double shadowPocket = Math.Pow(factor, 0.45) * Math.Pow(1.0 - factor, 1.8);
                    factor += (effectiveBlack * 0.45 * shadowPocket);
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

                // 3. 안개 억제 (Dehaze): 붕 뜬 로우-미드톤을 눌러 쨍한 콘트라스트 복원
                if (normDehaze > 0.001)
                {
                    double fogMask = 4.0 * factor * Math.Pow(Math.Max(0.0, 1.0 - factor), 1.6);
                    // 안개 장막을 확실하게 도려내도록 감산 파워를 0.12 -> 0.22로 강화
                    double dehazeCut = 0.22 * normDehaze * fogMask;
                    r -= dehazeCut;
                    g -= dehazeCut;
                    b -= dehazeCut;
                }

                // 4. 수풀 적 분리 (Foliage Pop): 중립 회색을 보존하는 식생 억제
                // 붉은기 폭발의 원인이던 과도한 Red 부스팅을 억제하고, 녹색 미드톤만 부드럽게 진정
                if (normFoliage > 0.001)
                {
                    double foliageMask = Math.Sin(Math.PI * factor);
                    g *= (1.0 - (0.09 * normFoliage * foliageMask)); // 눈 아픈 녹색 중간톤만 차분하게 억제
                    r *= (1.0 + (0.02 * normFoliage * foliageMask)); // 인체 윤곽선만 아주 미세하게 보존
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
                _closed = true;
            }
        }
    }
}