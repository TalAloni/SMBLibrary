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
    public partial class SSPIHelper
    {
        public static SecHandle AcquireNTLMCredentialsHandle()
        {
            return AcquireNTLMCredentialsHandle(null);
        }

        public static SecHandle AcquireNTLMCredentialsHandle(string domainName, string userName, string password)
        {
            SEC_WINNT_AUTH_IDENTITY auth = GetWinNTAuthIdentity(domainName, userName, password);
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
    }
}
