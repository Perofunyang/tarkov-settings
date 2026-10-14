using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace tarkov_settings
{
    public class ScreenAnalyzer : IDisposable
    {
        #region Win32 GDI Native P/Invoke
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool StretchBlt(
            IntPtr hdcDest, int nXOriginDest, int nYOriginDest, int nWidthDest, int nHeightDest,
            IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc,
            uint dwRop
        );

        [DllImport("gdi32.dll")]
        private static extern int SetStretchBltMode(IntPtr hdc, int iStretchMode);

        private const uint SRCCOPY = 0x00CC0020;
        private const int COLORONCOLOR = 3; // 초고속 서브샘플링 모드 (< 0.1ms)
        #endregion

        private readonly Bitmap _bmp;
        private readonly Graphics _gfx;
        private readonly int _width = 32;
        private readonly int _height = 32;

        private readonly float[,] _spatialWeights = new float[32, 32];
        private readonly float _totalMaxWeight = 0f;

        // [최적화] 매 프레임 GC 메모리 할당 방지용 고정 버퍼
        private readonly byte[] _rgbBuffer;

        public ScreenAnalyzer()
        {
            _bmp = new Bitmap(_width, _height, PixelFormat.Format24bppRgb);
            _gfx = Graphics.FromImage(_bmp);

            // 32 x 32 x 3 (24bpp stride 최대 128바이트)
            _rgbBuffer = new byte[128 * _height];

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

        private Screen GetTargetScreen()
        {
            try
            {
                string targetDevice = Display.Primary;
                if (!string.IsNullOrEmpty(targetDevice))
                {
                    foreach (Screen screen in Screen.AllScreens)
                    {
                        if (screen.DeviceName.Equals(targetDevice, StringComparison.OrdinalIgnoreCase))
                        {
                            return screen;
                        }
                    }
                }
            }
            catch { }

            return Screen.PrimaryScreen;
        }

        /// <summary>
        /// 전체 화면이 축소된 32x32 버퍼에서 상단(y=0)과 하단(y=31)의 타르코프 메뉴 바를 정밀 감지합니다.
        /// </summary>
        private bool IsTarkovMenuUI(byte[] rgbValues, int stride)
        {
            try
            {
                // 하단 메뉴 바 스캔 (y = 31)
                int bottomRowOffset = 31 * stride;
                int darkBottomPixels = 0;

                for (int x = 2; x < 30; x += 3)
                {
                    int i = bottomRowOffset + (x * 3);
                    byte b = rgbValues[i];
                    byte g = rgbValues[i + 1];
                    byte r = rgbValues[i + 2];

                    if (r <= 35 && g <= 35 && b <= 35 && Math.Abs(r - g) <= 12 && Math.Abs(g - b) <= 12)
                    {
                        darkBottomPixels++;
                    }
                }

                // 상단 헤더 바 스캔 (y = 0)
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
                Screen targetScreen = GetTargetScreen();
                Rectangle screenBounds = targetScreen.Bounds;

                // [핵심 해결] Win32 StretchBlt로 전체 모니터 화면을 32x32로 0.1ms 만에 초고속 하드웨어 축소 캡처
                IntPtr hdcScreen = GetDC(IntPtr.Zero);
                IntPtr hdcDest = _gfx.GetHdc();

                try
                {
                    SetStretchBltMode(hdcDest, COLORONCOLOR);
                    StretchBlt(
                        hdcDest, 0, 0, _width, _height,
                        hdcScreen, screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height,
                        SRCCOPY
                    );
                }
                finally
                {
                    _gfx.ReleaseHdc(hdcDest);
                    ReleaseDC(IntPtr.Zero, hdcScreen);
                }

                BitmapData data = _bmp.LockBits(
                    new Rectangle(0, 0, _width, _height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb
                );

                int stride = Math.Abs(data.Stride);
                int bytes = stride * _height;

                // 사전 할당된 버퍼로 복사 (Zero-GC Allocation)
                Marshal.Copy(data.Scan0, _rgbBuffer, 0, bytes);
                _bmp.UnlockBits(data);

                // 상인/보관함/메인 메뉴 UI 감지 시 동적 제어 차단
                if (IsTarkovMenuUI(_rgbBuffer, stride))
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
                        byte b = _rgbBuffer[i];
                        byte g = _rgbBuffer[i + 1];
                        byte r = _rgbBuffer[i + 2];

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