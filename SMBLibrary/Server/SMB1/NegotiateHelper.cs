/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using SMBLibrary.Authentication;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    /// <summary>
    /// Negotiate helper
    /// </summary>
    public class NegotiateHelper
    {
        internal static NegotiateResponseNTLM GetNegotiateResponse(SMB1Header header, NegotiateRequest request, INTLMAuthenticationProvider users)
        {
            NegotiateResponseNTLM response = new NegotiateResponseNTLM();

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
            ChallengeMessage challengeMessage = users.GetChallengeMessage(CreateNegotiateMessage());
            response.Challenge = challengeMessage.ServerChallenge;
            response.DomainName = String.Empty;
            response.ServerName = String.Empty;

            return response;
        }

        internal static NegotiateResponseNTLMExtended GetNegotiateResponseExtended(NegotiateRequest request, Guid serverGuid)
        {
            NegotiateResponseNTLMExtended response = new NegotiateResponseNTLMExtended();
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
            negotiateMessage.NegotiateFlags = NegotiateFlags.NegotiateUnicode | NegotiateFlags.NegotiateOEM | NegotiateFlags.RequestTarget | NegotiateFlags.NegotiateSign | NegotiateFlags.NegotiateSeal | NegotiateFlags.NegotiateLanManagerKey | NegotiateFlags.NegotiateNTLMKey | NegotiateFlags.NegotiateAlwaysSign | NegotiateFlags.NegotiateVersion | NegotiateFlags.Negotiate128 | NegotiateFlags.Negotiate56;
            negotiateMessage.Version = Authentication.Version.Server2003;
            return negotiateMessage;
        }
    }
}
