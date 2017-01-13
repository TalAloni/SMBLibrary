/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Utilities;
using SMBLibrary.Authentication;
using SMBLibrary.Authentication.Win32;
using Microsoft.Win32;

namespace SMBLibrary.Server.Win32
{
    public class Win32UserCollection : UserCollection, INTLMAuthenticationProvider
    {
        private SecHandle m_serverContext;
        private byte[] m_serverChallenge = new byte[8];

        public Win32UserCollection()
        {
            List<string> users = NetworkAPI.EnumerateNetworkUsers();
            foreach (string user in users)
            {
                this.Add(new User(user, String.Empty));
            }
        }

        public byte[] GenerateServerChallenge()
        {
            NegotiateMessage negotiateMessage = new NegotiateMessage();
            negotiateMessage.NegotiateFlags = NegotiateFlags.NegotiateUnicode | NegotiateFlags.NegotiateOEM | NegotiateFlags.RequestTarget | NegotiateFlags.NegotiateSign | NegotiateFlags.NegotiateSeal | NegotiateFlags.NegotiateLanManagerKey | NegotiateFlags.NegotiateNTLMKey | NegotiateFlags.NegotiateAlwaysSign | NegotiateFlags.NegotiateVersion | NegotiateFlags.Negotiate128 | NegotiateFlags.Negotiate56;
            negotiateMessage.Version = Authentication.Version.Server2003;

            byte[] negotiateMessageBytes = negotiateMessage.GetBytes();
            byte[] challengeMessageBytes = SSPIHelper.GetType2Message(negotiateMessageBytes, out m_serverContext);
            ChallengeMessage challengeMessage = new ChallengeMessage(challengeMessageBytes);
            
            m_serverChallenge = challengeMessage.ServerChallenge;
            return m_serverChallenge;
        }

        public byte[] GetChallengeMessageBytes(byte[] negotiateMessageBytes)
        {
            byte[] challengeMessageBytes = SSPIHelper.GetType2Message(negotiateMessageBytes, out m_serverContext);
            ChallengeMessage message = new ChallengeMessage(challengeMessageBytes);
            m_serverChallenge = message.ServerChallenge;
            return challengeMessageBytes;
        }

        /// <summary>
        /// Note: The 'limitblankpassworduse' (Under HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa)
        /// will cause AcceptSecurityContext to return SEC_E_LOGON_DENIED when the correct password is blank.
        /// </summary>
        public User Authenticate(string accountNameToAuth, byte[] lmResponse, byte[] ntlmResponse)
        {
            if (accountNameToAuth == String.Empty ||
                (String.Equals(accountNameToAuth, "Guest", StringComparison.InvariantCultureIgnoreCase) && IsPasswordEmpty(lmResponse, ntlmResponse) && this.EnableGuestLogin))
            {
                int guestIndex = IndexOf("Guest");
                if (guestIndex >= 0)
                {
                    return this[guestIndex];
                }
                return null;
            }

            int index = IndexOf(accountNameToAuth);
            if (index >= 0)
            {
                // We should not spam the security event log, and should call the Windows LogonUser API
                // just to verify the user has a blank password.
                if (!AreEmptyPasswordsAllowed() &&
                    IsPasswordEmpty(lmResponse, ntlmResponse) &&
                    LoginAPI.HasEmptyPassword(accountNameToAuth))
                {
                    throw new EmptyPasswordNotAllowedException();
                }

                AuthenticateMessage authenticateMessage = new AuthenticateMessage();
                authenticateMessage.NegotiateFlags = NegotiateFlags.NegotiateUnicode | NegotiateFlags.NegotiateOEM | NegotiateFlags.RequestTarget | NegotiateFlags.NegotiateSign | NegotiateFlags.NegotiateSeal | NegotiateFlags.NegotiateLanManagerKey | NegotiateFlags.NegotiateNTLMKey | NegotiateFlags.NegotiateAlwaysSign | NegotiateFlags.NegotiateVersion | NegotiateFlags.Negotiate128 | NegotiateFlags.Negotiate56;
                authenticateMessage.UserName = accountNameToAuth;
                authenticateMessage.LmChallengeResponse = lmResponse;
                authenticateMessage.NtChallengeResponse = ntlmResponse;
                authenticateMessage.Version = Authentication.Version.Server2003;
                byte[] authenticateMessageBytes = authenticateMessage.GetBytes();

                bool success = SSPIHelper.AuthenticateType3Message(m_serverContext, authenticateMessageBytes);
                if (success)
                {
                    return this[index];
                }
            }
            return null;
        }

