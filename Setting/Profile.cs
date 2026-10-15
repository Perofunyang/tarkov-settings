using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace tarkov_settings.Setting
{
    public class Profile
    {
        public string Name { get; set; } = "Default";
        public bool IsEnabled { get; set; } = true;
        public List<string> TargetProcesses { get; set; } = new List<string>();
        public double Brightness { get; set; } = 50.0;
        public double Contrast { get; set; } = 50.0;
        public double Gamma { get; set; } = 1.00;
        public int Saturation { get; set; } = 0;
        public double BlackStabilizer { get; set; } = 0.0;
        public double WhiteStabilizer { get; set; } = 0.0;
        public bool IsDynamicAdaptive { get; set; } = false;

        public double Dehaze { get; set; } = 0.0;
        public double FoliagePop { get; set; } = 0.0;
        public int Sharpening { get; set; } = 0;

        /// <summary>
        /// 프로세스 이름에서 공백과 (대소문자 무관) .exe 확장자를 제거합니다.
        /// </summary>
        public static string NormalizeProcessName(string name)
        {
            name = (name ?? string.Empty).Trim();
            return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? name.Substring(0, name.Length - 4).TrimEnd()
                : name;
        }

        /// <summary>
        /// 이 프로필이 활성화되어 있고, 주어진 프로세스를 대상으로 하는지 확인합니다.
        /// </summary>
        public bool MatchesProcess(string processName)
        {
            if (!IsEnabled || TargetProcesses == null || string.IsNullOrWhiteSpace(processName))
                return false;

            string active = NormalizeProcessName(processName);
            return TargetProcesses.Any(t =>
                string.Equals(NormalizeProcessName(t), active, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// NVIDIA 드라이버 샤프닝(DRS)은 타르코프 전용 프로필에만 적용되므로, 대상 판별에 사용합니다.
        /// </summary>
        [JsonIgnore]
        public bool IsTarkovProfile =>
            TargetProcesses != null &&
            TargetProcesses.Any(t => NormalizeProcessName(t).StartsWith("EscapeFromTarkov", StringComparison.OrdinalIgnoreCase));

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