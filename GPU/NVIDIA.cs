using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using NvAPIWrapper.Native;
using NvAPIWrapper.Native.Display.Structures;

namespace tarkov_settings.GPU
{
    class NVIDIA : IGPU
    {
        private GPUVendor _vendor;
        private DisplayHandle displayHandle;

        private int _maxSaturation;
        private int _minSaturation;
        private int _initSaturation;
        private int currentSaturation;
        private bool _loaded = false;   // DVC 핸들이 정상적으로 로드되었는지

        public GPUVendor Vendor
        {
            get => this._vendor;
        }

        public int MaxSaturation
        {
            get => _maxSaturation;
        }

        public int MinSaturation
        {
            get => _minSaturation;
        }

        public int InitSaturation
        {
            get => _initSaturation;
        }

        public int Saturation
        {
            get => currentSaturation;
            set
            {
                if (value > this.MaxSaturation)
                    value = this.MaxSaturation;
                if (value < this.MinSaturation)
                    value = this.MinSaturation;

                // Load()에 실패했다면(해당 모니터가 NVIDIA에 연결되지 않았거나 DVC 미지원) 조용히 무시
                if (!_loaded)
                    return;

                try
                {
                    DisplayApi.SetDVCLevel(displayHandle, value);
                    this.currentSaturation = value;
                }
                catch (Exception ex)
                {
                    Logger.Error("SetDVCLevel 실패", ex);
                }
            }
        }

        public NVIDIA(GPUVendor vendor)
        {
            try
            { 
                NvAPIWrapper.NVIDIA.Initialize();
            }
            catch (NvAPIWrapper.Native.Exceptions.NVIDIAApiException)
            {
                MessageBox.Show("NvAPI Intialize Failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this._vendor = vendor;
        }

        public void ResetSaturation()
        {
            this.Saturation = this.InitSaturation;
        }

        public void Load(string display) {
            _loaded = false;
            displayHandle = DisplayApi.GetAssociatedNvidiaDisplayHandle(display);
            PrivateDisplayDVCInfo dvcInfo = DisplayApi.GetDVCInfo(displayHandle);
            this._maxSaturation = dvcInfo.MaximumLevel;
            this._minSaturation = dvcInfo.MinimumLevel;
            this._initSaturation = this.currentSaturation = dvcInfo.CurrentLevel;
            _loaded = true;
        }

        public void Close() {
            try
            {
                NvAPIWrapper.NVIDIA.Unload();
            }
            catch (NvAPIWrapper.Native.Exceptions.NVIDIAApiException)
            {
                MessageBox.Show("NvAPI Unload Failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
