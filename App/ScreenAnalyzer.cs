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

                    // [개선] 측면 가중치 대폭 상향 (중앙 1.0배 -> 측면 외곽 최대 5.0배)
                    float weight = 1.0f + (distX * 4.0f) + (distY * 1.0f);

                    _spatialWeights[x, y] = weight;
                    _totalMaxWeight += weight;
                }
            }
        }

        public float AnalyzeLightRatio(double whiteStabilizer = 0)
        {
            try
            {
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

                        int luminance = (r * 299 + g * 587 + b * 114) / 1000;

                        if (luminance >= targetThreshold)
                        {
                            weightedBlindingSum += _spatialWeights[x, y];
                        }
                    }
                }

                // [개선] 감지 모수 분모를 10%로 축소하여 구석의 작은 불빛에도 100% 풀 부스트 발동
                float ratio = weightedBlindingSum / (_totalMaxWeight * 0.10f);
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