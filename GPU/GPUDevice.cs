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

                    // Intel / 기타: 채도·샤프닝 없이 감마 램프 기능만 사용
                    default:
                        return new NullGpu(vendor);
                }
            });

        public static IGPU Instance => instance.Value;

        /// <summary>
        /// Win32 API를 사용해 1ms 만에 하드웨어 Vendor ID(NVIDIA, AMD, Intel)를 감지합니다.
        /// </summary>
        private static GPUVendor DetectGPUVendor()
        {
            // 하이브리드(Intel iGPU + NVIDIA dGPU) 노트북은 Intel 어댑터가 먼저 열거될 수 있으므로,
            // 첫 매칭에서 바로 반환하지 않고 전체를 훑은 뒤 NVIDIA > AMD > Intel 순으로 우선합니다.
            bool hasNvidia = false, hasAmd = false, hasIntel = false;

            try
            {
                DISPLAY_DEVICE d = new DISPLAY_DEVICE();
                d.cb = Marshal.SizeOf(d);

                uint devNum = 0;
                while (EnumDisplayDevices(null, devNum, ref d, 0))
                {
                    string deviceId = (d.DeviceID ?? string.Empty).ToUpperInvariant();

                    if (deviceId.Contains("VEN_10DE")) hasNvidia = true;       // NVIDIA
                    else if (deviceId.Contains("VEN_1002")) hasAmd = true;     // AMD
                    else if (deviceId.Contains("VEN_8086")) hasIntel = true;   // INTEL

                    devNum++;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GPU 벤더 감지 실패", ex);
            }

            if (hasNvidia) return GPUVendor.NVIDIA;
            if (hasAmd) return GPUVendor.AMD;
            if (hasIntel) return GPUVendor.INTEL;
            return GPUVendor.ETC;
        }
    }
}