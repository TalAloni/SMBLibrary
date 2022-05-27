/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Runtime.InteropServices;

namespace SMBLibrary.Win32.Security
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SecHandle
    {
        public IntPtr dwLower;
        public IntPtr dwUpper;
    };

    public partial class SSPIHelper
    {
        private const int MAX_TOKEN_SIZE = 12000;

        private const uint SEC_E_OK = 0;
        private const uint SEC_I_CONTINUE_NEEDED = 0x00090312;
        private const uint SEC_E_INVALID_HANDLE = 0x80090301;
        private const uint SEC_E_INVALID_TOKEN = 0x80090308;
        private const uint SEC_E_LOGON_DENIED = 0x8009030C;
        private const uint SEC_E_BUFFER_TOO_SMALL = 0x80090321;

        private const uint SECURITY_NETWORK_DREP = 0x00;
        private const uint SECURITY_NATIVE_DREP = 0x10;

        private const uint SECPKG_CRED_INBOUND = 0x01;
        private const uint SECPKG_CRED_OUTBOUND = 0x02;
        private const uint SECPKG_CRED_BOTH = 0x03;

        private const uint ISC_REQ_CONFIDENTIALITY = 0x00000010;
        private const uint ISC_REQ_ALLOCATE_MEMORY = 0x00000100;
        private const uint ISC_REQ_INTEGRITY = 0x00010000;

        private const uint ASC_REQ_REPLAY_DETECT = 0x00000004;
        private const uint ASC_REQ_CONFIDENTIALITY = 0x00000010;
        private const uint ASC_REQ_USE_SESSION_KEY = 0x00000020;
        private const uint ASC_REQ_INTEGRITY = 0x00020000;

        private const uint SEC_WINNT_AUTH_IDENTITY_ANSI = 1;
        private const uint SEC_WINNT_AUTH_IDENTITY_UNICODE = 2;

        private const uint SECPKG_ATTR_NAME = 1; // Username
        private const uint SECPKG_ATTR_SESSION_KEY = 9;
        private const uint SECPKG_ATTR_ACCESS_TOKEN = 18;

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_INTEGER
        {
            public uint LowPart;
            public int HighPart;
        };

        /// <summary>
        /// When using the NTLM package, the maximum character lengths for user name, password, and domain are 256, 256, and 15, respectively.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct SEC_WINNT_AUTH_IDENTITY
        {
            public string User;
            public uint UserLength;
            public string Domain;
            public uint DomainLength;
            public string Password;
            public uint PasswordLength;
            public uint Flags;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct SecPkgContext_SessionKey
        {
            public uint SessionKeyLength;
            public IntPtr SessionKey;
        }

        [DllImport("secur32.dll", SetLastError = true)]
        private static extern uint AcquireCredentialsHandle(
          string pszPrincipal,
          string pszPackage,
          uint fCredentialUse,
          IntPtr pvLogonID,
          IntPtr pAuthData,
          IntPtr pGetKeyFn,
          IntPtr pvGetKeyArgument,
          out SecHandle phCredential,
          out SECURITY_INTEGER ptsExpiry);

        [DllImport("secur32.dll", SetLastError = true)]
        private static extern uint InitializeSecurityContext(
            ref SecHandle phCredential,
            IntPtr phContext,
            string pszTargetName,
            uint fContextReq,
            uint Reserved1,
            uint TargetDataRep,
            IntPtr pInput,
            uint Reserved2,
            ref SecHandle phNewContext,
            ref SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SECURITY_INTEGER ptsExpiry);

        [DllImport("secur32.dll", SetLastError = true)]
        private static extern uint InitializeSecurityContext(
            IntPtr phCredential,
            ref SecHandle phContext,
            string pszTargetName,
            uint fContextReq,
            uint Reserved1,
            uint TargetDataRep,
            ref SecBufferDesc pInput,
            uint Reserved2,
            ref SecHandle phNewContext,
            ref SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SECURITY_INTEGER ptsExpiry);

        [DllImport("secur32.dll", SetLastError = true)]
        private static extern uint AcceptSecurityContext(
            ref SecHandle phCredential,
            IntPtr phContext,
            ref SecBufferDesc pInput,
            uint fContextReq,
            uint TargetDataRep,
            ref SecHandle phNewContext,
            ref SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SECURITY_INTEGER ptsTimeStamp);

        [DllImport("secur32.dll", SetLastError = true)]
        private static extern uint AcceptSecurityContext(
            IntPtr phCredential,
            ref SecHandle phContext,
            ref SecBufferDesc pInput,
            uint fContextReq,
            uint TargetDataRep,
            ref SecHandle phNewContext,
            ref SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SECURITY_INTEGER ptsTimeStamp);

        [DllImport("secur32.Dll", SetLastError = true)]
        private static extern uint QueryContextAttributes(
            ref SecHandle phContext,
            uint ulAttribute,
            out string value);

        [DllImport("secur32.Dll", SetLastError = true)]
        private static extern uint QueryContextAttributes(
            ref SecHandle phContext,
            uint ulAttribute,
            out IntPtr value);

        [DllImport("secur32.Dll", SetLastError = true)]
        private static extern uint QueryContextAttributes(
            ref SecHandle phContext,
            uint ulAttribute,
            out SecPkgContext_SessionKey value);

        [DllImport("Secur32.dll")]
        private extern static uint FreeContextBuffer(
            IntPtr pvContextBuffer
        );

        [DllImport("Secur32.dll")]
        private extern static uint FreeCredentialsHandle(
            ref SecHandle phCredential
        );

        [DllImport("Secur32.dll")]
        public extern static uint DeleteSecurityContext(
            ref SecHandle phContext
        );

        public static string GetUserName(SecHandle context)
        {
            string userName;
            uint result = QueryContextAttributes(ref context, SECPKG_ATTR_NAME, out userName);
            if (result == SEC_E_OK)
            {
                return userName;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Windows Vista or newer is required for SECPKG_ATTR_SESSION_KEY to work.
        /// Windows XP / Server 2003 will return SEC_E_INVALID_TOKEN.
        /// </summary>
        public static byte[] GetSessionKey(SecHandle context)
        {
            SecPkgContext_SessionKey sessionKey;
            uint result = QueryContextAttributes(ref context, SECPKG_ATTR_SESSION_KEY, out sessionKey);
            if (result == SEC_E_OK)
            {
                int length = (int)sessionKey.SessionKeyLength;
                byte[] sessionKeyBytes = new byte[length];
                Marshal.Copy(sessionKey.SessionKey, sessionKeyBytes, 0, length);
                return sessionKeyBytes;
            }
            else
            {
                return null;
            }
        }

        public static IntPtr GetAccessToken(SecHandle serverContext)
        {
            IntPtr accessToken;
            uint result = QueryContextAttributes(ref serverContext, SECPKG_ATTR_ACCESS_TOKEN, out accessToken);
            if (result == SEC_E_OK)
            {
                return accessToken;
            }
            else
            {
                return IntPtr.Zero;
            }
        }

        private static SEC_WINNT_AUTH_IDENTITY GetWinNTAuthIdentity(string domainName, string userName, string password)
        {
            SEC_WINNT_AUTH_IDENTITY auth = new SEC_WINNT_AUTH_IDENTITY();
            auth.Domain = domainName;
            auth.DomainLength = (uint)domainName.Length;
            auth.User = userName;
            auth.UserLength = (uint)userName.Length;
            auth.Password = password;
            auth.PasswordLength = (uint)password.Length;
            auth.Flags = SEC_WINNT_AUTH_IDENTITY_ANSI;
            return auth;
        }
    }
}
