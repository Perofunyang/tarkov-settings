using System;
using System.Runtime.InteropServices;

namespace tarkov_settings.GPU
{
    public enum GPUVendor
    {
        NVIDIA = 1,
        AMD = 2,
        INTEL = 3,
        ETC = 4
    }

    class GPUDevice
    {
        #region Win32 API for Ultra-Fast GPU Vendor Detection
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);
        #endregion

        private static readonly Lazy<IGPU> instance =
            new Lazy<IGPU>(() =>
            {
                var vendor = DetectGPUVendor();

                // 확장성 확보: 향후 새로운 GPU 클래스가 추가되면 아래 switch문만 확장하면 됨
                switch (vendor)
                {
                    case GPUVendor.NVIDIA:
                        return new NVIDIA(vendor);

                    case GPUVendor.AMD:
                        return new AMD(vendor);

                    // 향후 인텔 Arc 지원 시 추가 영역
                    // case GPUVendor.INTEL:
                    //     return new Intel(vendor);

                    default:
                        throw new NotImplementedException("지원되지 않는 그래픽 카드입니다.");
                }
            });

        public static IGPU Instance => instance.Value;

        /// <summary>
        /// Win32 API를 사용해 1ms 만에 하드웨어 Vendor ID(NVIDIA, AMD, Intel)를 감지합니다.
        /// </summary>
        private static GPUVendor DetectGPUVendor()
        {
            try
            {
                DISPLAY_DEVICE d = new DISPLAY_DEVICE();
                d.cb = Marshal.SizeOf(d);

                uint devNum = 0;
                while (EnumDisplayDevices(null, devNum, ref d, 0))
                {
                    string deviceId = d.DeviceID.ToUpper();

                    // VEN_10DE = NVIDIA
                    if (deviceId.Contains("VEN_10DE"))
                        return GPUVendor.NVIDIA;

                    // VEN_1002 = AMD
                    if (deviceId.Contains("VEN_1002"))
                        return GPUVendor.AMD;

                    // VEN_8086 = INTEL
                    if (deviceId.Contains("VEN_8086"))
                        return GPUVendor.INTEL;

                    devNum++;
                }
            }
            catch { }

            return GPUVendor.ETC;
        }
    }
}