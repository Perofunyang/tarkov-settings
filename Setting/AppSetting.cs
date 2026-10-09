using System;
using System.Collections.Generic;
using System.Linq;

namespace tarkov_settings.Setting
{
    class AppSetting : Settings<AppSetting>
    {
        public string SelectedProfile { get; set; } = "Escape from Tarkov";
        public List<Profile> Profiles { get; set; } = new List<Profile>();
        public string display = @"\\.\DISPLAY1";
        public bool minimizeOnStart = false;

        /// <summary>
        /// 사용자 지정 최신 기본 추천 프로필 생성
        /// </summary>
        public static AppSetting CreateDefault()
        {
            var setting = new AppSetting();
            setting.Profiles = new List<Profile>
            {
                new Profile
                {
                    Name = "Escape from Tarkov",
                    IsEnabled = true,
                    TargetProcesses = new List<string> { "EscapeFromTarkov", "EscapeFromTarkovArena" },
                    Brightness = 45.0,
                    Contrast = 55.0,
                    Gamma = 2.30,
                    Saturation = 30,
                    BlackStabilizer = 25.0,
                    WhiteStabilizer = 15.0,
                    IsDynamicAdaptive = false,
                    Dehaze = 20.0,
                    FoliagePop = 25.0,
                    Sharpening = 50
                }
            };
            setting.SelectedProfile = "Escape from Tarkov";
            return setting;
        }

        public new static AppSetting Load(string fileName = "settings.json")
        {
            var setting = Settings<AppSetting>.Load(fileName);

            if (setting.Profiles == null || setting.Profiles.Count == 0)
            {
                setting = CreateDefault();
            }

            bool migrated = false;
            foreach (var p in setting.Profiles)
            {
                if (p.Brightness > 0 && p.Brightness <= 1.0)
                {
                    p.Brightness = Math.Round(p.Brightness * 100.0);
                    migrated = true;
                }
                if (p.Contrast > 0 && p.Contrast <= 1.0)
                {
                    p.Contrast = Math.Round(p.Contrast * 100.0);
                    migrated = true;
                }
            }

            if (migrated)
            {
                setting.Save();
            }

            if (string.IsNullOrEmpty(setting.SelectedProfile) || !setting.Profiles.Any(p => p.Name == setting.SelectedProfile))
            {
                setting.SelectedProfile = setting.Profiles[0].Name;
            }

            return setting;
        }
    }
}