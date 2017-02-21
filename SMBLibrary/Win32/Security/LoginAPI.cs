/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Utilities;

namespace SMBLibrary.Win32.Security
{
    public enum LogonType
    {
        Interactive = 2, // LOGON32_LOGON_INTERACTIVE
        Network = 3,     // LOGON32_LOGON_NETWORK
        Service = 5,     // LOGON32_LOGON_SERVICE
    }

    public class LoginAPI
    {
        private const int LOGON32_PROVIDER_WINNT40 = 2;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LogonUser(
            string lpszUsername,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            out IntPtr phToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        public static bool ValidateUserPassword(string userName, string password, LogonType logonType)
        {
            IntPtr token;
            bool success = LogonUser(userName, String.Empty, password, (int)logonType, LOGON32_PROVIDER_WINNT40, out token);
            if (!success)
            {
                Win32Error error = (Win32Error)Marshal.GetLastWin32Error();
                if (error == Win32Error.ERROR_ACCOUNT_RESTRICTION ||
                    error == Win32Error.ERROR_ACCOUNT_DISABLED ||
                    error == Win32Error.ERROR_LOGON_FAILURE ||
                    error == Win32Error.ERROR_LOGON_TYPE_NOT_GRANTED)
                {
                    return false;
                }
                throw new Exception("ValidateUser failed, Win32 error: " + error.ToString("D"));
            }
            CloseHandle(token);
            return success;
        }

        public static bool HasEmptyPassword(string userName)
        {
            IntPtr token;
            bool success = LogonUser(userName, String.Empty, String.Empty, (int)LogonType.Network, LOGON32_PROVIDER_WINNT40, out token);
            if (success)
            {
                CloseHandle(token);
                return true;
            }
            else
            {
                Win32Error error = (Win32Error)Marshal.GetLastWin32Error();
                return (error == Win32Error.ERROR_ACCOUNT_RESTRICTION ||
                        error == Win32Error.ERROR_ACCOUNT_DISABLED ||
                        error == Win32Error.ERROR_LOGON_TYPE_NOT_GRANTED);
            }
        }
    }
}
