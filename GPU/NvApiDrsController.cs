using System;
using System.Runtime.InteropServices;

namespace tarkov_settings.GPU
{
    public static class NvApiDrsController
    {
        // [추가] 멀티스레드 동시 접근 충돌(파일 락 에러)을 방지하는 락 객체
        private static readonly object _drsLock = new object();

        // 마지막으로 "성공적으로" 적용한 샤프닝 값. 같은 값이면 드라이버 파일 쓰기를 건너뜁니다.
        private static int? _lastAppliedPercent = null;

        #region NvAPI DRS Native P/Invoke
        [DllImport("nvapi.dll", EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NvAPI32_QueryInterface(uint id);

        [DllImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NvAPI64_QueryInterface(uint id);

        private static IntPtr QueryInterface(uint id)
        {
            try { return IntPtr.Size == 8 ? NvAPI64_QueryInterface(id) : NvAPI32_QueryInterface(id); }
            catch { return IntPtr.Zero; }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_Initialize_t();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_CreateSession_t(out IntPtr phSession);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_DestroySession_t(IntPtr hSession);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_LoadSettings_t(IntPtr hSession);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_SaveSettings_t(IntPtr hSession);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_GetBaseProfile_t(IntPtr hSession, out IntPtr phProfile);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_FindProfileByName_t(IntPtr hSession, ref NVAPI_UNICODE_STRING pProfileName, out IntPtr phProfile);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_SetSetting_t(IntPtr hSession, IntPtr hProfile, ref NVDRS_SETTING pSetting);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_DeleteProfileSetting_t(IntPtr hSession, IntPtr hProfile, uint settingId);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NVAPI_UNICODE_STRING
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2048)]
            public string Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private struct NVDRS_SETTING
        {
            public uint version;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2048)]
            public string settingName;
            public uint settingId;
            public uint settingType;
            public uint settingLocation;
            public uint isCurrentPredefined;
            public uint isPredefinedValid;
            public uint u32PredefinedValue;
            public uint u32CurrentValue;
        }

        private const uint DRS_SETTING_VER = 0x00010000 | 2072;
        private const uint SHARPEN_FILTER_EN = 0x00598928;
        private const uint SHARPEN_FILTER_VALUE = 0x002ED8CD;
        private const uint SHARPEN_FILTER_DENOISE = 0x002ED8CE;
        private const uint NIS_ENABLE = 0x00ABAC21;
        private const uint NIS_SHARPEN_VALUE = 0x00ABAB21;
        #endregion

