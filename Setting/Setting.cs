using System;
using System.IO;
using Newtonsoft.Json;

namespace tarkov_settings.Setting
{
    internal class Settings<T> where T : new()
    {
        private const string DEFAULT_FILENAME = "settings.json";

        // [핵심] 쓰기 권한을 확인하여 실행 폴더 또는 AppData 경로를 반환하는 메서드
        private static string GetWritableFilePath(string fileName)
        {
            string localDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string localFilePath = Path.Combine(localDirectory, fileName);

            try
            {
                // 1. 현재 실행 폴더에 쓰기 권한이 있는지 임시 테스트
                string testFilePath = Path.Combine(localDirectory, ".write_test");
                File.WriteAllText(testFilePath, "test");
                File.Delete(testFilePath);

                // 쓰기가 성공하면 프로그램 바로 옆에 저장 (포터블 모드 - 찌꺼기 안 남음)
                return localFilePath;
            }
            catch (UnauthorizedAccessException)
            {
                // 2. C:\Program Files 등 쓰기 권한이 없는 곳이면 AppData로 안전하게 우회
                string appDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TarkovSettings");

                if (!Directory.Exists(appDataDir))
                {
                    Directory.CreateDirectory(appDataDir);
                }

                return Path.Combine(appDataDir, fileName);
            }
            catch
            {
                // 기타 예외 발생 시 기본 실행 경로 반환
                return localFilePath;
            }
        }

        public void Save(string fileName = DEFAULT_FILENAME)
        {
            try
            {
                string filePath = GetWritableFilePath(fileName);
                File.WriteAllText(filePath, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Settings Save Error] {ex.Message}");
            }
        }

        public static T Load(string fileName = DEFAULT_FILENAME)
        {
            T t = new T();
            try
            {
                string filePath = GetWritableFilePath(fileName);
                if (File.Exists(filePath))
                {
                    t = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Settings Load Error] {ex.Message}");
            }
            return t;
        }
    }
}