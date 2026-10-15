using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using tarkov_settings.GPU;

namespace tarkov_settings
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct RAMP
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Red;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Green;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Blue;
    }

    class Display
    {
        static IGPU gpu;

        [DllImport("gdi32")]
        public static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32")]
        public static extern bool DeleteDC([In] IntPtr hdc);

        public readonly static List<string> displays;
        private static string _primary;
        public static string Primary
        {
            get => _primary;
            set
            {
                // [수정] displays가 비어있거나 null이 들어올 경우의 방어 코드
                if (!string.IsNullOrEmpty(value) && displays != null && displays.Contains(value))
                {
                    _primary = value;
                }
                else if (displays != null && displays.Count > 0)
                {
                    _primary = displays[0];
                }
                else
                {
                    _primary = @"\\.\DISPLAY1"; // 모니터 리스트가 일시적으로 비었을 때 안전 폴백
                }

                try
                {
                    if (!string.IsNullOrEmpty(_primary) && gpu != null)
                    {
                        gpu.Load(_primary);
                    }
                }
                catch (NotImplementedException) { }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Display Load Error] {ex.Message}");
                }
            }
        }

        static Display()
        {
            displays = GetWinDisplays();
            gpu = GPUDevice.Instance;
        }

        private static List<string> GetWinDisplays()
        {
            List<string> list = new List<string>();
            foreach (Screen screen in Screen.AllScreens)
            {
                list.Add(screen.DeviceName);
            }
            return list;
        }
    }
}
