using System;
using System.Collections.Generic;
using System.Linq;

namespace tarkov_settings.Setting
{
    class AppSetting : Settings<AppSetting>
    {
        /// <summary>설정 파일 스키마 버전. 0 = 버전 필드 도입 이전(구버전) 파일.</summary>
        public const int CurrentVersion = 1;
        public int SettingsVersion { get; set; } = 0;

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
            setting.SettingsVersion = CurrentVersion;
            setting.Profiles = new List<Profile>
            {
                new Profile
                {
                    Name = "Escape from Tarkov",
                    IsEnabled = true,
                    TargetProcesses = new List<string> { "EscapeFromTarkov", "EscapeFromTarkovArena" },
                    Brightness = 50.0,
                    Contrast = 55.0,
                    Gamma = 2.50,
                    Saturation = 35,
                    BlackStabilizer = 50.0,
                    WhiteStabilizer = 10.0,
                    IsDynamicAdaptive = false,
                    Dehaze = 85.0,
                    FoliagePop = 30.0,
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
                if (p.TargetProcesses == null)
                {
                    p.TargetProcesses = new List<string>();
                    migrated = true;
                }
            }

            // v0 -> v1: 밝기/대비가 0~1 스케일이던 구버전 값을 0~100 으로 변환 (딱 한 번만 수행)
            // 버전 필드 없이 값만으로 판단하면, 새 스케일의 정상 값 1(=1%)이 재시작 때마다 100으로 바뀌는 문제가 있었음
            if (setting.SettingsVersion < 1)
            {
                foreach (var p in setting.Profiles)
                {
                    if (p.Brightness > 0 && p.Brightness <= 1.0)
                        p.Brightness = Math.Round(p.Brightness * 100.0);
                    if (p.Contrast > 0 && p.Contrast <= 1.0)
                        p.Contrast = Math.Round(p.Contrast * 100.0);
                }
                setting.SettingsVersion = CurrentVersion;
                migrated = true;
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