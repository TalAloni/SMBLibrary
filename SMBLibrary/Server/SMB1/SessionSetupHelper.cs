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
    /// Session Setup helper
    /// </summary>
    public class SessionSetupHelper
    {
        internal static SMB1Command GetSessionSetupResponse(SMB1Header header, SessionSetupAndXRequest request, INTLMAuthenticationProvider users, SMB1ConnectionState state)
        {
            SessionSetupAndXResponse response = new SessionSetupAndXResponse();
            // The PrimaryDomain field in the request is used to determine with domain controller should authenticate the user credentials,
            // However, the domain controller itself does not use this field.
            // See: http://msdn.microsoft.com/en-us/library/windows/desktop/aa378749%28v=vs.85%29.aspx
            User user;
            try
            {
                user = users.Authenticate(request.AccountName, request.OEMPassword, request.UnicodePassword);
            }
            catch (EmptyPasswordNotAllowedException)
            {
                header.Status = NTStatus.STATUS_ACCOUNT_RESTRICTION;
                return new ErrorResponse(CommandName.SMB_COM_SESSION_SETUP_ANDX);
            }

            if (user != null)
            {
                ushort? userID = state.AddConnectedUser(user.AccountName);
                if (!userID.HasValue)
                {
                    header.Status = NTStatus.STATUS_TOO_MANY_SESSIONS;
                    return new ErrorResponse(CommandName.SMB_COM_SESSION_SETUP_ANDX);
                }
                header.UID = userID.Value;
                response.PrimaryDomain = request.PrimaryDomain;
            }
            else if (users.FallbackToGuest(user.AccountName))
            {
                ushort? userID = state.AddConnectedUser("Guest");
                if (!userID.HasValue)
                {
                    header.Status = NTStatus.STATUS_TOO_MANY_SESSIONS;
                    return new ErrorResponse(CommandName.SMB_COM_SESSION_SETUP_ANDX);
                }
                header.UID = userID.Value;
                response.Action = SessionSetupAction.SetupGuest;
                response.PrimaryDomain = request.PrimaryDomain;
            }
            else
            {
                header.Status = NTStatus.STATUS_LOGON_FAILURE;
                return new ErrorResponse(CommandName.SMB_COM_SESSION_SETUP_ANDX);
            }
            if ((request.Capabilities & ServerCapabilities.LargeRead) > 0)
            {
                state.LargeRead = true;
            }
            if ((request.Capabilities & ServerCapabilities.LargeWrite) > 0)
            {
                state.LargeWrite = true;
            }
            response.NativeOS = String.Empty; // "Windows Server 2003 3790 Service Pack 2"
            response.NativeLanMan = String.Empty; // "Windows Server 2003 5.2"

            return response;
        }

        internal static SMB1Command GetSessionSetupResponseExtended(SMB1Header header, SessionSetupAndXRequestExtended request, INTLMAuthenticationProvider users, SMB1ConnectionState state)
        {
            SessionSetupAndXResponseExtended response = new SessionSetupAndXResponseExtended();

            // [MS-SMB] The Windows GSS implementation supports raw Kerberos / NTLM messages in the SecurityBlob
            byte[] messageBytes = request.SecurityBlob;
            bool isRawMessage = true;
            if (!AuthenticationMessageUtils.IsSignatureValid(messageBytes))
            {
                messageBytes = GSSAPIHelper.GetNTLMSSPMessage(request.SecurityBlob);
                isRawMessage = false;
            }
            if (!AuthenticationMessageUtils.IsSignatureValid(messageBytes))
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
                return new ErrorResponse(CommandName.SMB_COM_SESSION_SETUP_ANDX);
            }

            MessageTypeName messageType = AuthenticationMessageUtils.GetMessageType(messageBytes);
            if (messageType == MessageTypeName.Negotiate)
            {
                byte[] challengeMessageBytes = users.GetChallengeMessageBytes(messageBytes);
                if (isRawMessage)
                {
                    response.SecurityBlob = challengeMessageBytes;
                }
                else
                {
                    response.SecurityBlob = GSSAPIHelper.GetGSSTokenResponseBytesFromNTLMSSPMessage(challengeMessageBytes);
                }
                header.Status = NTStatus.STATUS_MORE_PROCESSING_REQUIRED;
            }
            else // MessageTypeName.Authenticate
            {
                User user;
                try
                {
                    user = users.Authenticate(messageBytes);
                }
                catch (EmptyPasswordNotAllowedException)
                {
                    header.Status = NTStatus.STATUS_ACCOUNT_RESTRICTION;
                    return new ErrorResponse(CommandName.SMB_COM_SESSION_SETUP_ANDX);
                }

                if (user != null)
                {
                    ushort? userID = state.AddConnectedUser(user.AccountName);
                    if (!userID.HasValue)
                    {
                        header.Status = NTStatus.STATUS_TOO_MANY_SESSIONS;
                        return new ErrorResponse(CommandName.SMB_COM_SESSION_SETUP_ANDX);
                    }
                    header.UID = userID.Value;
                }
                else if (users.FallbackToGuest(user.AccountName))
                {
                    ushort? userID = state.AddConnectedUser("Guest");
                    if (!userID.HasValue)
                    {
                        header.Status = NTStatus.STATUS_TOO_MANY_SESSIONS;
                        return new ErrorResponse(CommandName.SMB_COM_SESSION_SETUP_ANDX);
                    }
                    header.UID = userID.Value;
                    response.Action = SessionSetupAction.SetupGuest;
                }
                else
                {
                    header.Status = NTStatus.STATUS_LOGON_FAILURE;
                    return new ErrorResponse(CommandName.SMB_COM_SESSION_SETUP_ANDX);
                }
            }
            response.NativeOS = String.Empty; // "Windows Server 2003 3790 Service Pack 2"
            response.NativeLanMan = String.Empty; // "Windows Server 2003 5.2"

            return response;
        }
    }
}
