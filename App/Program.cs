using System;
using System.Threading;
using System.Windows.Forms;

using tarkov_settings.GPU;

namespace tarkov_settings
{
    static class Program
    {
        private static MainForm mForm;
        private static Mutex _singleInstanceMutex;

        [STAThread]
        static void Main()
        {
            // 처리되지 않은 UI 예외는 대화상자 대신 로그로 남기고 계속 실행합니다.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => Logger.Error("UI ThreadException", e.Exception);

            // 치명적 예외/프로세스 종료 시에도 디스플레이 색상이 변조된 채 남지 않도록 복구합니다.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Logger.Error("UnhandledException", e.ExceptionObject as Exception);
                ColorController.EmergencyReset();
            };
            AppDomain.CurrentDomain.ProcessExit += (s, e) => ColorController.EmergencyReset();

            // 단일 인스턴스: 두 번째 인스턴스가 이미 변조된 감마 램프를 "원본"으로 캡처하는 문제 방지
            bool createdNew;
            _singleInstanceMutex = new Mutex(true, @"Local\tarkov-settings.single-instance", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show(
                    "Tarkov Settings가 이미 실행 중입니다.\n시스템 트레이 아이콘을 확인하세요.",
                    "Tarkov Settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                IGPU gpu = GPUDevice.Instance;
                Logger.Info($"시작: GPU={gpu.Vendor}");

                if (gpu.Vendor == GPUVendor.AMD)
                {
                    /* AMD Saturation (equals to Digital Vibrance of Nvidia) is not supported yet. */
                    MessageBox.Show(
                        "AMD Device Detected - Saturation is not supported yet.",
                        "Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                // Open Main Form
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                mForm = new MainForm();
                Application.Run(mForm);

                // Unload NvAPI dll after Application.Exit()
                try
                {
                    gpu.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error("GPU Close 실패", ex);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Main 예외", ex);
                ColorController.EmergencyReset();
                MessageBox.Show(
                    "치명적인 오류가 발생했습니다. 자세한 내용은 tarkov-settings.log 를 확인하세요.\n\n" + ex.Message,
                    "Tarkov Settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                try { _singleInstanceMutex.ReleaseMutex(); } catch { }
                _singleInstanceMutex.Dispose();
            }
        }
    }
}
