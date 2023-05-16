/* Copyright (C) 2017-2022 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.Authentication.GSSAPI;
using SMBLibrary.Authentication.NTLM;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Server.SMB2
{
    /// <summary>
    /// Session Setup helper
    /// </summary>
    internal class SessionSetupHelper
    {
        internal static SMB2Command GetSessionSetupResponse(SessionSetupRequest request, GSSProvider securityProvider, SMB2ConnectionState state)
        {
            // [MS-SMB2] Windows [..] will also accept raw Kerberos messages and implicit NTLM messages as part of GSS authentication.
            SessionSetupResponse response = new SessionSetupResponse();
            byte[] outputToken;
            NTStatus status = securityProvider.AcceptSecurityContext(ref state.AuthenticationContext, request.SecurityBuffer, out outputToken);
            if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.SEC_I_CONTINUE_NEEDED)
            {
                string userName = securityProvider.GetContextAttribute(state.AuthenticationContext, GSSAttributeName.UserName) as string;
                string domainName = securityProvider.GetContextAttribute(state.AuthenticationContext, GSSAttributeName.DomainName) as string;
                string machineName = securityProvider.GetContextAttribute(state.AuthenticationContext, GSSAttributeName.MachineName) as string;
                string osVersion = securityProvider.GetContextAttribute(state.AuthenticationContext, GSSAttributeName.OSVersion) as string;
                state.LogToServer(Severity.Information, "Session Setup: User '{0}' failed authentication (Domain: '{1}', Workstation: '{2}', OS version: '{3}'), NTStatus: {4}", userName, domainName, machineName, osVersion, status);
                return new ErrorResponse(request.CommandName, status);
            }

            if (outputToken != null)
            {
                response.SecurityBuffer = outputToken;
            }

            // According to [MS-SMB2] 3.3.5.5.3, response.Header.SessionID must be allocated if the server returns STATUS_MORE_PROCESSING_REQUIRED
            ulong sessionID = request.Header.SessionID;
            if (sessionID == 0)
            {
                ulong? allocatedSessionID = state.AllocateSessionID();
                if (!allocatedSessionID.HasValue)
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_TOO_MANY_SESSIONS);
                }
                sessionID = allocatedSessionID.Value;
                response.Header.SessionID = allocatedSessionID.Value;
            }
            else if (state.GetSession(sessionID) != null)
            {
                // We already have an established session associated with this sessionID, the client is in violation
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_REQUEST_NOT_ACCEPTED);
            }

            if (status == NTStatus.SEC_I_CONTINUE_NEEDED)
            {
                response.Header.Status = NTStatus.STATUS_MORE_PROCESSING_REQUIRED;
            }
            else // status == STATUS_SUCCESS
            {
                string userName = securityProvider.GetContextAttribute(state.AuthenticationContext, GSSAttributeName.UserName) as string;
                string domainName = securityProvider.GetContextAttribute(state.AuthenticationContext, GSSAttributeName.DomainName) as string;
                string machineName = securityProvider.GetContextAttribute(state.AuthenticationContext, GSSAttributeName.MachineName) as string;
                string osVersion = securityProvider.GetContextAttribute(state.AuthenticationContext, GSSAttributeName.OSVersion) as string;
                byte[] sessionKey = securityProvider.GetContextAttribute(state.AuthenticationContext, GSSAttributeName.SessionKey) as byte[];
                object accessToken = securityProvider.GetContextAttribute(state.AuthenticationContext, GSSAttributeName.AccessToken);
                bool? isGuest = securityProvider.GetContextAttribute(state.AuthenticationContext, GSSAttributeName.IsGuest) as bool?;

                if (sessionKey != null && sessionKey.Length > 16)
                {
                    // [MS-SMB2] 3.3.1.8 SessionKey MUST be set to the first 16 bytes of the cryptographic key queried from the GSS protocol for this authenticated context.
                    sessionKey = ByteReader.ReadBytes(sessionKey, 0, 16);
                }

                if (!isGuest.HasValue || !isGuest.Value)
                {
                    state.LogToServer(Severity.Information, "Session Setup: User '{0}' authenticated successfully (Domain: '{1}', Workstation: '{2}', OS version: '{3}').", userName, domainName, machineName, osVersion);
                    bool signingRequired = (request.SecurityMode & SecurityMode.SigningRequired) > 0;
                    SMB2Dialect smb2Dialect = SMBServer.ToSMB2Dialect(state.Dialect);
                    byte[] signingKey = SMB2Cryptography.GenerateSigningKey(sessionKey, smb2Dialect, null);
                    state.CreateSession(sessionID, userName, machineName, sessionKey, accessToken, signingRequired, signingKey);
                }
                else
                {
                    state.LogToServer(Severity.Information, "Session Setup: User '{0}' failed authentication (Domain: '{1}', Workstation: '{2}', OS version: '{3}'), logged in as guest.", userName, domainName, machineName, osVersion);
                    state.CreateSession(sessionID, "Guest", machineName, sessionKey, accessToken, false, null);
                    response.SessionFlags = SessionFlags.IsGuest;
                }
            }
            return response;
        }
    }
}
