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
    public abstract class NTLMAuthenticationProviderBase : IGSSMechanism
    {
        public static readonly byte[] NTLMSSPIdentifier = new byte[] { 0x2b, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x02, 0x02, 0x0a };

        public NTStatus AcceptSecurityContext(ref object context, byte[] inputToken, out byte[] outputToken)
        {
            outputToken = null;
            if (!AuthenticationMessageUtils.IsSignatureValid(inputToken))
            {
                return NTStatus.SEC_E_INVALID_TOKEN;
            }

            MessageTypeName messageType = AuthenticationMessageUtils.GetMessageType(inputToken);
            if (messageType == MessageTypeName.Negotiate)
            {
                NegotiateMessage input = new NegotiateMessage(inputToken);
                ChallengeMessage output;
                NTStatus status = GetChallengeMessage(out context, input, out output);
                outputToken = output.GetBytes();
                return status;
            }
            else if (messageType == MessageTypeName.Authenticate)
            {
                AuthenticateMessage message = new AuthenticateMessage(inputToken);
                return Authenticate(context, message);
            }
            else
            {
                return NTStatus.SEC_E_INVALID_TOKEN;
            }
        }

        public abstract NTStatus GetChallengeMessage(out object context, NegotiateMessage negotiateMessage, out ChallengeMessage challengeMessage);

        public abstract NTStatus Authenticate(object context, AuthenticateMessage authenticateMessage);

        public abstract bool DeleteSecurityContext(ref object context);

        public abstract object GetContextAttribute(object context, GSSAttributeName attributeName);

        public byte[] Identifier
        {
            get
            {
                return NTLMSSPIdentifier;
            }
        }
    }
}