        /// <summary>
        /// Note: The 'limitblankpassworduse' (Under HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa)
        /// will cause AcceptSecurityContext to return SEC_E_LOGON_DENIED when the correct password is blank.
        /// </summary>
        public User Authenticate(byte[] authenticateMessageBytes)
        {
            AuthenticateMessage message = new AuthenticateMessage(authenticateMessageBytes);
            if ((message.NegotiateFlags & NegotiateFlags.NegotiateAnonymous) > 0 ||
                (String.Equals(message.UserName, "Guest", StringComparison.InvariantCultureIgnoreCase) && IsPasswordEmpty(message) && this.EnableGuestLogin))
            {
                int guestIndex = IndexOf("Guest");
                if (guestIndex >= 0)
                {
                    return this[guestIndex];
                }
                return null;
            }

            int index = IndexOf(message.UserName);
            if (index >= 0)
            {
                // We should not spam the security event log, and should call the Windows LogonUser API
                // just to verify the user has a blank password.
                if (!AreEmptyPasswordsAllowed() &&
                    IsPasswordEmpty(message) &&
                    LoginAPI.HasEmptyPassword(message.UserName))
                {
                    throw new EmptyPasswordNotAllowedException();
                }

                bool success = SSPIHelper.AuthenticateType3Message(m_serverContext, authenticateMessageBytes);
                if (success)
                {
                    return this[index];
                }
            }
            return null;
        }

        public bool IsPasswordEmpty(byte[] lmResponse, byte[] ntlmResponse)
        {
            // Special case for anonymous authentication
            // Windows NT4 SP6 will send 1 null byte OEMPassword and 0 bytes UnicodePassword for anonymous authentication
            if (lmResponse.Length == 0 || ByteUtils.AreByteArraysEqual(lmResponse, new byte[] { 0x00 }) || ntlmResponse.Length == 0)
            {
                return true;
            }

            byte[] emptyPasswordLMv1Response = NTAuthentication.ComputeLMv1Response(m_serverChallenge, String.Empty);
            if (ByteUtils.AreByteArraysEqual(emptyPasswordLMv1Response, lmResponse))
            {
                return true;
            }

            byte[] emptyPasswordNTLMv1Response = NTAuthentication.ComputeNTLMv1Response(m_serverChallenge, String.Empty);
            if (ByteUtils.AreByteArraysEqual(emptyPasswordNTLMv1Response, ntlmResponse))
            {
                return true;
            }

            return false;
        }

        public bool IsPasswordEmpty(AuthenticateMessage message)
        {
            // Special case for anonymous authentication, see [MS-NLMP] 3.3.1 - NTLM v1 Authentication
            if (message.LmChallengeResponse.Length == 1 || message.NtChallengeResponse.Length == 0)
            {
                return true;
            }

            byte[] clientChallenge = ByteReader.ReadBytes(message.LmChallengeResponse, 0, 8);
            byte[] emptyPasswordNTLMv1Response = NTAuthentication.ComputeNTLMv1ExtendedSecurityResponse(m_serverChallenge, clientChallenge, String.Empty);
            if (ByteUtils.AreByteArraysEqual(emptyPasswordNTLMv1Response, message.NtChallengeResponse))
            {
                return true;
            }

            if (message.NtChallengeResponse.Length > 24)
            {
                NTLMv2ClientChallengeStructure clientChallengeStructure = new NTLMv2ClientChallengeStructure(message.NtChallengeResponse, 16);
                byte[] clientChallengeStructurePadded = clientChallengeStructure.GetBytesPadded();
                byte[] emptyPasswordNTLMv2Response = NTAuthentication.ComputeNTLMv2Response(m_serverChallenge, clientChallengeStructurePadded, String.Empty, message.UserName, message.DomainName);
                if (ByteUtils.AreByteArraysEqual(emptyPasswordNTLMv2Response, message.NtChallengeResponse))
                {
                    return true;
                }
            }

            return false;
        }

        public bool FallbackToGuest(string userName)
        {
            return (EnableGuestLogin && (IndexOf(userName) == -1));
        }

        /// <summary>
        /// We immitate Windows, Guest logins are disabled when the guest account has password set
        /// </summary>
        private bool EnableGuestLogin
        {
            get
            {
                return (IndexOf("Guest") >= 0) && LoginAPI.HasEmptyPassword("Guest");
            }
        }

        public static bool AreEmptyPasswordsAllowed()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa");
            object value = key.GetValue("limitblankpassworduse", 1);
            if (value is int)
            {
                if ((int)value != 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
