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
                    Contrast = 50.0,
                    Gamma = 1.70,
                    Saturation = 40,
                    BlackStabilizer = 30,
                    WhiteStabilizer = 25,
                    IsDynamicAdaptive = false,
                    Dehaze = 20,
                    FoliagePop = 25
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

            // [구버전 호환 마이그레이션] 기존 0.0 ~ 1.0 소수점을 0 ~ 100 정수로 안전 변환
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

            // 구버전 파일이었던 경우, 새 규격(40, 50, Dehaze:0 등)으로 디스크에 즉시 덮어써서 영구 업그레이드
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