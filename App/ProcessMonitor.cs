using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace tarkov_settings
{
    static class NativeMethods
    {
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;

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
        #endregion

        // [개선 1] using 문을 사용하여 Process 객체 리소스를 사용 즉시 해제 (메모리 누수 및 프레임 드랍 방지)
        public static string GetActiveWindowTitle()
        {
            try
            {
                IntPtr handle = GetForegroundWindow();
                if (handle == IntPtr.Zero) return null;

                GetWindowThreadProcessId(handle, out uint processID);
                if (processID == 0) return null;

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
    }

    class ProcessMonitor
    {
        private NativeMethods.WinEventDelegate processHook;

        private readonly ColorController cController = ColorController.Instance;

        private HashSet<string> pTargets = new HashSet<string>();

        // [개선 2] 포커스 상태 추적 변수 (불필요한 중복 GDI/GPU 호출 방지)
        private bool _isTargetFocused = false;
        // [추가] 현재 타르코프 게임이 활성화되어 있는지 여부를 외부에서 확인하는 프로퍼티
        public bool IsTargetFocused => _isTargetFocused;

        #region Singleton Pattern implement
        private static readonly Lazy<ProcessMonitor> instance =
            new Lazy<ProcessMonitor>(() => new ProcessMonitor());

        public static ProcessMonitor Instance => instance.Value;
        #endregion

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

            // Init ColorController
            cController.Init();
        }

        /**
         * Window Focus changed Event Handler
         */
        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // [개선 3] 폼이 파괴되었거나(Disposed) 닫히는 중이면 작업 안함 (튕김 방지)
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

        // [개선 4] 상태가 전환될 때만 1회 호출되도록 상태 제어 로직 적용
        private void OnFocusChangedInternal()
        {
            if (Parent == null || Parent.IsDisposed) return;

            string activeTitle = NativeMethods.GetActiveWindowTitle();
            bool isTargetNow = !string.IsNullOrEmpty(activeTitle) &&
                               this.pTargets.Contains(activeTitle.ToLower()) &&
                               Parent.IsEnabled;

            // 1. 게임 창으로 '들어왔을 때' (False -> True 일 때만 1회 실행)
            if (isTargetNow)
            {
                if (!_isTargetFocused)
                {
                    Console.WriteLine("[pMonitor] Target Process IS focused");
                    _isTargetFocused = true;

                    var (b, c, g, dvl) = Parent.GetColorValue();

                    // [추가] 블랙 스태빌라이저 및 동적 적응형 수치 동기화
                    cController.BlackStabilizer = Parent.BlackStabilizer;
                    cController.WhiteStabilizer = Parent.WhiteStabilizer;
                    cController.IsDynamicAdaptive = Parent.IsDynamicAdaptive;

                    cController.ChangeColorRamp(brightness: b, contrast: c, gamma: g, reset: false);
                    cController.DVL = dvl;
                }
            }
            // 2. 게임 창에서 '나갔을 때' (True -> False 일 때만 1회 실행)
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

        /**
         * Reset to original color settings before exit
         */
        public void Close()
        {
            Console.WriteLine("[pMonitor] Remove Delegates");
            NativeMethods.dele -= processHook;
            NativeMethods.UnHook();

            _isTargetFocused = false;

            Console.WriteLine("[pMonitor] Resetting Color");
            cController.Close();
        }

        private static int GetWorkingThreads()
        {
            System.Threading.ThreadPool.GetMaxThreads(out int maxThreads, out int _);
            System.Threading.ThreadPool.GetAvailableThreads(out int availableThreads, out _);
            return maxThreads - availableThreads;
        }
    }
}