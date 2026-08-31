using System;
using System.Collections.Generic;
using System.Linq;

namespace tarkov_settings.Setting
{
    class AppSetting : Settings<AppSetting>
    {
        // 현재 UI에서 선택/편집 중인 프로필 이름
        public string SelectedProfile { get; set; } = "Escape from Tarkov";

        // 등록된 다중 프로필 목록
        public List<Profile> Profiles { get; set; } = new List<Profile>();

        public string display = @"\\.\DISPLAY1";
        public bool minimizeOnStart = false;

        /// <summary>
        /// 프로그램 최초 실행 시 기본 프로필 (Escape from Tarkov 전용 1개)
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
                    TargetProcesses = new List<string> { "EscapeFromTarkov" },
                    Brightness = 0.40,
                    Contrast = 0.45,
                    Gamma = 1.7,
                    Saturation = 25,
                    BlackStabilizer = 40,
                    WhiteStabilizer = 35,
                    IsDynamicAdaptive = false
                }
            };
            setting.SelectedProfile = "Escape from Tarkov";
            return setting;
        }

        /// <summary>
        /// settings.json 파일을 읽고, 구버전 파일이거나 비어있으면 안전하게 기본 프로필로 생성
        /// </summary>
        public new static AppSetting Load(string fileName = "settings.json")
        {
            var setting = Settings<AppSetting>.Load(fileName);

            if (setting.Profiles == null || setting.Profiles.Count == 0)
            {
                setting = CreateDefault();
            }

            if (string.IsNullOrEmpty(setting.SelectedProfile) || !setting.Profiles.Any(p => p.Name == setting.SelectedProfile))
            {
                setting.SelectedProfile = setting.Profiles[0].Name;
            }

            return setting;
        }
    }
}