using System;
using System.IO;
using tarkov_settings.Setting;

namespace tarkov_settings
{
    /// <summary>
    /// WinExe에서는 Console 출력이 보이지 않으므로, 문제 추적용 간단한 파일 로거를 제공합니다.
    /// 로그 쓰기 실패가 앱 동작에 영향을 주지 않도록 모든 예외를 삼킵니다.
    /// </summary>
    internal static class Logger
    {
        private const string FileName = "tarkov-settings.log";
        private const long MaxBytes = 512 * 1024;
        private static readonly object _lock = new object();

        public static void Info(string message) => Write("INFO ", message);

        public static void Error(string message, Exception ex = null)
            => Write("ERROR", ex == null ? message : message + " :: " + ex);

        private static void Write(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    string path = AppPaths.GetPath(FileName);

                    var fi = new FileInfo(path);
                    if (fi.Exists && fi.Length > MaxBytes)
                    {
                        string old = path + ".old";
                        if (File.Exists(old)) File.Delete(old);
                        File.Move(path, old);
                    }

                    File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
                }
            }
            catch
            {
                // 로깅 실패는 무시
            }
        }
    }
}
