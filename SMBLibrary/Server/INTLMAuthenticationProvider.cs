/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using SMBLibrary.Authentication.NTLM;

namespace SMBLibrary.Server
{
    public interface INTLMAuthenticationProvider
    {
        ChallengeMessage GetChallengeMessage(NegotiateMessage negotiateMessage);
        
        bool Authenticate(AuthenticateMessage authenticateMessage);

        /// <summary>
        /// Permit access to this user via the guest user account if the normal authentication process fails.
        /// </summary>
        /// <remarks>
        /// Windows will permit fallback when these conditions are met:
        /// 1. The guest user account is enabled.
        /// 2. The guest user account does not have a password set.
        /// 3. The specified account does not exist.
        ///    OR:
        ///    The password is correct but 'limitblankpassworduse' is set to 1 (logon over a network is disabled for accounts without a password).
        /// </remarks>
        bool FallbackToGuest(string userName);
    }
}
