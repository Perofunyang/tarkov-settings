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

        private readonly float[,] _spatialWeights = new float[32, 32];
        private readonly float _totalMaxWeight = 0f;

        public ScreenAnalyzer()
        {
            _bmp = new Bitmap(_width, _height, PixelFormat.Format24bppRgb);
            _gfx = Graphics.FromImage(_bmp);

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    float distX = Math.Abs(x - 15.5f) / 15.5f;
                    float distY = Math.Abs(y - 15.5f) / 15.5f;

                    // 선형 측면 가중치 (중앙 1.0배 -> 측면 3.0배)
                    float weight = 1.0f + (distX * 2.0f) + (distY * 0.5f);

                    _spatialWeights[x, y] = weight;
                    _totalMaxWeight += weight;
                }
            }
        }

        /// <summary>
        /// 타르코프 상인/보관함/메인 메뉴 UI 화면인지 0.001ms 만에 초고속 스캔합니다.
        /// </summary>
        private bool IsTarkovMenuUI(byte[] rgbValues, int stride)
        {
            try
            {
                // 1. 하단 메뉴 바 스캔 (y = 31 행의 샘플 픽셀들이 어두운 무채색 패널인지 확인)
                int bottomRowOffset = 31 * stride;
                int darkBottomPixels = 0;

                for (int x = 2; x < 30; x += 3)
                {
                    int i = bottomRowOffset + (x * 3);
                    byte b = rgbValues[i];
                    byte g = rgbValues[i + 1];
                    byte r = rgbValues[i + 2];

                    // 타르코프 하단 메뉴 바 특유의 어두운 무채색 회색 (RGB 35 이하)
                    if (r <= 35 && g <= 35 && b <= 35 && Math.Abs(r - g) <= 12 && Math.Abs(g - b) <= 12)
                    {
                        darkBottomPixels++;
                    }
                }

                // 2. 상단 헤더 바 스캔 (y = 0 행의 샘플 픽셀들)
                int topRowOffset = 0 * stride;
                int darkTopPixels = 0;

                for (int x = 2; x < 30; x += 3)
                {
                    int i = topRowOffset + (x * 3);
                    byte b = rgbValues[i];
                    byte g = rgbValues[i + 1];
                    byte r = rgbValues[i + 2];

                    if (r <= 40 && g <= 40 && b <= 40)
                    {
                        darkTopPixels++;
                    }
                }

                // 상단과 하단 영역이 동시에 타르코프 메뉴 UI 패턴과 일치하면 메뉴/상인 화면으로 판별!
                return (darkBottomPixels >= 7 && darkTopPixels >= 7);
            }
            catch
            {
                return false;
            }
        }

        public float AnalyzeLightRatio(double whiteStabilizer = 0)
        {
            try
            {
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

                // [핵심] 상인/보관함/메인 메뉴 UI 감지 시 동적 제어 즉시 끄기 (0.0 반환)
                if (IsTarkovMenuUI(rgbValues, stride))
                {
                    return 0.0f;
                }

                int targetThreshold = (int)Math.Max(160 - (whiteStabilizer * 0.4), 120);
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

                        int avgBrightness = (r + g + b) / 3;

                        if (avgBrightness >= targetThreshold)
                        {
                            weightedBlindingSum += _spatialWeights[x, y];
                        }
                    }
                }

                float ratio = weightedBlindingSum / (_totalMaxWeight * 0.12f);
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