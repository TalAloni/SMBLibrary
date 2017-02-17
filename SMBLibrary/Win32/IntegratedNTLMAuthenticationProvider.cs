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
using System.Runtime.InteropServices;
using Utilities;
using SMBLibrary.Authentication.GSSAPI;
using SMBLibrary.Authentication.NTLM;
using Microsoft.Win32;

namespace SMBLibrary.Win32.Security
{
    public class IntegratedNTLMAuthenticationProvider : NTLMAuthenticationProviderBase
    {
        public class AuthContext
        {
            public SecHandle ServerContext;
            public string WorkStation;
            public string UserName;
            public byte[] SessionKey;
            public bool IsGuest;

            public AuthContext(SecHandle serverContext, string workStation)
            {
                ServerContext = serverContext;
                WorkStation = workStation;
            }
        }

        public override Win32Error GetChallengeMessage(out object context, NegotiateMessage negotiateMessage, out ChallengeMessage challengeMessage)
        {
            byte[] negotiateMessageBytes = negotiateMessage.GetBytes();
            SecHandle serverContext;
            byte[] challengeMessageBytes;
            try
            {
                challengeMessageBytes = SSPIHelper.GetType2Message(negotiateMessageBytes, out serverContext);
            }
            catch (Exception)
            {
                context = null;
                challengeMessage = null;
                return (Win32Error)Marshal.GetLastWin32Error();
            }

            context = new AuthContext(serverContext, negotiateMessage.Workstation);
            challengeMessage = new ChallengeMessage(challengeMessageBytes);
            return Win32Error.ERROR_SUCCESS;
        }

        /// <summary>
        /// Authenticate will return false when the password is correct in these cases:
        /// 1. The correct password is blank and 'limitblankpassworduse' is set to 1.
        /// 2. The user is listed in the "Deny access to this computer from the network" list.
        /// </summary>
        public override Win32Error Authenticate(object context, AuthenticateMessage message)
        {
            AuthContext authContext = context as AuthContext;
            if (authContext == null)
            {
                return Win32Error.ERROR_NO_TOKEN;
            }

            authContext.UserName = message.UserName;
            authContext.SessionKey = message.EncryptedRandomSessionKey;
            if ((message.NegotiateFlags & NegotiateFlags.Anonymous) > 0)
            {
                if (this.EnableGuestLogin)
                {
                    authContext.IsGuest = true;
                    return Win32Error.ERROR_SUCCESS;
                }
                else
                {
                    return Win32Error.ERROR_LOGON_FAILURE;
                }
            }

            byte[] messageBytes = message.GetBytes();
            bool success;
            try
            {
                success = SSPIHelper.AuthenticateType3Message(authContext.ServerContext, messageBytes);
            }
            catch (Exception)
            {
                return (Win32Error)Marshal.GetLastWin32Error();
            }

            if (success)
            {
                return Win32Error.ERROR_SUCCESS;
            }
            else
            {
                Win32Error result = (Win32Error)Marshal.GetLastWin32Error();
                // Windows will permit fallback when these conditions are met:
                // 1. The guest user account is enabled.
                // 2. The guest user account does not have a password set.
                // 3. The specified account does not exist.
                //    OR:
                //    The password is correct but 'limitblankpassworduse' is set to 1 (logon over a network is disabled for accounts without a password).
                bool allowFallback = (!IsUserExists(message.UserName) || result == Win32Error.ERROR_ACCOUNT_RESTRICTION);
                if (allowFallback && this.EnableGuestLogin)
                {
                    authContext.IsGuest = true;
                    return Win32Error.ERROR_SUCCESS;
                }
                else
                {
                    return result;
                }
            }
        }

        public override void DeleteSecurityContext(ref object context)
        {
            AuthContext authContext = context as AuthContext;
            if (authContext == null)
            {
                return;
            }

            SecHandle handle = ((AuthContext)context).ServerContext;
            SSPIHelper.DeleteSecurityContext(ref handle);
        }

        public override object GetContextAttribute(object context, GSSAttributeName attributeName)
        {
            AuthContext authContext = context as AuthContext;
            if (authContext != null)
            {
                switch (attributeName)
                {
                    case GSSAttributeName.AccessToken:
                        return authContext.ServerContext;
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

        public static bool IsUserExists(string userName)
        {
            return NetworkAPI.IsUserExists(userName);
        }
    }
}
