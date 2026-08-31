using System;
using System.Collections.Generic;

namespace tarkov_settings.Setting
{
    public class Profile
    {
        // 프로필 이름 및 활성화 여부
        public string Name { get; set; } = "Default";
        public bool IsEnabled { get; set; } = true;

        // 이 프로필이 작동할 게임 실행 파일 목록 (예: ["EscapeFromTarkov", "Expedition Into Darkness"])
        public List<string> TargetProcesses { get; set; } = new List<string>();

        // 해당 게임 전용 디스플레이 및 스태빌라이저 수치
        public double Brightness { get; set; } = 0.50;
        public double Contrast { get; set; } = 0.50;
        public double Gamma { get; set; } = 1.00;
        public int Saturation { get; set; } = 0;
        public double BlackStabilizer { get; set; } = 0;
        public double WhiteStabilizer { get; set; } = 0;
        public bool IsDynamicAdaptive { get; set; } = false;

        /// <summary>
        /// 프로필 복제 메서드
        /// </summary>
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
                IsDynamicAdaptive = this.IsDynamicAdaptive
            };
        }
    }
}