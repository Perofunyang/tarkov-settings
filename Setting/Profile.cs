using System;
using System.Collections.Generic;

namespace tarkov_settings.Setting
{
    public class Profile
    {
        public string Name { get; set; } = "Default";
        public bool IsEnabled { get; set; } = true;
        public List<string> TargetProcesses { get; set; } = new List<string>();

        public double Brightness { get; set; } = 45.0;
        public double Contrast { get; set; } = 50.0;
        public double Gamma { get; set; } = 1.70;
        public int Saturation { get; set; } = 40;
        public double BlackStabilizer { get; set; } = 30.0;
        public double WhiteStabilizer { get; set; } = 25.0;
        public bool IsDynamicAdaptive { get; set; } = false;

        public double Dehaze { get; set; } = 20.0;
        public double FoliagePop { get; set; } = 25.0;

        // [신규 필드] 엔비디아 드라이버 레벨 선명도 (0 ~ 100)
        public int Sharpening { get; set; } = 50;

        public Profile Clone(string newName = null)
        {
            return new Profile
            {
                Name = newName ?? (this.Name + " (Copy)"),
                IsEnabled = this.IsEnabled,
                TargetProcesses = new List<string>(this.TargetProcesses),
                Brightness = this.Brightness,
                Contrast = this.Contrast,
                Gamma = this.Gamma,
                Saturation = this.Saturation,
                BlackStabilizer = this.BlackStabilizer,
                WhiteStabilizer = this.WhiteStabilizer,
                IsDynamicAdaptive = this.IsDynamicAdaptive,
                Dehaze = this.Dehaze,
                FoliagePop = this.FoliagePop,
                Sharpening = this.Sharpening
            };
        }
    }
}