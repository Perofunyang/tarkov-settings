using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using tarkov_settings.Setting;

namespace tarkov_settings
{
    static class NativeMethods
    {
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        public static WinEventDelegate dele = null;
        private static IntPtr m_hhook;

        public static void SetHook()
        {
            m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND,
                EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                dele,
                0, 0, WINEVENT_OUTOFCONTEXT | 2);
        }

        public static void UnHook()
        {
            if (m_hhook != IntPtr.Zero)
            {
                UnhookWinEvent(m_hhook);
                m_hhook = IntPtr.Zero;
            }
        }

        #region Win32 API Calls
        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
        #endregion

        public static string GetActiveWindowTitle()
        {
            try
            {
                IntPtr handle = GetForegroundWindow();
                if (handle == IntPtr.Zero) return null;

                GetWindowThreadProcessId(handle, out uint processID);
                if (processID == 0) return null;

                IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processID);
                if (hProcess != IntPtr.Zero)
                {
                    try
                    {
                        int capacity = 1024;
                        StringBuilder sb = new StringBuilder(capacity);
                        if (QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
                        {
                            string fullPath = sb.ToString();
                            return Path.GetFileNameWithoutExtension(fullPath);
                        }
                    }
                    finally
                    {
                        CloseHandle(hProcess);
                    }
                }

                try
                {
                    using (var proc = Process.GetProcessById(Convert.ToInt32(processID)))
                    {
                        return proc.ProcessName;
                    }
                }
                catch
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }

    class ProcessMonitor
    {
        private NativeMethods.WinEventDelegate processHook;
        private readonly ColorController cController = ColorController.Instance;

        private bool _isTargetFocused = false;
        private string _currentFocusedProfileName = null; // 현재 적용 중인 프로필 이름 추적
        private System.Threading.Timer _pollTimer;

        #region Singleton Pattern implement
        private static readonly Lazy<ProcessMonitor> instance =
            new Lazy<ProcessMonitor>(() => new ProcessMonitor());

        public static ProcessMonitor Instance => instance.Value;
        #endregion

        public bool IsTargetFocused => _isTargetFocused;
        public MainForm Parent { get; set; }

        private ProcessMonitor() { }

        public void Init()
        {
            processHook = new NativeMethods.WinEventDelegate(WinEventProc);
            NativeMethods.dele += processHook;
            NativeMethods.SetHook();

            cController.Init();

            // 0.5초 주기 안전 폴링 타이머
            _pollTimer = new System.Threading.Timer((state) =>
            {
                WinEventProc(IntPtr.Zero, 0, IntPtr.Zero, 0, 0, 0, 0);
            }, null, 500, 500);

            // 시작 시점 즉시 1회 검사
            OnFocusChangedInternal();
        }

        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (Parent == null || Parent.IsDisposed || !Parent.IsHandleCreated)
                return;

            if (Parent.InvokeRequired)
            {
                try
                {
                    Parent.BeginInvoke(new Action(() => OnFocusChangedInternal()));
                }
                catch (ObjectDisposedException) { }
            }
            else
            {
                OnFocusChangedInternal();
            }
        }

        private void OnFocusChangedInternal()
        {
            if (Parent == null || Parent.IsDisposed) return;

            string activeTitle = NativeMethods.GetActiveWindowTitle();

            // [핵심] 현재 활성화된 창과 일치하는 프로필 자동 검색
            Profile matchedProfile = Parent.GetMatchingProfile(activeTitle);

            if (matchedProfile != null && Parent.IsEnabled)
            {
                // 게임 창이 새로 포커스되었거나, 다른 게임으로 바로 전환된 경우
                if (!_isTargetFocused || _currentFocusedProfileName != matchedProfile.Name)
                {
                    Console.WriteLine($"[pMonitor] Target Process IS focused: {activeTitle} (Profile: {matchedProfile.Name})");
                    _isTargetFocused = true;
                    _currentFocusedProfileName = matchedProfile.Name;

                    // 매칭된 프로필의 수치를 ColorController에 즉시 주입 및 적용
                    cController.Brightness = matchedProfile.Brightness;
                    cController.Contrast = matchedProfile.Contrast;
                    cController.Gamma = matchedProfile.Gamma;
                    cController.BlackStabilizer = matchedProfile.BlackStabilizer;
                    cController.WhiteStabilizer = matchedProfile.WhiteStabilizer;
                    cController.IsDynamicAdaptive = matchedProfile.IsDynamicAdaptive;

                    cController.ApplyColorSettings(reset: false);
                    cController.DVL = matchedProfile.Saturation;
                }
            }
            else
            {
                // 게임이 아닌 창(바탕화면, 브라우저 등)일 때
                if (_isTargetFocused)
                {
                    Console.WriteLine("[pMonitor] Target Process is NOT focused");
                    _isTargetFocused = false;
                    _currentFocusedProfileName = null;

                    cController.ChangeColorRamp(reset: true);
                    cController.ResetDVL();
                }
            }
        }

        public void Close()
        {
            _pollTimer?.Dispose();

            NativeMethods.dele -= processHook;
            NativeMethods.UnHook();

            _isTargetFocused = false;
            _currentFocusedProfileName = null;

            cController.Close();
        }
    }
}