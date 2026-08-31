using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

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

        /// <summary>
        /// QueryFullProcessImageName API를 사용하여 권한 오류(Access Denied) 없이 모든 게임의 정확한 실행 파일명을 읽어옵니다.
        /// </summary>
        public static string GetActiveWindowTitle()
        {
            try
            {
                IntPtr handle = GetForegroundWindow();
                if (handle == IntPtr.Zero) return null;

                GetWindowThreadProcessId(handle, out uint processID);
                if (processID == 0) return null;

                // [핵심] PROCESS_QUERY_LIMITED_INFORMATION 플래그로 권한 문제 완벽 해결
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
                            // "C:\...\Expedition Into Darkness.exe" -> "Expedition Into Darkness"
                            return Path.GetFileNameWithoutExtension(fullPath);
                        }
                    }
                    finally
                    {
                        CloseHandle(hProcess);
                    }
                }

                // 백업용 C# 레거시 방식
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
        private HashSet<string> pTargets = new HashSet<string>();

        private bool _isTargetFocused = false;
        private System.Threading.Timer _pollTimer;

        #region Singleton Pattern implement
        private static readonly Lazy<ProcessMonitor> instance =
            new Lazy<ProcessMonitor>(() => new ProcessMonitor());

        public static ProcessMonitor Instance => instance.Value;
        #endregion

        public bool IsTargetFocused => _isTargetFocused;
        public MainForm Parent { get; set; }

        private ProcessMonitor() { }

        public void Add(string process)
        {
            if (!string.IsNullOrEmpty(process))
            {
                this.pTargets.Add(process.ToLower());
            }
        }

        public void Init()
        {
            processHook = new NativeMethods.WinEventDelegate(WinEventProc);
            NativeMethods.dele += processHook;
            NativeMethods.SetHook();

            cController.Init();

            // [추가] 이벤트 누락 방지를 위한 0.5초 주기 안전 폴링 타이머
            _pollTimer = new System.Threading.Timer((state) =>
            {
                WinEventProc(IntPtr.Zero, 0, IntPtr.Zero, 0, 0, 0, 0);
            }, null, 500, 500);

            // 시작 시점에 현재 켜진 활성 창 즉시 1회 검사
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
            bool isTargetNow = !string.IsNullOrEmpty(activeTitle) &&
                               this.pTargets.Contains(activeTitle.ToLower()) &&
                               Parent.IsEnabled;

            if (isTargetNow)
            {
                if (!_isTargetFocused)
                {
                    Console.WriteLine("[pMonitor] Target Process IS focused : " + activeTitle);
                    _isTargetFocused = true;

                    var (b, c, g, dvl) = Parent.GetColorValue();

                    cController.BlackStabilizer = Parent.BlackStabilizer;
                    cController.WhiteStabilizer = Parent.WhiteStabilizer;
                    cController.IsDynamicAdaptive = Parent.IsDynamicAdaptive;

                    cController.ChangeColorRamp(brightness: b,
                                                contrast: c,
                                                gamma: g,
                                                reset: false);
                    cController.DVL = dvl;
                }
            }
            else
            {
                if (_isTargetFocused)
                {
                    Console.WriteLine("[pMonitor] Target Process is NOT focused");
                    _isTargetFocused = false;

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

            cController.Close();
        }
    }
}