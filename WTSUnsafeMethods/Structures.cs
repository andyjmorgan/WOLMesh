using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WTSUnsafeMethods
{
    public static class Structures
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_SESSION_INFO
        {
            public Int32 SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pWinStationName;

            public WTS_CONNECTSTATE_CLASS State;
        }

        public enum WTS_INFO_CLASS
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType,
            WTSIdleTime,
            WTSLogonTime,
            WTSIncomingBytes,
            WTSOutgoingBytes,
            WTSIncomingFrames,
            WTSOutgoingFrames,
            WTSClientInfo,
            WTSSessionInfo,
            WTSSessionInfoEx,
            WTSConfigInfo,
            WTSValidationInfo,
            WTSSessionAddressV4,
            WTSIsRemoteSession
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class WTSInfo
        {
            public WTS_CONNECTSTATE_CLASS State;

            public uint SessionId;

            public uint IncomingBytes;

            public uint OutgoingBytes;

            public uint IncomingFrames;

            public uint OutgoingFrames;

            public uint IncomingCompressedBytes;

            public uint OutgoingCompressBytes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string WinStationName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 17)]
            public string Domain;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
            public string Username;

            public long ConnectTime;

            public long DisconnectTime;

            public long LastInputTime;

            public long LogonTime;

            public long CurrentTime;
        }
        public class WTSInfoEX : WTSInfo
        {
            public WTSInfo _info;
            public DateTime ConnectTimeUTC;

            public DateTime DisconnectTimeUTC;

            public DateTime LastInputTimeUTC;

            public DateTime LogonTimeUTC;

            public DateTime CurrentTimeUTC;

            public WTSInfoEX(WTSInfo info) : base()
            {
                State = info.State;
                SessionId = info.SessionId;

                IncomingBytes = info.IncomingBytes;

                OutgoingBytes = info.OutgoingBytes;
                IncomingFrames = info.IncomingFrames;
                OutgoingFrames = info.OutgoingFrames;
                IncomingCompressedBytes = info.IncomingCompressedBytes;
                OutgoingCompressBytes = info.OutgoingCompressBytes;
                WinStationName = info.WinStationName;
                Domain = info.Domain;
                Username = info.Username;
                ConnectTime = info.ConnectTime;
                DisconnectTime = info.DisconnectTime;
                LastInputTime = info.LastInputTime;
                LogonTime = info.LogonTime;
                CurrentTime = info.CurrentTime;
                ConnectTimeUTC = InternalClasses.DateTimeFromLong(info.ConnectTime);
                DisconnectTimeUTC = InternalClasses.DateTimeFromLong(info.DisconnectTime);
                LastInputTimeUTC = InternalClasses.DateTimeFromLong(info.LastInputTime);
                LogonTimeUTC = InternalClasses.DateTimeFromLong(info.LogonTime);
                CurrentTimeUTC = InternalClasses.DateTimeFromLong(info.CurrentTime);
            }
        }
        public enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }
        public static IntPtr WTS_CURRENT_SERVER_HANDLE = (IntPtr)null;

        [Flags]
        public enum MB_type
        {
            MB_ABORTRETRYIGNORE = 2,
            MB_APPLMODAL = 0,
            MB_CANCELTRYCONTINUE = 6,
            MB_DEFAULT_DESKTOP_ONLY = 131072,
            MB_DEFBUTTON1 = 0,
            MB_DEFBUTTON2 = 256,
            MB_DEFBUTTON3 = 512,
            MB_DEFBUTTON4 = 768,
            MB_DEFMASK = 3840,
            MB_HELP = 16384,
            MB_ICONASTERISK = 64,
            MB_ICONERROR = 16,
            MB_ICONEXCLAMATION = 48,
            MB_ICONHAND = 16,
            MB_ICONINFORMATION = 64,
            MB_ICONMASK = 240,
            MB_ICONQUESTION = 32,
            MB_ICONSTOP = 16,
            MB_ICONWARNING = 48,
            MB_MISCMASK = 49152,
            MB_MODEMASK = 12288,
            MB_NOFOCUS = 32768,
            MB_OK = 0,
            MB_OKCANCEL = 1,
            MB_RETRYCANCEL = 5,
            MB_RIGHT = 524288,
            MB_RTLREADING = 1048576,
            MB_SERVICE_NOTIFICATION = 2097152,
            MB_SERVICE_NOTIFICATION_NT3X = 262144,
            MB_SETFOREGROUND = 65536,
            MB_SYSTEMMODAL = 4096,
            MB_TASKMODAL = 8192,
            MB_TOPMOST = 262144,
            MB_TYPEMASK = 15,
            MB_USERICON = 128,
            MB_YESNO = 4,
            MB_YESNOCANCEL = 3,
            None = 0
        }
    }
}
