namespace tarkov_settings.GPU
{
    /// <summary>
    /// Intel / 기타 GPU용 구현. 채도(Digital Vibrance)와 DRS는 지원하지 않지만,
    /// 감마 램프(밝기/대비/감마/스태빌라이저)는 GPU 벤더와 무관하게 동작하므로 앱 실행은 허용합니다.
    /// </summary>
    internal class NullGpu : IGPU
    {
        public GPUVendor Vendor { get; }

        public int Saturation { get; set; }
        public int MaxSaturation => 0;
        public int MinSaturation => 0;
        public int InitSaturation => 0;

        public NullGpu(GPUVendor vendor)
        {
            Vendor = vendor;
        }

        public void ResetSaturation() { }
        public void Load(string display) { }
        public void Close() { }
    }
}
