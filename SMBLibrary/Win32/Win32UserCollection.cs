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

        public ChallengeMessage GetChallengeMessage(NegotiateMessage negotiateMessage)
        {
            byte[] negotiateMessageBytes = negotiateMessage.GetBytes();
            byte[] challengeMessageBytes = SSPIHelper.GetType2Message(negotiateMessageBytes, out m_serverContext);
            ChallengeMessage challengeMessage = new ChallengeMessage(challengeMessageBytes);
            m_serverChallenge = challengeMessage.ServerChallenge;
            return challengeMessage;
        }

        /// <summary>
        /// Authenticate will return false when the password is correct in these cases:
        /// 1. The correct password is blank and 'limitblankpassworduse' is set to 1.
        /// 2. The user is listed in the "Deny access to this computer from the network" list.
        /// </summary>
        public bool Authenticate(AuthenticateMessage message)
        {
            if ((message.NegotiateFlags & NegotiateFlags.Anonymous) > 0)
            {
                return this.EnableGuestLogin;
            }

            // AuthenticateType3Message is not reliable when 'limitblankpassworduse' is set to 1 and the user has an empty password set.
            // Note: Windows LogonUser API calls will be listed in the security event log.
            if (!AreEmptyPasswordsAllowed() &&
                IsPasswordEmpty(message) &&
                LoginAPI.HasEmptyPassword(message.UserName))
            {
                if (FallbackToGuest(message.UserName))
                {
                    return false;
                }
                else
                {
                    throw new EmptyPasswordNotAllowedException();
                }
            }

            byte[] messageBytes = message.GetBytes();
            bool success = SSPIHelper.AuthenticateType3Message(m_serverContext, messageBytes);
            return success;
        }

        public bool IsPasswordEmpty(AuthenticateMessage message)
        {
            // See [MS-NLMP] 3.3.1 - NTLM v1 Authentication
            // Special case for anonymous authentication:
            if (message.LmChallengeResponse.Length == 1 || message.NtChallengeResponse.Length == 0)
            {
                return true;
            }

            if ((message.NegotiateFlags & NegotiateFlags.ExtendedSecurity) > 0)
            {
                // NTLM v1 extended security:
                byte[] clientChallenge = ByteReader.ReadBytes(message.LmChallengeResponse, 0, 8);
                byte[] emptyPasswordNTLMv1Response = NTAuthentication.ComputeNTLMv1ExtendedSecurityResponse(m_serverChallenge, clientChallenge, String.Empty);
                if (ByteUtils.AreByteArraysEqual(emptyPasswordNTLMv1Response, message.NtChallengeResponse))
                {
                    return true;
                }

                // NTLM v2:
                byte[] _LMv2ClientChallenge = ByteReader.ReadBytes(message.LmChallengeResponse, 16, 8);
                byte[] emptyPasswordLMv2Response = NTAuthentication.ComputeLMv2Response(m_serverChallenge, _LMv2ClientChallenge, String.Empty, message.UserName, message.DomainName);
                if (ByteUtils.AreByteArraysEqual(emptyPasswordLMv2Response, message.LmChallengeResponse))
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
            }
            else
            {
                // NTLM v1:
                byte[] emptyPasswordLMv1Response = NTAuthentication.ComputeLMv1Response(m_serverChallenge, String.Empty);
                if (ByteUtils.AreByteArraysEqual(emptyPasswordLMv1Response, message.LmChallengeResponse))
                {
                    return true;
                }

                byte[] emptyPasswordNTLMv1Response = NTAuthentication.ComputeNTLMv1Response(m_serverChallenge, String.Empty);
                if (ByteUtils.AreByteArraysEqual(emptyPasswordNTLMv1Response, message.NtChallengeResponse))
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
        /// We immitate Windows, Guest logins are disabled in any of these cases:
        /// 1. The Guest account is disabled.
        /// 2. The Guest account has password set.
        /// 3. The Guest account is listed in the "deny access to this computer from the network" list.
        /// </summary>
        private bool EnableGuestLogin
        {
            get
            {
                return LoginAPI.ValidateUserPassword("Guest", String.Empty, LogonType.Network);
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
