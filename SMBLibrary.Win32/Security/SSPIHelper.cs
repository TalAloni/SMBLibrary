/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SMBLibrary.Win32.Security
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SecHandle
    {
        public IntPtr dwLower;
        public IntPtr dwUpper;
    };

    public class SSPIHelper
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

        public static SecHandle AcquireNTLMCredentialsHandle()
        {
            return AcquireNTLMCredentialsHandle(null);
        }

        public static SecHandle AcquireNTLMCredentialsHandle(string domainName, string userName, string password)
        {
            SEC_WINNT_AUTH_IDENTITY auth = new SEC_WINNT_AUTH_IDENTITY();
            auth.Domain = domainName;
            auth.DomainLength = (uint)domainName.Length;
            auth.User = userName;
            auth.UserLength = (uint)userName.Length;
            auth.Password = password;
            auth.PasswordLength = (uint)password.Length;
            auth.Flags = SEC_WINNT_AUTH_IDENTITY_ANSI;
            return AcquireNTLMCredentialsHandle(auth);
        }

        private static SecHandle AcquireNTLMCredentialsHandle(SEC_WINNT_AUTH_IDENTITY? auth)
        {
            SecHandle credential;
            SECURITY_INTEGER expiry;

            IntPtr pAuthData;
            if (auth.HasValue)
            {
                pAuthData = Marshal.AllocHGlobal(Marshal.SizeOf(auth.Value));
                Marshal.StructureToPtr(auth.Value, pAuthData, false);
            }
            else
            {
                pAuthData = IntPtr.Zero;
            }

            uint result = AcquireCredentialsHandle(null, "NTLM", SECPKG_CRED_BOTH, IntPtr.Zero, pAuthData, IntPtr.Zero, IntPtr.Zero, out credential, out expiry);
            if (pAuthData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pAuthData);
            }
            if (result != SEC_E_OK)
            {
                throw new Exception("AcquireCredentialsHandle failed, Error code 0x" + result.ToString("X8"));
            }

            return credential;
        }

        public static byte[] GetType1Message(string userName, string password, out SecHandle clientContext)
        {
            return GetType1Message(String.Empty, userName, password, out clientContext);
        }

        public static byte[] GetType1Message(string domainName, string userName, string password, out SecHandle clientContext)
        {
            SecHandle credentialsHandle = AcquireNTLMCredentialsHandle(domainName, userName, password);
            clientContext = new SecHandle();
            SecBuffer outputBuffer = new SecBuffer(MAX_TOKEN_SIZE);
            SecBufferDesc output = new SecBufferDesc(outputBuffer);
            uint contextAttributes;
            SECURITY_INTEGER expiry;

            uint result = InitializeSecurityContext(ref credentialsHandle, IntPtr.Zero, null, ISC_REQ_CONFIDENTIALITY | ISC_REQ_INTEGRITY, 0, SECURITY_NATIVE_DREP, IntPtr.Zero, 0, ref clientContext, ref output, out contextAttributes, out expiry);
            if (result != SEC_E_OK && result != SEC_I_CONTINUE_NEEDED)
            {
                if (result == SEC_E_INVALID_HANDLE)
                {
                    throw new Exception("InitializeSecurityContext failed, Invalid handle");
                }
                else if (result == SEC_E_BUFFER_TOO_SMALL)
                {
                    throw new Exception("InitializeSecurityContext failed, Buffer too small");
                }
                else
                {
                    throw new Exception("InitializeSecurityContext failed, Error code 0x" + result.ToString("X8"));
                }
            }
            FreeCredentialsHandle(ref credentialsHandle);
            byte[] messageBytes = output.GetBufferBytes(0);
            outputBuffer.Dispose();
            output.Dispose();
            return messageBytes;
        }

        public static byte[] GetType3Message(SecHandle clientContext, byte[] type2Message)
        {
            SecHandle newContext = new SecHandle();
            SecBuffer inputBuffer = new SecBuffer(type2Message);
            SecBufferDesc input = new SecBufferDesc(inputBuffer);
            SecBuffer outputBuffer = new SecBuffer(MAX_TOKEN_SIZE);
            SecBufferDesc output = new SecBufferDesc(outputBuffer);
            uint contextAttributes;
            SECURITY_INTEGER expiry;

            uint result = InitializeSecurityContext(IntPtr.Zero, ref clientContext, null, ISC_REQ_CONFIDENTIALITY | ISC_REQ_INTEGRITY, 0, SECURITY_NATIVE_DREP, ref input, 0, ref newContext, ref output, out contextAttributes, out expiry);
            if (result != SEC_E_OK)
            {
                if (result == SEC_E_INVALID_HANDLE)
                {
                    throw new Exception("InitializeSecurityContext failed, Invalid handle");
                }
                else if (result == SEC_E_INVALID_TOKEN)
                {
                    throw new Exception("InitializeSecurityContext failed, Invalid token");
                }
                else if (result == SEC_E_BUFFER_TOO_SMALL)
                {
                    throw new Exception("InitializeSecurityContext failed, Buffer too small");
                }
                else
                {
                    throw new Exception("InitializeSecurityContext failed, Error code 0x" + result.ToString("X8"));
                }
            }
            byte[] messageBytes = output.GetBufferBytes(0);
            inputBuffer.Dispose();
            input.Dispose();
            outputBuffer.Dispose();
            output.Dispose();
            return messageBytes;
        }

        public static byte[] GetType2Message(byte[] type1MessageBytes, out SecHandle serverContext)
        {
            SecHandle credentialsHandle = AcquireNTLMCredentialsHandle();
            SecBuffer inputBuffer = new SecBuffer(type1MessageBytes);
            SecBufferDesc input = new SecBufferDesc(inputBuffer);
            serverContext = new SecHandle();
            SecBuffer outputBuffer = new SecBuffer(MAX_TOKEN_SIZE);
            SecBufferDesc output = new SecBufferDesc(outputBuffer);
            uint contextAttributes;
            SECURITY_INTEGER timestamp;

            uint result = AcceptSecurityContext(ref credentialsHandle, IntPtr.Zero, ref input, ASC_REQ_INTEGRITY | ASC_REQ_CONFIDENTIALITY, SECURITY_NATIVE_DREP, ref serverContext, ref output, out contextAttributes, out timestamp);
            if (result != SEC_E_OK && result != SEC_I_CONTINUE_NEEDED)
            {
                if (result == SEC_E_INVALID_HANDLE)
                {
                    throw new Exception("AcceptSecurityContext failed, Invalid handle");
                }
                else if (result == SEC_E_INVALID_TOKEN)
                {
                    throw new Exception("AcceptSecurityContext failed, Invalid token");
                }
                else if (result == SEC_E_BUFFER_TOO_SMALL)
                {
                    throw new Exception("AcceptSecurityContext failed, Buffer too small");
                }
                else
                {
                    throw new Exception("AcceptSecurityContext failed, Error code 0x" + result.ToString("X8"));
                }
            }
            FreeCredentialsHandle(ref credentialsHandle);
            byte[] messageBytes = output.GetBufferBytes(0);
            inputBuffer.Dispose();
            input.Dispose();
            outputBuffer.Dispose();
            output.Dispose();
            return messageBytes;
        }

        /// <summary>
        /// AcceptSecurityContext will return SEC_E_LOGON_DENIED when the password is correct in these cases:
        /// 1. The account is listed under the "Deny access to this computer from the network" list.
        /// 2. 'limitblankpassworduse' is set to 1, non-guest is attempting to login with an empty password,
        ///    and the Guest account is disabled, has non-empty pasword set or listed under the "Deny access to this computer from the network" list.
        /// 
        /// Note: "If the Guest account is enabled, SSPI logon may succeed as Guest for user credentials that are not valid".
        /// </summary>
        /// <remarks>
        /// 1. 'limitblankpassworduse' will not affect the Guest account.
        /// 2. Listing the user in the "Deny access to this computer from the network" or the "Deny logon locally" lists will not affect AcceptSecurityContext if all of these conditions are met.
        /// - 'limitblankpassworduse' is set to 1.
        /// - The user has an empty password set.
        /// - Guest is NOT listed in the "Deny access to this computer from the network" list.
        /// - Guest is enabled and has empty pasword set.
        /// </remarks>
        public static bool AuthenticateType3Message(SecHandle serverContext, byte[] type3MessageBytes)
        {
            SecHandle newContext = new SecHandle();
            SecBuffer inputBuffer = new SecBuffer(type3MessageBytes);
            SecBufferDesc input = new SecBufferDesc(inputBuffer);
            SecBuffer outputBuffer = new SecBuffer(MAX_TOKEN_SIZE);
            SecBufferDesc output = new SecBufferDesc(outputBuffer);
            uint contextAttributes;
            SECURITY_INTEGER timestamp;

            uint result = AcceptSecurityContext(IntPtr.Zero, ref serverContext, ref input, ASC_REQ_INTEGRITY | ASC_REQ_CONFIDENTIALITY, SECURITY_NATIVE_DREP, ref newContext, ref output, out contextAttributes, out timestamp);

            inputBuffer.Dispose();
            input.Dispose();
            outputBuffer.Dispose();
            output.Dispose();

            if (result == SEC_E_OK)
            {
                return true;
            }
            else if ((uint)result == SEC_E_LOGON_DENIED)
            {
                return false;
            }
            else
            {
                if (result == SEC_E_INVALID_HANDLE)
                {
                    throw new Exception("AcceptSecurityContext failed, Invalid handle");
                }
                else if (result == SEC_E_INVALID_TOKEN)
                {
                    throw new Exception("AcceptSecurityContext failed, Invalid security token");
                }
                else
                {
                    throw new Exception("AcceptSecurityContext failed, Error code 0x" + result.ToString("X8"));
                }
            }
        }

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
    }
}
