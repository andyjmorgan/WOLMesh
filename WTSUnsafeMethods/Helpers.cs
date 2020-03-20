using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static WTSUnsafeMethods.pInvoke;
using static WTSUnsafeMethods.Structures;

namespace WTSUnsafeMethods
{
    public class Helpers
    {
        public static IntPtr OpenServer(String Name)
        {
            IntPtr server = WTSOpenServer(Name);
            return server;
        }
        public static void CloseServer(IntPtr ServerHandle)
        {
            WTSCloseServer(ServerHandle);
        }
        //public static void ListUsers(String ServerName)
        //{
        //    IntPtr serverHandle = IntPtr.Zero;
        //    List<String> resultList = new List<string>();
        //    serverHandle = OpenServer(ServerName);

        //    try
        //    {
        //        IntPtr SessionInfoPtr = IntPtr.Zero;
        //        IntPtr userPtr = IntPtr.Zero;
        //        IntPtr domainPtr = IntPtr.Zero;
        //        Int32 sessionCount = 0;
        //        Int32 retVal = WTSEnumerateSessions(serverHandle, 0, 1, ref SessionInfoPtr, ref sessionCount);
        //        Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
        //        IntPtr currentSession = SessionInfoPtr;
        //        uint bytes = 0;

        //        if (retVal != 0)
        //        {
        //            for (int i = 0; i < sessionCount; i++)
        //            {
        //                WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure((System.IntPtr)currentSession, typeof(WTS_SESSION_INFO));
        //                currentSession += dataSize;

        //                WTSQuerySessionInformation(serverHandle, si.SessionID, WTS_INFO_CLASS.WTSSessionInfo, out userPtr, out bytes);


        //                Console.WriteLine("Domain and User: " + Marshal.PtrToStringAnsi(domainPtr) + "\\" + Marshal.PtrToStringAnsi(userPtr));
        //                WTSFreeMemory(userPtr);
        //                WTSFreeMemory(domainPtr);
        //            }
        //            WTSFreeMemory(SessionInfoPtr);
        //        }
        //    }
        //    finally
        //    {
        //        CloseServer(serverHandle);
        //    }
        //}
        public static bool LogOffSession(int sessionID)
        {
            return pInvoke.WTSLogoffSession(Structures.WTS_CURRENT_SERVER_HANDLE, sessionID, false);
        }

        public static void SendMessageViaMessageBoxToSession(int sessionID, string title, string message, Int32 timeout, string FullProcessPath, string WorkingDirectory)
        {
            ProcessExtensions.StartProcessInSession(
                                        FullProcessPath, (uint)sessionID, @"/t=""" + title + @""" /m=""" + 
                                        message + @""" /s=" + (timeout * 60) * 1000, WorkingDirectory, true);

        }

        public static bool SendMessageToSession(int sessionID, string message, string title, int timeout)
        {
            int resp = 0;
            return pInvoke.WTSSendMessage(WTSUnsafeMethods.Structures.WTS_CURRENT_SERVER_HANDLE, sessionID, title, title.Length, message, message.Length, (int)0, timeout, out resp, false);
        }
        public static List<WTS_SESSION_INFO> ListSessions()
        {
            List<String> ret = new List<string>();
            //server = OpenServer(ServerName);

            List<WTS_SESSION_INFO> retList = new List<WTS_SESSION_INFO>();

            IntPtr ppSessionInfo = IntPtr.Zero;

            Int32 count = 0;
            Int32 retval = WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1, ref ppSessionInfo, ref count);
            Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));

            Int64 current = (int)ppSessionInfo;

            if (retval != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure((System.IntPtr)current, typeof(WTS_SESSION_INFO));
                    current += dataSize;
                    retList.Add(si);
                }
                WTSFreeMemory(ppSessionInfo);
            }
            return retList;
        }
        public static bool DisconnectSession(int SessionID)
        {
            return WTSDisconnectSession(WTS_CURRENT_SERVER_HANDLE, SessionID, false);
        }
        public static WTSInfoEX GetSessionDetails(int sessionId)
        {
            IntPtr SessionData = IntPtr.Zero;
            try
            {
                if (WTSQuerySessionInformation(WTS_CURRENT_SERVER_HANDLE, sessionId, WTS_INFO_CLASS.WTSSessionInfo, out SessionData, out uint bytesReturned) == false)
                {

                    throw new Exception("VDP_QuerySessionInformation failed for SessionID: " + sessionId);
                }
                WTSInfo data = (WTSInfo)Marshal.PtrToStructure(SessionData, typeof(WTSInfo));
                return new WTSInfoEX(data);
            }
            finally
            {
                if (SessionData != IntPtr.Zero)
                {
                    WTSFreeMemory(SessionData);
                }
            }
        }
    }
}
