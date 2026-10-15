using System;
using System.IO;
using Newtonsoft.Json;

namespace tarkov_settings.Setting
{
    internal class Settings<T> where T : new()
    {
        private const string DEFAULT_FILENAME = "settings.json";
        private static readonly object _ioLock = new object();

        /// <summary>
        /// 임시 파일에 먼저 쓴 뒤 교체하는 방식으로 저장합니다.
        /// 저장 도중 크래시/정전이 나도 기존 settings.json 이 깨지지 않으며, 직전 버전은 .bak 으로 남습니다.
        /// </summary>
        public void Save(string fileName = DEFAULT_FILENAME)
        {
            lock (_ioLock)
            {
                try
                {
                    string filePath = AppPaths.GetPath(fileName);
                    string tmpPath = filePath + ".tmp";
                    string bakPath = filePath + ".bak";

                    File.WriteAllText(tmpPath, JsonConvert.SerializeObject(this, Formatting.Indented));

                    if (File.Exists(filePath))
                        File.Replace(tmpPath, filePath, bakPath);
                    else
                        File.Move(tmpPath, filePath);
                }
                catch (Exception ex)
                {
                    Logger.Error("설정 저장 실패", ex);
                }
            }
        }

        /// <summary>
        /// settings.json 을 읽고, 손상되었으면 손상 파일을 보존한 뒤 .bak 으로 복구를 시도합니다.
        /// 둘 다 실패하면 새 인스턴스(기본값)를 반환합니다.
        /// </summary>
        public static T Load(string fileName = DEFAULT_FILENAME)
        {
            lock (_ioLock)
            {
                string filePath = AppPaths.GetPath(fileName);

                foreach (string candidate in new[] { filePath, filePath + ".bak" })
                {
                    if (!File.Exists(candidate)) continue;

                    try
                    {
                        T loaded = JsonConvert.DeserializeObject<T>(File.ReadAllText(candidate));
                        if (loaded != null)
                        {
                            if (candidate != filePath)
                                Logger.Info("settings.json 이 손상되어 백업(.bak)에서 복구했습니다.");
                            return loaded;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("설정 로드 실패: " + candidate, ex);
                        if (candidate == filePath)
                            PreserveCorruptFile(candidate);
                    }
                }

                return new T();
            }
        }

        private static void PreserveCorruptFile(string path)
        {
            try
            {
                string dest = path + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                File.Copy(path, dest, true);
                Logger.Info("손상된 설정 파일을 보존했습니다: " + dest);
            }
            catch
            {
                // 보존 실패는 무시
            }
        }
    }
}
