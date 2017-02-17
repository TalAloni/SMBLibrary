/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.Authentication.GSSAPI;

namespace SMBLibrary.Authentication.NTLM
{
    public abstract class NTLMAuthenticationProviderBase
    {
        public abstract Win32Error GetChallengeMessage(out object context, NegotiateMessage negotiateMessage, out ChallengeMessage challengeMessage);

        public abstract Win32Error Authenticate(object context, AuthenticateMessage authenticateMessage);

        public abstract void DeleteSecurityContext(ref object context);

        public abstract object GetContextAttribute(object context, GSSAttributeName attributeName);
    }
}
