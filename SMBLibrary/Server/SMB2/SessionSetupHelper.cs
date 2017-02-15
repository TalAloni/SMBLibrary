/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.Authentication;
using SMBLibrary.Authentication.NTLM;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Server.SMB2
{
    /// <summary>
    /// Session Setup helper
    /// </summary>
    public class SessionSetupHelper
    {
        internal static SMB2Command GetSessionSetupResponse(SessionSetupRequest request, INTLMAuthenticationProvider users, SMB2ConnectionState state)
        {
            // [MS-SMB2] Windows [..] will also accept raw Kerberos messages and implicit NTLM messages as part of GSS authentication.
            SessionSetupResponse response = new SessionSetupResponse();
            byte[] messageBytes = request.SecurityBuffer;
            bool isRawMessage = true;
            if (!AuthenticationMessageUtils.IsSignatureValid(messageBytes))
            {
                messageBytes = GSSAPIHelper.GetNTLMSSPMessage(request.SecurityBuffer);
                isRawMessage = false;
            }
            if (!AuthenticationMessageUtils.IsSignatureValid(messageBytes))
            {
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_NOT_SUPPORTED);
            }

            // According to [MS-SMB2] 3.3.5.5.3, response.Header.SessionID must be allocated if the server returns STATUS_MORE_PROCESSING_REQUIRED
            if (request.Header.SessionID == 0)
            {
                ulong? sessionID = state.AllocateSessionID();
                if (!sessionID.HasValue)
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_TOO_MANY_SESSIONS);
                }
                response.Header.SessionID = sessionID.Value;
            }

            MessageTypeName messageType = AuthenticationMessageUtils.GetMessageType(messageBytes);
            if (messageType == MessageTypeName.Negotiate)
            {
                NegotiateMessage negotiateMessage = new NegotiateMessage(messageBytes);
                ChallengeMessage challengeMessage = users.GetChallengeMessage(negotiateMessage);
                if (isRawMessage)
                {
                    response.SecurityBuffer = challengeMessage.GetBytes();
                }
                else
                {
                    response.SecurityBuffer = GSSAPIHelper.GetGSSTokenResponseBytesFromNTLMSSPMessage(challengeMessage.GetBytes());
                }
                response.Header.Status = NTStatus.STATUS_MORE_PROCESSING_REQUIRED;
            }
            else // MessageTypeName.Authenticate
            {
                AuthenticateMessage authenticateMessage = new AuthenticateMessage(messageBytes);
                bool loginSuccess;
                try
                {
                    loginSuccess = users.Authenticate(authenticateMessage);
                }
                catch (EmptyPasswordNotAllowedException)
                {
                    state.LogToServer(Severity.Information, "User '{0}' authentication using an empty password was rejected", authenticateMessage.UserName);
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_ACCOUNT_RESTRICTION);
                }

                if (loginSuccess)
                {
                    state.LogToServer(Severity.Information, "User '{0}' authenticated successfully", authenticateMessage.UserName);
                    state.CreateSession(request.Header.SessionID, authenticateMessage.UserName, authenticateMessage.WorkStation);
                }
                else if (users.FallbackToGuest(authenticateMessage.UserName))
                {
                    state.LogToServer(Severity.Information, "User '{0}' failed authentication. logged in as guest", authenticateMessage.UserName);
                    state.CreateSession(request.Header.SessionID, "Guest", authenticateMessage.WorkStation);
                    response.SessionFlags = SessionFlags.IsGuest;
                }
                else
                {
                    state.LogToServer(Severity.Information, "User '{0}' failed authentication", authenticateMessage.UserName);
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_LOGON_FAILURE);
                }

                if (!isRawMessage)
                {
                    response.SecurityBuffer = GSSAPIHelper.GetGSSTokenAcceptCompletedResponse();
                }
            }
            return response;
        }
    }
}