        /// <summary>
        /// 오직 타르코프 게임 프로필에만 클래식 샤프닝을 격리 주입합니다.
        /// 사용자가 NVIDIA 제어판에서 직접 설정한 전역(Base) 프로필은 건드리지 않습니다.
        /// 반환 코드는 로그(tarkov-settings.log)에 기록되므로, 적용이 안 될 때 원인 확인에 사용하세요.
        /// </summary>
        /// <param name="force">true면 직전 적용값과 같아도 다시 씁니다.</param>
        public static bool ApplySharpening(int sharpenPercent, bool force = false)
        {
            sharpenPercent = Math.Max(0, Math.Min(100, sharpenPercent));

            // [수정] 여러 스레드(슬라이더 타이머, 알트탭 감지)가 동시에 드라이버 I/O를 호출하지 못하도록 보호
            lock (_drsLock)
            {
                if (!force && _lastAppliedPercent == sharpenPercent)
                    return true;

                try
                {
                    IntPtr pInit = QueryInterface(0x0150E828);
                    if (pInit == IntPtr.Zero) return false;

                    var nvInit = (NvAPI_Initialize_t)Marshal.GetDelegateForFunctionPointer(pInit, typeof(NvAPI_Initialize_t));
                    int initRc = nvInit();
                    if (initRc != 0)
                    {
                        Logger.Error($"NvAPI_Initialize 실패 (rc={initRc})");
                        return false;
                    }

                    IntPtr pCreateSession = QueryInterface(0x0694D52E);
                    IntPtr pDestroySession = QueryInterface(0xDAD9CFF8);
                    IntPtr pLoadSettings = QueryInterface(0x375ECD69);
                    IntPtr pSaveSettings = QueryInterface(0xFC1349AB);
                    IntPtr pFindProfile = QueryInterface(0x7E4A11B8);
                    IntPtr pSetSetting = QueryInterface(0x577DD202);

                    if (pCreateSession == IntPtr.Zero || pDestroySession == IntPtr.Zero || pLoadSettings == IntPtr.Zero ||
                        pSaveSettings == IntPtr.Zero || pFindProfile == IntPtr.Zero || pSetSetting == IntPtr.Zero)
                    {
                        Logger.Error("NvAPI DRS 함수 포인터를 찾지 못했습니다. (드라이버 버전/인터페이스 ID 불일치 가능)");
                        return false;
                    }

                    var drsCreate = (NvAPI_DRS_CreateSession_t)Marshal.GetDelegateForFunctionPointer(pCreateSession, typeof(NvAPI_DRS_CreateSession_t));
                    var drsDestroy = (NvAPI_DRS_DestroySession_t)Marshal.GetDelegateForFunctionPointer(pDestroySession, typeof(NvAPI_DRS_DestroySession_t));
                    var drsLoad = (NvAPI_DRS_LoadSettings_t)Marshal.GetDelegateForFunctionPointer(pLoadSettings, typeof(NvAPI_DRS_LoadSettings_t));
                    var drsSave = (NvAPI_DRS_SaveSettings_t)Marshal.GetDelegateForFunctionPointer(pSaveSettings, typeof(NvAPI_DRS_SaveSettings_t));
                    var drsFind = (NvAPI_DRS_FindProfileByName_t)Marshal.GetDelegateForFunctionPointer(pFindProfile, typeof(NvAPI_DRS_FindProfileByName_t));
                    var drsSet = (NvAPI_DRS_SetSetting_t)Marshal.GetDelegateForFunctionPointer(pSetSetting, typeof(NvAPI_DRS_SetSetting_t));

                    int rc = drsCreate(out IntPtr hSession);
                    if (rc != 0)
                    {
                        Logger.Error($"NvAPI_DRS_CreateSession 실패 (rc={rc})");
                        return false;
                    }

                    try
                    {
                        rc = drsLoad(hSession);
                        if (rc != 0)
                        {
                            Logger.Error($"NvAPI_DRS_LoadSettings 실패 (rc={rc})");
                            return false;
                        }

                        // 타르코프 전용 격리 주입: "Escape from Tarkov" 엔비디아 공식 프로필에만 단독 적용
                        string[] tarkovProfiles = { "Escape from Tarkov", "Escape from Tarkov: Arena" };
                        uint isEnable = (sharpenPercent > 0) ? 1u : 0u;
                        uint val = (uint)sharpenPercent;

                        bool anyWritten = false;
                        bool allOk = true;

                        foreach (var name in tarkovProfiles)
                        {
                            var uName = new NVAPI_UNICODE_STRING { Value = name };
                            int findRc = drsFind(hSession, ref uName, out IntPtr hTargetProfile);
                            if (findRc != 0 || hTargetProfile == IntPtr.Zero)
                            {
                                // Arena 프로필은 없을 수 있으므로 정보 수준으로만 기록
                                Logger.Info($"DRS 프로필을 찾지 못했습니다: '{name}' (rc={findRc})");
                                continue;
                            }

                            allOk &= SetDword(drsSet, hSession, hTargetProfile, SHARPEN_FILTER_EN, isEnable, "SHARPEN_FILTER_EN");
                            allOk &= SetDword(drsSet, hSession, hTargetProfile, SHARPEN_FILTER_VALUE, val, "SHARPEN_FILTER_VALUE");
                            allOk &= SetDword(drsSet, hSession, hTargetProfile, SHARPEN_FILTER_DENOISE, 15u, "SHARPEN_FILTER_DENOISE");
                            allOk &= SetDword(drsSet, hSession, hTargetProfile, NIS_ENABLE, 0u, "NIS_ENABLE");
                            anyWritten = true;
                        }

                        if (!anyWritten)
                            return false;

                        rc = drsSave(hSession);
                        if (rc != 0)
                        {
                            Logger.Error($"NvAPI_DRS_SaveSettings 실패 (rc={rc}) - 관리자 권한이 필요하거나 드라이버가 거부했을 수 있습니다.");
                            return false;
                        }

                        if (allOk)
                        {
                            _lastAppliedPercent = sharpenPercent;
                            Logger.Info($"DRS 샤프닝 적용 완료: {sharpenPercent}%");
                        }
                        return allOk;
                    }
                    finally
                    {
                        drsDestroy(hSession);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("NvApiDrs 예외", ex);
                    return false;
                }
            }
        }

        private static bool SetDword(NvAPI_DRS_SetSetting_t drsSet, IntPtr hSession, IntPtr hProfile, uint settingId, uint value, string label)
        {
            var s = new NVDRS_SETTING
            {
                version = DRS_SETTING_VER,
                settingId = settingId,
                settingType = 0,
                u32CurrentValue = value
            };

            int rc = drsSet(hSession, hProfile, ref s);
            if (rc != 0)
            {
                // rc=-9 (NVAPI_INCOMPATIBLE_STRUCT_VERSION) 이면 NVDRS_SETTING 구조체 레이아웃/버전 상수 점검 필요
                Logger.Error($"NvAPI_DRS_SetSetting 실패: {label} (rc={rc})");
                return false;
            }
            return true;
        }
    }
}