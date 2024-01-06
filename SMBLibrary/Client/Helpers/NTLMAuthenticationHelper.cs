/* Copyright (C) 2017-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using SMBLibrary.Authentication.NTLM;
using Utilities;

namespace SMBLibrary.Client
{
    public class NTLMAuthenticationHelper
    {
        public static byte[] GetNegotiateMessage(string domainName, string userName, string password, AuthenticationMethod authenticationMethod)
        {
            NegotiateMessage negotiateMessage = new NegotiateMessage();
            negotiateMessage.NegotiateFlags = NegotiateFlags.UnicodeEncoding |
                                              NegotiateFlags.OEMEncoding |
                                              NegotiateFlags.Sign |
                                              NegotiateFlags.NTLMSessionSecurity |
                                              NegotiateFlags.DomainNameSupplied |
                                              NegotiateFlags.WorkstationNameSupplied |
                                              NegotiateFlags.AlwaysSign |
                                              NegotiateFlags.Version |
                                              NegotiateFlags.Use128BitEncryption |
                                              NegotiateFlags.Use56BitEncryption;

            if (!(userName == String.Empty && password == String.Empty))
            {
                negotiateMessage.NegotiateFlags |= NegotiateFlags.KeyExchange;
            }

            if (authenticationMethod == AuthenticationMethod.NTLMv1)
            {
                negotiateMessage.NegotiateFlags |= NegotiateFlags.LanManagerSessionKey;
            }
            else
            {
                negotiateMessage.NegotiateFlags |= NegotiateFlags.ExtendedSessionSecurity;
            }

            negotiateMessage.Version = NTLMVersion.Server2003;
            negotiateMessage.DomainName = domainName;
            negotiateMessage.Workstation = Environment.MachineName;
            return negotiateMessage.GetBytes();
        }

        public static byte[] GetAuthenticateMessage(byte[] negotiateMessageBytes, byte[] challengeMessageBytes, string domainName, string userName, string password, string spn, AuthenticationMethod authenticationMethod, out byte[] sessionKey)
        {
            sessionKey = null;

            ChallengeMessage challengeMessage = GetChallengeMessage(challengeMessageBytes);
            if (challengeMessage == null)
            {
                return null;
            }

            DateTime time = DateTime.UtcNow;
            byte[] clientChallenge = new byte[8];
            new Random().NextBytes(clientChallenge);

            AuthenticateMessage authenticateMessage = new AuthenticateMessage();
            // https://msdn.microsoft.com/en-us/library/cc236676.aspx
            authenticateMessage.NegotiateFlags = NegotiateFlags.Sign |
                                                 NegotiateFlags.NTLMSessionSecurity |
                                                 NegotiateFlags.AlwaysSign |
                                                 NegotiateFlags.Version |
                                                 NegotiateFlags.Use128BitEncryption |
                                                 NegotiateFlags.Use56BitEncryption;
            if ((challengeMessage.NegotiateFlags & NegotiateFlags.UnicodeEncoding) > 0)
            {
                authenticateMessage.NegotiateFlags |= NegotiateFlags.UnicodeEncoding;
            }
            else
            {
                authenticateMessage.NegotiateFlags |= NegotiateFlags.OEMEncoding;
            }

            if ((challengeMessage.NegotiateFlags & NegotiateFlags.KeyExchange) > 0)
            {
                authenticateMessage.NegotiateFlags |= NegotiateFlags.KeyExchange;
            }

            if (authenticationMethod == AuthenticationMethod.NTLMv1)
            {
                authenticateMessage.NegotiateFlags |= NegotiateFlags.LanManagerSessionKey;
            }
            else
            {
                authenticateMessage.NegotiateFlags |= NegotiateFlags.ExtendedSessionSecurity;
            }

            if (userName == String.Empty && password == String.Empty)
            {
                authenticateMessage.NegotiateFlags |= NegotiateFlags.Anonymous;
            }

            authenticateMessage.UserName = userName;
            authenticateMessage.DomainName = domainName;
            authenticateMessage.WorkStation = Environment.MachineName;
            byte[] sessionBaseKey;
            byte[] keyExchangeKey;
            if (authenticationMethod == AuthenticationMethod.NTLMv1 || authenticationMethod == AuthenticationMethod.NTLMv1ExtendedSessionSecurity)
            {
                // https://msdn.microsoft.com/en-us/library/cc236699.aspx
                if (userName == String.Empty && password == String.Empty)
                {
                    authenticateMessage.LmChallengeResponse = new byte[1];
                    authenticateMessage.NtChallengeResponse = new byte[0];
                }
                else if (authenticationMethod == AuthenticationMethod.NTLMv1)
                {
                    authenticateMessage.LmChallengeResponse = NTLMCryptography.ComputeLMv1Response(challengeMessage.ServerChallenge, password);
                    authenticateMessage.NtChallengeResponse = NTLMCryptography.ComputeNTLMv1Response(challengeMessage.ServerChallenge, password);
                }
                else // NTLMv1ExtendedSessionSecurity
                {
                    authenticateMessage.LmChallengeResponse = ByteUtils.Concatenate(clientChallenge, new byte[16]);
                    authenticateMessage.NtChallengeResponse = NTLMCryptography.ComputeNTLMv1ExtendedSessionSecurityResponse(challengeMessage.ServerChallenge, clientChallenge, password);
                }
                
                sessionBaseKey = new MD4().GetByteHashFromBytes(NTLMCryptography.NTOWFv1(password));
                byte[] lmowf = NTLMCryptography.LMOWFv1(password);
                keyExchangeKey = NTLMCryptography.KXKey(sessionBaseKey, authenticateMessage.NegotiateFlags, authenticateMessage.LmChallengeResponse, challengeMessage.ServerChallenge, lmowf);
            }
            else // NTLMv2
            {
                // https://msdn.microsoft.com/en-us/library/cc236700.aspx
                NTLMv2ClientChallenge clientChallengeStructure = new NTLMv2ClientChallenge(time, clientChallenge, challengeMessage.TargetInfo, spn);
                byte[] clientChallengeStructurePadded = clientChallengeStructure.GetBytesPadded();
                byte[] ntProofStr = NTLMCryptography.ComputeNTLMv2Proof(challengeMessage.ServerChallenge, clientChallengeStructurePadded, password, userName, domainName);
                if (userName == String.Empty && password == String.Empty)
                {
                    authenticateMessage.LmChallengeResponse = new byte[1];
                    authenticateMessage.NtChallengeResponse = new byte[0];
                }
                else
                {
                    authenticateMessage.LmChallengeResponse = NTLMCryptography.ComputeLMv2Response(challengeMessage.ServerChallenge, clientChallenge, password, userName, challengeMessage.TargetName);
                    authenticateMessage.NtChallengeResponse = ByteUtils.Concatenate(ntProofStr, clientChallengeStructurePadded);
                }
                
                byte[] responseKeyNT = NTLMCryptography.NTOWFv2(password, userName, domainName);
                sessionBaseKey = new HMACMD5(responseKeyNT).ComputeHash(ntProofStr);
                keyExchangeKey = sessionBaseKey;
            }

            authenticateMessage.Version = NTLMVersion.Server2003;

            // https://msdn.microsoft.com/en-us/library/cc236676.aspx
            if ((challengeMessage.NegotiateFlags & NegotiateFlags.KeyExchange) > 0)
            {
                sessionKey = new byte[16];
                new Random().NextBytes(sessionKey);
                authenticateMessage.EncryptedRandomSessionKey = RC4.Encrypt(keyExchangeKey, sessionKey);
            }
            else
            {
                sessionKey = keyExchangeKey;
            }

            authenticateMessage.CalculateMIC(sessionKey, negotiateMessageBytes, challengeMessageBytes);
            return authenticateMessage.GetBytes();
        }

        private static ChallengeMessage GetChallengeMessage(byte[] messageBytes)
        {
            if (AuthenticationMessageUtils.IsSignatureValid(messageBytes))
            {
                MessageTypeName messageType = AuthenticationMessageUtils.GetMessageType(messageBytes);
                if (messageType == MessageTypeName.Challenge)
                {
                    try
                    {
                        return new ChallengeMessage(messageBytes);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            return null;
        }
    }
}
