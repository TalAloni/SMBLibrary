/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.Authentication.GSSAPI;
using Utilities;

namespace SMBLibrary.Authentication.NTLM
{
    public delegate string GetUserPassword(string userName);

    public class IndependentNTLMAuthenticationProvider : NTLMAuthenticationProviderBase
    {
        public class AuthContext
        {
            public string WorkStation;
            public byte[] ServerChallenge;
            public string UserName;
            public byte[] SessionKey;
            public bool IsGuest;

            public AuthContext(string workStation, byte[] serverChallenge)
            {
                WorkStation = workStation;
                ServerChallenge = serverChallenge;
            }
        }

        private GetUserPassword m_GetUserPassword;

        /// <param name="getUserPassword">
        /// The NTLM challenge response will be compared against the provided password.
        /// </param>
        public IndependentNTLMAuthenticationProvider(GetUserPassword getUserPassword)
        {
            m_GetUserPassword = getUserPassword;
        }

        public override NTStatus GetChallengeMessage(out object context, NegotiateMessage negotiateMessage, out ChallengeMessage challengeMessage)
        {
            byte[] serverChallenge = GenerateServerChallenge();
            context = new AuthContext(negotiateMessage.Workstation, serverChallenge);

            challengeMessage = new ChallengeMessage();
            // https://msdn.microsoft.com/en-us/library/cc236691.aspx
            challengeMessage.NegotiateFlags = NegotiateFlags.TargetTypeServer |
                                              NegotiateFlags.TargetInfo |
                                              NegotiateFlags.TargetNameSupplied |
                                              NegotiateFlags.Version;
            // [MS-NLMP] NTLMSSP_NEGOTIATE_NTLM MUST be set in the [..] CHALLENGE_MESSAGE to the client.
            challengeMessage.NegotiateFlags |= NegotiateFlags.NTLMSessionSecurity;

            if ((negotiateMessage.NegotiateFlags & NegotiateFlags.UnicodeEncoding) > 0)
            {
                challengeMessage.NegotiateFlags |= NegotiateFlags.UnicodeEncoding;
            }
            else if ((negotiateMessage.NegotiateFlags & NegotiateFlags.OEMEncoding) > 0)
            {
                challengeMessage.NegotiateFlags |= NegotiateFlags.OEMEncoding;
            }

            if ((negotiateMessage.NegotiateFlags & NegotiateFlags.ExtendedSessionSecurity) > 0)
            {
                challengeMessage.NegotiateFlags |= NegotiateFlags.ExtendedSessionSecurity;
            }
            else if ((negotiateMessage.NegotiateFlags & NegotiateFlags.LanManagerKey) > 0)
            {
                challengeMessage.NegotiateFlags |= NegotiateFlags.LanManagerKey;
            }

            if ((negotiateMessage.NegotiateFlags & NegotiateFlags.Sign) > 0)
            {
                // [MS-NLMP] If the client sends NTLMSSP_NEGOTIATE_SIGN to the server in the NEGOTIATE_MESSAGE,
                // the server MUST return NTLMSSP_NEGOTIATE_SIGN to the client in the CHALLENGE_MESSAGE.
                challengeMessage.NegotiateFlags |= NegotiateFlags.Sign;
            }

            if ((negotiateMessage.NegotiateFlags & NegotiateFlags.Seal) > 0)
            {
                // [MS-NLMP] If the client sends NTLMSSP_NEGOTIATE_SEAL to the server in the NEGOTIATE_MESSAGE,
                // the server MUST return NTLMSSP_NEGOTIATE_SEAL to the client in the CHALLENGE_MESSAGE.
                challengeMessage.NegotiateFlags |= NegotiateFlags.Seal;
            }

            if ((negotiateMessage.NegotiateFlags & NegotiateFlags.Sign) > 0 ||
                (negotiateMessage.NegotiateFlags & NegotiateFlags.Seal) > 0)
            {
                if ((negotiateMessage.NegotiateFlags & NegotiateFlags.Use56BitEncryption) > 0)
                {
                    // [MS-NLMP] If the client sends NTLMSSP_NEGOTIATE_SEAL or NTLMSSP_NEGOTIATE_SIGN with
                    // NTLMSSP_NEGOTIATE_56 to the server in the NEGOTIATE_MESSAGE, the server MUST return
                    // NTLMSSP_NEGOTIATE_56 to the client in the CHALLENGE_MESSAGE.
                    challengeMessage.NegotiateFlags |= NegotiateFlags.Use56BitEncryption;
                }
                if ((negotiateMessage.NegotiateFlags & NegotiateFlags.Use128BitEncryption) > 0)
                {
                    // [MS-NLMP] If the client sends NTLMSSP_NEGOTIATE_128 to the server in the NEGOTIATE_MESSAGE,
                    // the server MUST return NTLMSSP_NEGOTIATE_128 to the client in the CHALLENGE_MESSAGE only if
                    // the client sets NTLMSSP_NEGOTIATE_SEAL or NTLMSSP_NEGOTIATE_SIGN.
                    challengeMessage.NegotiateFlags |= NegotiateFlags.Use128BitEncryption;
                }
            }
            challengeMessage.TargetName = Environment.MachineName;
            challengeMessage.ServerChallenge = serverChallenge;
            challengeMessage.TargetInfo = AVPairUtils.GetAVPairSequence(Environment.MachineName, Environment.MachineName);
            challengeMessage.Version = NTLMVersion.Server2003;
            return NTStatus.SEC_I_CONTINUE_NEEDED;
        }

