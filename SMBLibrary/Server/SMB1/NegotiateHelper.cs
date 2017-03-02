/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using SMBLibrary.Authentication.GSSAPI;
using SMBLibrary.Authentication.NTLM;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    /// <summary>
    /// Negotiate helper
    /// </summary>
    public class NegotiateHelper
    {
        internal static NegotiateResponse GetNegotiateResponse(SMB1Header header, NegotiateRequest request, GSSProvider securityProvider, ConnectionState state)
        {
            NegotiateResponse response = new NegotiateResponse();

            response.DialectIndex = (ushort)request.Dialects.IndexOf(SMBServer.NTLanManagerDialect);
            response.SecurityMode = SecurityMode.UserSecurityMode | SecurityMode.EncryptPasswords;
            response.MaxMpxCount = 50;
            response.MaxNumberVcs = 1;
            response.MaxBufferSize = 16644;
            response.MaxRawSize = 65536;
            response.Capabilities = ServerCapabilities.Unicode |
                                    ServerCapabilities.LargeFiles |
                                    ServerCapabilities.NTSMB |
                                    ServerCapabilities.NTStatusCode |
                                    ServerCapabilities.NTFind |
                                    ServerCapabilities.LargeRead |
                                    ServerCapabilities.LargeWrite;
            response.SystemTime = DateTime.UtcNow;
            response.ServerTimeZone = (short)-TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
            NegotiateMessage negotiateMessage = CreateNegotiateMessage();
            ChallengeMessage challengeMessage;
            NTStatus status = securityProvider.GetNTLMChallengeMessage(out state.AuthenticationContext, negotiateMessage, out challengeMessage);
            if (status == NTStatus.SEC_I_CONTINUE_NEEDED)
            {
                response.Challenge = challengeMessage.ServerChallenge;
            }
            response.DomainName = String.Empty;
            response.ServerName = String.Empty;

            return response;
        }

        internal static NegotiateResponseExtended GetNegotiateResponseExtended(NegotiateRequest request, Guid serverGuid)
        {
            NegotiateResponseExtended response = new NegotiateResponseExtended();
            response.DialectIndex = (ushort)request.Dialects.IndexOf(SMBServer.NTLanManagerDialect);
            response.SecurityMode = SecurityMode.UserSecurityMode | SecurityMode.EncryptPasswords;
            response.MaxMpxCount = 50;
            response.MaxNumberVcs = 1;
            response.MaxBufferSize = 16644;
            response.MaxRawSize = 65536;
            response.Capabilities = ServerCapabilities.Unicode |
                                    ServerCapabilities.LargeFiles |
                                    ServerCapabilities.NTSMB |
                                    ServerCapabilities.NTStatusCode |
                                    ServerCapabilities.NTFind |
                                    ServerCapabilities.LargeRead |
                                    ServerCapabilities.LargeWrite |
                                    ServerCapabilities.ExtendedSecurity;
            response.SystemTime = DateTime.UtcNow;
            response.ServerTimeZone = (short)-TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
            response.ServerGuid = serverGuid;

            return response;
        }

        private static NegotiateMessage CreateNegotiateMessage()
        {
            NegotiateMessage negotiateMessage = new NegotiateMessage();
            negotiateMessage.NegotiateFlags = NegotiateFlags.UnicodeEncoding |
                                              NegotiateFlags.OEMEncoding |
                                              NegotiateFlags.Sign |
                                              NegotiateFlags.LanManagerKey |
                                              NegotiateFlags.NTLMSessionSecurity |
                                              NegotiateFlags.AlwaysSign |
                                              NegotiateFlags.Version |
                                              NegotiateFlags.Use128BitEncryption |
                                              NegotiateFlags.Use56BitEncryption;
            negotiateMessage.Version = NTLMVersion.Server2003;
            return negotiateMessage;
        }
    }
}
