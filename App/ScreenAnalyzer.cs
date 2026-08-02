using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace tarkov_settings
{
    public class ScreenAnalyzer : IDisposable
    {
        private readonly Bitmap _bmp;
        private readonly Graphics _gfx;
        private readonly int _width = 32;
        private readonly int _height = 32;

        // [추가] 공간 가중치 맵 및 최대 가중치 총합
        private readonly float[,] _spatialWeights = new float[32, 32];
        private readonly float _totalMaxWeight = 0f;

        public ScreenAnalyzer()
        {
            _bmp = new Bitmap(_width, _height, PixelFormat.Format24bppRgb);
            _gfx = Graphics.FromImage(_bmp);

            // [핵심] 생성자에서 측면(좌우) 광원에 더 높은 가중치를 부여하는 32x32 맵 사전 계산 (0.0ms)
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    // 화면 중심(15.5)으로부터의 수평/수직 거리 정규화 (0.0 ~ 1.0)
                    float distX = Math.Abs(x - 15.5f) / 15.5f;
                    float distY = Math.Abs(y - 15.5f) / 15.5f;

                    // 좌/우 측면 외곽일수록 높은 가중치 부여 (중앙 1.0배 ~ 측면 외곽 최대 3.0배)
                    float weight = 1.0f + (distX * distX * 2.0f) + (distY * distY * 0.5f);

                    _spatialWeights[x, y] = weight;
                    _totalMaxWeight += weight;
                }
            }
        }

        /// <summary>
        /// 측면 광원에 높은 가중치를 두어 화면 내 광원 유입 비율(0.0 ~ 1.0)을 정교하게 계산합니다.
        /// 화이트 스태빌라이저 수치에 따라 광원 감지 임계값을 동적으로 보정하여 광원 비율을 계산합니다.
        /// </summary>
        public float AnalyzeLightRatio(double whiteStabilizer = 0)
        {
            try
            {
                // [핵심] 화이트 스태빌라이저로 깎인 밝기만큼 감지 임계값을 자동으로 낮춤 (190 -> 최소 140)
                int targetThreshold = (int)Math.Max(190 - (whiteStabilizer * 0.5), 140);

                var screenBounds = Screen.PrimaryScreen.Bounds;

                _gfx.CopyFromScreen(
                    screenBounds.X, screenBounds.Y, 0, 0,
                    new Size(screenBounds.Width, screenBounds.Height),
                    CopyPixelOperation.SourceCopy
                );

                BitmapData data = _bmp.LockBits(
                    new Rectangle(0, 0, _width, _height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb
                );

                int stride = Math.Abs(data.Stride);
                int bytes = stride * _height;
                byte[] rgbValues = new byte[bytes];
                Marshal.Copy(data.Scan0, rgbValues, 0, bytes);
                _bmp.UnlockBits(data);

                float weightedBlindingSum = 0f;

                for (int y = 0; y < _height; y++)
                {
                    int rowOffset = y * stride;
                    for (int x = 0; x < _width; x++)
                    {
                        int i = rowOffset + (x * 3);
                        byte b = rgbValues[i];
                        byte g = rgbValues[i + 1];
                        byte r = rgbValues[i + 2];

                        // 휘도(Luminance) 계산
                        int luminance = (r * 299 + g * 587 + b * 114) / 1000;


                        // 동적 감지 임계값 적용
                        if (luminance >= targetThreshold)
                        {
                            weightedBlindingSum += _spatialWeights[x, y];
                        }
                    }
                }

                // 측면 광원 점유율 정규화 (가중치 총합의 25% 이상 감지 시 최대 보상 1.0 도달)
                float ratio = weightedBlindingSum / (_totalMaxWeight * 0.25f);
                return Math.Min(ratio, 1.0f);
            }
            catch
            {
                return 0f;
            }
        }

        public void Dispose()
        {
            _gfx?.Dispose();
            _bmp?.Dispose();
        }
    }
}