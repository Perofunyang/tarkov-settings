using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tarkov_settings.GPU
{
    class AMD : IGPU
    {
        private GPUVendor _vendor;
        public GPUVendor Vendor
        {
            get => this._vendor;
        }

        // AMD 채도는 아직 미지원: 호출되어도 예외 없이 무시합니다.
        public int Saturation { get; set; }

        public int MaxSaturation => 0;
        public int MinSaturation => 0;
        public int InitSaturation => 0;

        public AMD(GPUVendor vendor)
        {
            this._vendor = vendor;
        }

        public void ResetSaturation() { }

        public void Load(string display) { }

        public void Close() { }
    }
}
