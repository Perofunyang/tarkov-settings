using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tarkov_settings.Setting
{
    class AppSetting : Settings<AppSetting>
    {
        public double brightness = 0.5;
        public double contrast = 0.5;
        public double gamma = 1.0;
        public int saturation = 0;

        // [추가] 블랙 스태빌라이저 수치 및 동적 적응형 모드 설정
        public double blackStabilizer = 0;
        public bool isDynamicAdaptive = false;

        // [추가] 화이트 스태빌라이저
        public double whiteStabilizer = 0;

        public HashSet<string> pTargets = new HashSet<string>{
            "EscapeFromTarkov"
        };
        public string display = @"\\.\DISPLAY1";
        public bool minimizeOnStart = false;
    }
}
