/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SMBLibrary.Authentication.Win32
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
        private const uint SEC_I_CONTINUE_NEEDED = 0x90312;
        private const uint SEC_E_INVALID_HANDLE = 0x80090301;
        private const uint SEC_E_INVALID_TOKEN = 0x80090308;
        private const uint SEC_E_LOGON_DENIED = 0x8009030C;
        private const uint SEC_E_BUFFER_TOO_SMALL = 0x80090321;

        private const int SECURITY_NETWORK_DREP = 0x00;
        private const int SECURITY_NATIVE_DREP = 0x10;

        private const int SECPKG_CRED_INBOUND = 0x01;
        private const int SECPKG_CRED_OUTBOUND = 0x02;
        private const int SECPKG_CRED_BOTH = 0x03;

        private const int ISC_REQ_CONFIDENTIALITY = 0x00000010;
        private const int ISC_REQ_ALLOCATE_MEMORY = 0x00000100;
        private const int ISC_REQ_INTEGRITY = 0x00010000;

        private const int ASC_REQ_REPLAY_DETECT = 0x00000004;
        private const int ASC_REQ_CONFIDENTIALITY = 0x00000010;
        private const int ASC_REQ_USE_SESSION_KEY = 0x00000020;
        private const int ASC_REQ_INTEGRITY = 0x00020000;

        private const int SEC_WINNT_AUTH_IDENTITY_ANSI = 1;
        private const int SEC_WINNT_AUTH_IDENTITY_UNICODE = 2;
        
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
            public int UserLength;
            public string Domain;
            public int DomainLength;
            public string Password;
            public int PasswordLength;
            public int Flags;
        };

        [DllImport("secur32.dll", SetLastError = true)]
        private static extern int AcquireCredentialsHandle(
          string pszPrincipal,
          string pszPackage,
          int fCredentialUse,
          IntPtr pvLogonID,
          IntPtr pAuthData,
          IntPtr pGetKeyFn,
          IntPtr pvGetKeyArgument,
          out SecHandle phCredential,
          out SECURITY_INTEGER ptsExpiry);

        [DllImport("secur32.dll", SetLastError = true)]
        private static extern int InitializeSecurityContext(
            ref SecHandle phCredential,
            IntPtr phContext,
            string pszTargetName,
            int fContextReq,
            int Reserved1,
            int TargetDataRep,
            IntPtr pInput,
            int Reserved2,
            ref SecHandle phNewContext,
            ref SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SECURITY_INTEGER ptsExpiry);

        [DllImport("secur32.dll", SetLastError = true)]
        private static extern int InitializeSecurityContext(
            IntPtr phCredential,
            ref SecHandle phContext,
            string pszTargetName,
            int fContextReq,
            int Reserved1,
            int TargetDataRep,
            ref SecBufferDesc pInput,
            int Reserved2,
            ref SecHandle phNewContext,
            ref SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SECURITY_INTEGER ptsExpiry);

        [DllImport("secur32.dll", SetLastError = true)]
        private static extern int AcceptSecurityContext(
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
        private static extern int AcceptSecurityContext(
            IntPtr phCredential,
            ref SecHandle phContext,
            ref SecBufferDesc pInput,
            uint fContextReq,
            uint TargetDataRep,
            ref SecHandle phNewContext,
            ref SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SECURITY_INTEGER ptsTimeStamp);

        [DllImport("Secur32.dll")]
        private extern static int FreeContextBuffer(
            IntPtr pvContextBuffer
        );

        [DllImport("Secur32.dll")]
        private extern static int FreeCredentialsHandle(
            ref SecHandle phCredential
        );

        [DllImport("Secur32.dll")]
        private extern static int DeleteSecurityContext(
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
            auth.DomainLength = domainName.Length;
            auth.User = userName;
            auth.UserLength = userName.Length;
            auth.Password = password;
            auth.PasswordLength = password.Length;
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

            int result = AcquireCredentialsHandle(null, "NTLM", SECPKG_CRED_BOTH, IntPtr.Zero, pAuthData, IntPtr.Zero, IntPtr.Zero, out credential, out expiry);
            if (pAuthData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pAuthData);
            }
            if (result != SEC_E_OK)
            {
                throw new Exception("AcquireCredentialsHandle failed, Error code " + ((uint)result).ToString("X"));
            }

            return credential;
        }

        public static byte[] GetType1Message(string userName, string password, out SecHandle clientContext)
        {
            return GetType1Message(String.Empty, userName, password, out clientContext);
        }

        public static byte[] GetType1Message(string domainName, string userName, string password, out SecHandle clientContext)
        {
            SecHandle handle = AcquireNTLMCredentialsHandle(domainName, userName, password);
            clientContext = new SecHandle();
            SecBufferDesc output = new SecBufferDesc(MAX_TOKEN_SIZE);
            uint contextAttributes;
            SECURITY_INTEGER expiry;

            int result = InitializeSecurityContext(ref handle, IntPtr.Zero, null, ISC_REQ_CONFIDENTIALITY | ISC_REQ_INTEGRITY, 0, SECURITY_NATIVE_DREP, IntPtr.Zero, 0, ref clientContext, ref output, out contextAttributes, out expiry);
            if (result != SEC_E_OK && result != SEC_I_CONTINUE_NEEDED)
            {
                if ((uint)result == SEC_E_INVALID_HANDLE)
                {
                    throw new Exception("InitializeSecurityContext failed, Invalid handle");
                }
                else if ((uint)result == SEC_E_BUFFER_TOO_SMALL)
                {
                    throw new Exception("InitializeSecurityContext failed, Buffer too small");
                }
                else
                {
                    throw new Exception("InitializeSecurityContext failed, Error code " + ((uint)result).ToString("X"));
                }
            }
            return output.GetSecBufferBytes();
        }

        public static byte[] GetType3Message(SecHandle clientContext, byte[] type2Message)
        {
            SecHandle newContext = new SecHandle();
            SecBufferDesc input = new SecBufferDesc(type2Message);
            SecBufferDesc output = new SecBufferDesc(MAX_TOKEN_SIZE);
            uint contextAttributes;
            SECURITY_INTEGER expiry;

            int result = InitializeSecurityContext(IntPtr.Zero, ref clientContext, null, ISC_REQ_CONFIDENTIALITY | ISC_REQ_INTEGRITY, 0, SECURITY_NATIVE_DREP, ref input, 0, ref newContext, ref output, out contextAttributes, out expiry);
            if (result != SEC_E_OK)
            {
                if ((uint)result == SEC_E_INVALID_HANDLE)
                {
                    throw new Exception("InitializeSecurityContext failed, invalid handle");
                }
                else if ((uint)result == SEC_E_BUFFER_TOO_SMALL)
                {
                    throw new Exception("InitializeSecurityContext failed, buffer too small");
                }
                else
                {
                    throw new Exception("InitializeSecurityContext failed, error code " + ((uint)result).ToString("X"));
                }
            }
            return output.GetSecBufferBytes();
        }

        public static byte[] GetType2Message(byte[] type1MessageBytes, out SecHandle serverContext)
        {
            SecHandle handle = AcquireNTLMCredentialsHandle();
            SecBufferDesc type1Message = new SecBufferDesc(type1MessageBytes);
            serverContext = new SecHandle();
            SecBufferDesc output = new SecBufferDesc(MAX_TOKEN_SIZE);
            uint contextAttributes;
            SECURITY_INTEGER timestamp;

            int result = AcceptSecurityContext(ref handle, IntPtr.Zero, ref type1Message, ASC_REQ_INTEGRITY | ASC_REQ_CONFIDENTIALITY, SECURITY_NATIVE_DREP, ref serverContext, ref output, out contextAttributes, out timestamp);
            if (result != SEC_E_OK && result != SEC_I_CONTINUE_NEEDED)
            {
                if ((uint)result == SEC_E_INVALID_HANDLE)
                {
                    throw new Exception("AcceptSecurityContext failed, invalid handle");
                }
                else if ((uint)result == SEC_E_BUFFER_TOO_SMALL)
                {
                    throw new Exception("AcceptSecurityContext failed, buffer too small");
                }
                else
                {
                    throw new Exception("AcceptSecurityContext failed, error code " + ((uint)result).ToString("X"));
                }
            }
            FreeCredentialsHandle(ref handle);
            return output.GetSecBufferBytes();
        }

        /// <summary>
        /// AcceptSecurityContext will return SEC_E_LOGON_DENIED when the password is correct in these cases:
        /// 1. The account is listed under the "Deny access to this computer from the network" list.
        /// 2. 'limitblankpassworduse' is set to 1, non-guest is attempting to login with an empty password,
        ///    and the Guest account is disabled, has non-empty pasword set or listed under the "Deny access to this computer from the network" list.
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
            SecBufferDesc type3Message = new SecBufferDesc(type3MessageBytes);
            SecBufferDesc output = new SecBufferDesc(MAX_TOKEN_SIZE);
            uint contextAttributes;
            SECURITY_INTEGER timestamp;

            int result = AcceptSecurityContext(IntPtr.Zero, ref serverContext, ref type3Message, ASC_REQ_INTEGRITY | ASC_REQ_CONFIDENTIALITY, SECURITY_NATIVE_DREP, ref newContext, ref output, out contextAttributes, out timestamp);
            
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
                if ((uint)result == SEC_E_INVALID_TOKEN)
                {
                    throw new Exception("AcceptSecurityContext failed, invalid security token");
                }
                else
                {
                    throw new Exception("AcceptSecurityContext failed, error code " + ((uint)result).ToString("X"));
                }
            }
        }
    }
}
