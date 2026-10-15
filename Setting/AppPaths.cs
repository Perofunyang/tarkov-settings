using System;
using System.IO;

namespace tarkov_settings.Setting
{
    /// <summary>
    /// 설정/로그 파일을 저장할 폴더를 한 번만 결정합니다.
    /// 실행 폴더에 쓸 수 있으면 포터블로 그곳을, 아니면 %APPDATA%\TarkovSettings 를 사용합니다.
    /// </summary>
    internal static class AppPaths
    {
        private static readonly Lazy<string> _dataDirectory = new Lazy<string>(Resolve);

        public static string DataDirectory => _dataDirectory.Value;

        public static string GetPath(string fileName) => Path.Combine(DataDirectory, fileName);

        private static string Resolve()
        {
            string localDir = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                string probe = Path.Combine(localDir, ".write_test");
                File.WriteAllText(probe, "test");
                File.Delete(probe);
                return localDir;
            }
            catch
            {
                // Program Files 등 쓰기 불가 폴더이거나 읽기 전용 매체: AppData로 대체
                try
                {
                    string appData = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "TarkovSettings");
                    Directory.CreateDirectory(appData);
                    return appData;
                }
                catch
                {
                    return localDir;
                }
            }
        }
    }
}
