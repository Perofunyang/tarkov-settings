using System;
using System.Collections.Generic;

namespace tarkov_settings.Setting
{
    public class Profile
    {
        public string Name { get; set; } = "Default";
        public bool IsEnabled { get; set; } = true;
        public List<string> TargetProcesses { get; set; } = new List<string>();

        // [방안 A 반영] 기본값 50.0 (0 ~ 100)
        public double Brightness { get; set; } = 50.0;
        public double Contrast { get; set; } = 50.0;
        public double Gamma { get; set; } = 1.00;
        public int Saturation { get; set; } = 0;
        public double BlackStabilizer { get; set; } = 0;
        public double WhiteStabilizer { get; set; } = 0;
        public bool IsDynamicAdaptive { get; set; } = false;

        public double Dehaze { get; set; } = 0;
        public double FoliagePop { get; set; } = 0;

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
                FoliagePop = this.FoliagePop
            };
        }
    }
}