using System;
using System.Runtime.InteropServices;

namespace tarkov_settings.GPU
{
    public static class NvApiDrsController
    {
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
        /// 전역(Base) 설정은 순정으로 복구하고, 오직 타르코프 게임 프로필에만 클래식 샤프닝을 격리 주입합니다.
        /// </summary>
        public static bool ApplySharpening(int sharpenPercent)
        {
            try
            {
                IntPtr pInit = QueryInterface(0x0150E828);
                if (pInit == IntPtr.Zero) return false;

                var nvInit = (NvAPI_Initialize_t)Marshal.GetDelegateForFunctionPointer(pInit, typeof(NvAPI_Initialize_t));
                if (nvInit() != 0) return false;

                IntPtr pCreateSession = QueryInterface(0x0694D52E);
                IntPtr pDestroySession = QueryInterface(0xDAD9CFF8);
                IntPtr pLoadSettings = QueryInterface(0x375ECD69);
                IntPtr pSaveSettings = QueryInterface(0xFC1349AB);
                IntPtr pGetBaseProfile = QueryInterface(0xC2266519);
                IntPtr pFindProfile = QueryInterface(0x7E4A11B8);
                IntPtr pSetSetting = QueryInterface(0x577DD202);
                IntPtr pDeleteSetting = QueryInterface(0x3D64550F);

                if (pCreateSession == IntPtr.Zero || pSaveSettings == IntPtr.Zero) return false;

                var drsCreate = (NvAPI_DRS_CreateSession_t)Marshal.GetDelegateForFunctionPointer(pCreateSession, typeof(NvAPI_DRS_CreateSession_t));
                var drsDestroy = (NvAPI_DRS_DestroySession_t)Marshal.GetDelegateForFunctionPointer(pDestroySession, typeof(NvAPI_DRS_DestroySession_t));
                var drsLoad = (NvAPI_DRS_LoadSettings_t)Marshal.GetDelegateForFunctionPointer(pLoadSettings, typeof(NvAPI_DRS_LoadSettings_t));
                var drsSave = (NvAPI_DRS_SaveSettings_t)Marshal.GetDelegateForFunctionPointer(pSaveSettings, typeof(NvAPI_DRS_SaveSettings_t));
                var drsGetBase = (NvAPI_DRS_GetBaseProfile_t)Marshal.GetDelegateForFunctionPointer(pGetBaseProfile, typeof(NvAPI_DRS_GetBaseProfile_t));
                var drsFind = (NvAPI_DRS_FindProfileByName_t)Marshal.GetDelegateForFunctionPointer(pFindProfile, typeof(NvAPI_DRS_FindProfileByName_t));
                var drsSet = (NvAPI_DRS_SetSetting_t)Marshal.GetDelegateForFunctionPointer(pSetSetting, typeof(NvAPI_DRS_SetSetting_t));
                var drsDelete = (pDeleteSetting != IntPtr.Zero) ? (NvAPI_DRS_DeleteProfileSetting_t)Marshal.GetDelegateForFunctionPointer(pDeleteSetting, typeof(NvAPI_DRS_DeleteProfileSetting_t)) : null;

                if (drsCreate(out IntPtr hSession) != 0) return false;

                try
                {
                    drsLoad(hSession);

                    // 1. [자동 자가치유] 전역(Base Profile)에 걸렸던 샤프닝/NIS 오버라이드를 삭제하여 바탕화면/크롬 깜빡임 원천 박멸
                    if (drsGetBase(hSession, out IntPtr hBaseProfile) == 0)
                    {
                        if (drsDelete != null)
                        {
                            drsDelete(hSession, hBaseProfile, SHARPEN_FILTER_EN);
                            drsDelete(hSession, hBaseProfile, SHARPEN_FILTER_VALUE);
                            drsDelete(hSession, hBaseProfile, SHARPEN_FILTER_DENOISE);
                            drsDelete(hSession, hBaseProfile, NIS_ENABLE);
                            drsDelete(hSession, hBaseProfile, NIS_SHARPEN_VALUE);
                        }
                        else
                        {
                            SetDword(drsSet, hSession, hBaseProfile, SHARPEN_FILTER_EN, 0u);
                            SetDword(drsSet, hSession, hBaseProfile, NIS_ENABLE, 0u);
                        }
                    }

                    // 2. [타르코프 전용 격리 주입] "Escape from Tarkov" 엔비디아 공식 프로필에만 단독 적용
                    string[] tarkovProfiles = { "Escape from Tarkov", "Escape from Tarkov: Arena" };
                    uint isEnable = (sharpenPercent > 0) ? 1u : 0u;
                    uint val = (uint)Math.Max(0, Math.Min(100, sharpenPercent));

                    foreach (var name in tarkovProfiles)
                    {
                        var uName = new NVAPI_UNICODE_STRING { Value = name };
                        if (drsFind(hSession, ref uName, out IntPtr hTargetProfile) == 0 && hTargetProfile != IntPtr.Zero)
                        {
                            // 순수 클래식 샤프닝만 주입 (지형 까만 네모 깜빡임 0%)
                            SetDword(drsSet, hSession, hTargetProfile, SHARPEN_FILTER_EN, isEnable);
                            SetDword(drsSet, hSession, hTargetProfile, SHARPEN_FILTER_VALUE, val);
                            SetDword(drsSet, hSession, hTargetProfile, SHARPEN_FILTER_DENOISE, 15u);
                            SetDword(drsSet, hSession, hTargetProfile, NIS_ENABLE, 0u); // NIS는 무조건 0으로 차단
                        }
                    }

                    drsSave(hSession);
                    return true;
                }
                finally
                {
                    drsDestroy(hSession);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NvApiDrs Exception] {ex.Message}");
            }
            return false;
        }

        private static void SetDword(NvAPI_DRS_SetSetting_t drsSet, IntPtr hSession, IntPtr hProfile, uint settingId, uint value)
        {
            var s = new NVDRS_SETTING
            {
                version = DRS_SETTING_VER,
                settingId = settingId,
                settingType = 0,
                u32CurrentValue = value
            };
            drsSet(hSession, hProfile, ref s);
        }
    }
}