        public override NTStatus Authenticate(object context, AuthenticateMessage message)
        {
            AuthContext authContext = context as AuthContext;
            if (authContext == null)
            {
                // There are two possible reasons for authContext to be null:
                // 1. We have a bug in our implementation, let's assume that's not the case,
                //    according to [MS-SMB2] 3.3.5.5.1 we aren't allowed to return SEC_E_INVALID_HANDLE anyway.
                // 2. The client sent AuthenticateMessage without sending NegotiateMessage first,
                //    in this case the correct response is SEC_E_INVALID_TOKEN.
                return NTStatus.SEC_E_INVALID_TOKEN;
            }

            authContext.UserName = message.UserName;
            authContext.SessionKey = message.EncryptedRandomSessionKey;
            if ((message.NegotiateFlags & NegotiateFlags.Anonymous) > 0)
            {
                if (this.EnableGuestLogin)
                {
                    authContext.IsGuest = true;
                    return NTStatus.STATUS_SUCCESS;
                }
                else
                {
                    return NTStatus.STATUS_LOGON_FAILURE;
                }
            }

            string password = m_GetUserPassword(message.UserName);
            if (password == null)
            {
                if (this.EnableGuestLogin)
                {
                    authContext.IsGuest = true;
                    return NTStatus.STATUS_SUCCESS;
                }
                else
                {
                    return NTStatus.STATUS_LOGON_FAILURE;
                }
            }

            bool success;
            byte[] serverChallenge = authContext.ServerChallenge;
            if ((message.NegotiateFlags & NegotiateFlags.ExtendedSessionSecurity) > 0)
            {
                if (AuthenticationMessageUtils.IsNTLMv1ExtendedSecurity(message.LmChallengeResponse))
                {
                    // NTLM v1 Extended Security:
                    success = AuthenticateV1Extended(password, serverChallenge, message.LmChallengeResponse, message.NtChallengeResponse);
                }
                else
                {
                    // NTLM v2:
                    success = AuthenticateV2(message.DomainName, message.UserName, password, serverChallenge, message.LmChallengeResponse, message.NtChallengeResponse);
                }
            }
            else
            {
                success = AuthenticateV1(password, serverChallenge, message.LmChallengeResponse, message.NtChallengeResponse);
            }

            if (success)
            {
                return NTStatus.STATUS_SUCCESS;
            }
            else
            {
                return NTStatus.STATUS_LOGON_FAILURE;
            }
        }

        public override void DeleteSecurityContext(ref object context)
        {
        }

        public override object GetContextAttribute(object context, GSSAttributeName attributeName)
        {
            AuthContext authContext = context as AuthContext;
            if (authContext != null)
            {
                switch (attributeName)
                {
                    case GSSAttributeName.IsGuest:
                        return authContext.IsGuest;
                    case GSSAttributeName.MachineName:
                        return authContext.WorkStation;
                    case GSSAttributeName.SessionKey:
                        return authContext.SessionKey;
                    case GSSAttributeName.UserName:
                        return authContext.UserName;
                }
            }

            return null;
        }

        private bool EnableGuestLogin
        {
            get
            {
                return (m_GetUserPassword("Guest") == String.Empty);
            }
        }

        /// <summary>
        /// LM v1 / NTLM v1
        /// </summary>
        private static bool AuthenticateV1(string password, byte[] serverChallenge, byte[] lmResponse, byte[] ntResponse)
        {
            byte[] expectedLMResponse = NTLMCryptography.ComputeLMv1Response(serverChallenge, password);
            if (ByteUtils.AreByteArraysEqual(expectedLMResponse, lmResponse))
            {
                return true;
            }

            byte[] expectedNTResponse = NTLMCryptography.ComputeNTLMv1Response(serverChallenge, password);
            return ByteUtils.AreByteArraysEqual(expectedNTResponse, ntResponse);
        }

        /// <summary>
        /// LM v1 / NTLM v1 Extended Security
        /// </summary>
        private static bool AuthenticateV1Extended(string password, byte[] serverChallenge, byte[] lmResponse, byte[] ntResponse)
        {
            byte[] clientChallenge = ByteReader.ReadBytes(lmResponse, 0, 8);
            byte[] expectedNTLMv1Response = NTLMCryptography.ComputeNTLMv1ExtendedSecurityResponse(serverChallenge, clientChallenge, password);

            return ByteUtils.AreByteArraysEqual(expectedNTLMv1Response, ntResponse);
        }

        /// <summary>
        /// LM v2 / NTLM v2
        /// </summary>
        private bool AuthenticateV2(string domainName, string accountName, string password, byte[] serverChallenge, byte[] lmResponse, byte[] ntResponse)
        {
            byte[] _LMv2ClientChallenge = ByteReader.ReadBytes(lmResponse, 16, 8);
            byte[] expectedLMv2Response = NTLMCryptography.ComputeLMv2Response(serverChallenge, _LMv2ClientChallenge, password, accountName, domainName);
            if (ByteUtils.AreByteArraysEqual(expectedLMv2Response, lmResponse))
            {
                return true;
            }

            if (AuthenticationMessageUtils.IsNTLMv2NTResponse(ntResponse))
            {
                byte[] clientNTProof = ByteReader.ReadBytes(ntResponse, 0, 16);
                byte[] clientChallengeStructurePadded = ByteReader.ReadBytes(ntResponse, 16, ntResponse.Length - 16);
                byte[] expectedNTProof = NTLMCryptography.ComputeNTLMv2Proof(serverChallenge, clientChallengeStructurePadded, password, accountName, domainName);

                return ByteUtils.AreByteArraysEqual(clientNTProof, expectedNTProof);
            }
            return false;
        }

        /// <summary>
        /// Generate 8-byte server challenge
        /// </summary>
        private static byte[] GenerateServerChallenge()
        {
            byte[] serverChallenge = new byte[8];
            new Random().NextBytes(serverChallenge);
            return serverChallenge;
        }
    }
}
