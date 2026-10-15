using System;
using System.Net.Http;
using System.Windows.Forms;

namespace tarkov_settings
{
    public partial class UpdateNotifier : Form
    {
        private Version current, latest;

        // Utilize repository's file as version notifier
        // I know it sounds very dangerous. but i am broke as hell.
        private string downloadUrl = @"https://github.com/Perofunyang/tarkov-settings/releases/latest";
        private string checkUrl = @"https://raw.githubusercontent.com/Perofunyang/tarkov-settings/main/version";
        public UpdateNotifier(Version current)
        {
            InitializeComponent();
            this.current = current;
            this.CurrentVersionLabel.Text = current.ToString();
            CheckUpdate();
        }
        private async void CheckUpdate()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // [수정 1] 네트워크 지연으로 인한 앱 지연을 방지하기 위해 4초 타임아웃 지정
                    client.Timeout = TimeSpan.FromSeconds(4);

                    HttpResponseMessage response = await client.GetAsync(checkUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        this.Dispose();
                        return;
                    }

                    string versionStr = (await response.Content.ReadAsStringAsync())?.Trim();

                    // [수정 2] 안전한 버전 파싱
                    if (Version.TryParse(versionStr, out Version latestVersion))
                    {
                        latest = latestVersion;
                        this.LatestVersionLabel.Text = latest.ToString();

                        // 새 버전이 있을 때만 업데이트 알림 모달 창 표시
                        if (latest > current && !this.IsDisposed)
                        {
                            this.ShowDialog();
                        }
                        else
                        {
                            this.Dispose();
                        }
                    }
                    else
                    {
                        this.Dispose();
                    }
                }
            }
            catch
            {
                // [수정 3] 인터넷 연결 실패, 방화벽 차단 등 예외 발생 시 에러창을 띄우지 않고 조용히 종료
                try { this.Dispose(); } catch { }
            }
        }
        
        private void UpdateCancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(downloadUrl);
            Application.Exit();
        }
    }
}
