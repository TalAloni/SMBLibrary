/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.Authentication.NTLM;
using Utilities;

namespace SMBLibrary.Authentication.GSSAPI
{
    public class GSSProvider
    {
        public static readonly byte[] NTLMSSPIdentifier = new byte[] { 0x2b, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x02, 0x02, 0x0a };

        private List<IGSSMechanism> m_mechanisms;
        private Dictionary<object, IGSSMechanism> m_contextToMechanism = new Dictionary<object, IGSSMechanism>();

        public GSSProvider(IGSSMechanism mechanism)
        {
            m_mechanisms = new List<IGSSMechanism>();
            m_mechanisms.Add(mechanism);
        }

        public GSSProvider(List<IGSSMechanism> mechanisms)
        {
            m_mechanisms = mechanisms;
        }

        public byte[] GetSPNEGOTokenInitBytes()
        {
            SimpleProtectedNegotiationTokenInit token = new SimpleProtectedNegotiationTokenInit();
            token.MechanismTypeList = new List<byte[]>();
            foreach (IGSSMechanism mechanism in m_mechanisms)
            {
                token.MechanismTypeList.Add(mechanism.Identifier);
            }
            return SimpleProtectedNegotiationToken.GetTokenBytes(token);
        }

        public NTStatus AcceptSecurityContext(ref object context, byte[] inputToken, out byte[] outputToken)
        {
            outputToken = null;
            SimpleProtectedNegotiationToken spnegoToken = SimpleProtectedNegotiationToken.ReadToken(inputToken, 0);
            if (spnegoToken != null)
            {
                if (spnegoToken is SimpleProtectedNegotiationTokenInit)
                {
                    SimpleProtectedNegotiationTokenInit tokenInit = (SimpleProtectedNegotiationTokenInit)spnegoToken;
                    IGSSMechanism mechanism = FindMechanism(tokenInit.MechanismTypeList);
                    if (mechanism != null)
                    {
                        byte[] mechanismOutput;
                        NTStatus status = mechanism.AcceptSecurityContext(ref context, tokenInit.MechanismToken, out mechanismOutput);
                        outputToken = GetSPNEGOTokenResponseBytes(mechanismOutput, status, mechanism.Identifier);
                        m_contextToMechanism[context] = mechanism;
                        return status;
                    }
                    return NTStatus.SEC_E_SECPKG_NOT_FOUND;
                }
                else // SimpleProtectedNegotiationTokenResponse
                {
                    IGSSMechanism mechanism;
                    if (!m_contextToMechanism.TryGetValue(context, out mechanism))
                    {
                        // We assume that the problem is not with our implementation and that the client has sent
                        // SimpleProtectedNegotiationTokenResponse without first sending SimpleProtectedNegotiationTokenInit.
                        return NTStatus.SEC_E_INVALID_TOKEN;
                    }
                    SimpleProtectedNegotiationTokenResponse tokenResponse = (SimpleProtectedNegotiationTokenResponse)spnegoToken;
                    byte[] mechanismOutput;
                    NTStatus status = mechanism.AcceptSecurityContext(ref context, tokenResponse.ResponseToken, out mechanismOutput);
                    outputToken = GetSPNEGOTokenResponseBytes(mechanismOutput, status, null);
                    return status;
                }
            }
            else
            {
                // [MS-SMB] The Windows GSS implementation supports raw Kerberos / NTLM messages in the SecurityBlob.
                // [MS-SMB2] Windows [..] will also accept raw Kerberos messages and implicit NTLM messages as part of GSS authentication.
                if (AuthenticationMessageUtils.IsSignatureValid(inputToken))
                {
                    MessageTypeName messageType = AuthenticationMessageUtils.GetMessageType(inputToken);
                    IGSSMechanism ntlmAuthenticationProvider = FindMechanism(NTLMSSPIdentifier);
                    if (ntlmAuthenticationProvider != null)
                    {
                        NTStatus status = ntlmAuthenticationProvider.AcceptSecurityContext(ref context, inputToken, out outputToken);
                        if (messageType == MessageTypeName.Negotiate)
                        {
                            m_contextToMechanism[context] = ntlmAuthenticationProvider;
                        }
                        return status;
                    }
                    else
                    {
                        return NTStatus.SEC_E_SECPKG_NOT_FOUND;
                    }
                }
            }
            return NTStatus.SEC_E_INVALID_TOKEN;
        }

        public object GetContextAttribute(object context, GSSAttributeName attributeName)
        {
            IGSSMechanism mechanism;
            if (!m_contextToMechanism.TryGetValue(context, out mechanism))
            {
                return null;
            }
            return mechanism.GetContextAttribute(context, attributeName);
        }

        public bool DeleteSecurityContext(ref object context)
        {
            bool result = false;
            if (context != null)
            {
                IGSSMechanism mechanism;
                if (m_contextToMechanism.TryGetValue(context, out mechanism))
                {
                    object contextReference = context;
                    result = mechanism.DeleteSecurityContext(ref context);
                    if (result)
                    {
                        m_contextToMechanism.Remove(contextReference);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Helper method for legacy implementation.
        /// </summary>
        public NTStatus GetNTLMChallengeMessage(out object context, NegotiateMessage negotiateMessage, out ChallengeMessage challengeMessage)
        {
            context = null;
            challengeMessage = null;
            IGSSMechanism ntlmAuthenticationProvider = FindMechanism(NTLMSSPIdentifier);
            if (ntlmAuthenticationProvider != null)
            {
                byte[] outputToken;
                NTStatus result = ntlmAuthenticationProvider.AcceptSecurityContext(ref context, negotiateMessage.GetBytes(), out outputToken);
                challengeMessage = new ChallengeMessage(outputToken);
                m_contextToMechanism.Add(context, ntlmAuthenticationProvider);
                return result;
            }
            else
            {
                return NTStatus.SEC_E_SECPKG_NOT_FOUND;
            }
        }

        /// <summary>
        /// Helper method for legacy implementation.
        /// </summary>
        public NTStatus NTLMAuthenticate(object context, AuthenticateMessage authenticateMessage)
        {
            IGSSMechanism ntlmAuthenticationProvider = FindMechanism(NTLMSSPIdentifier);
            if (ntlmAuthenticationProvider != null)
            {
                byte[] outputToken;
                NTStatus result = ntlmAuthenticationProvider.AcceptSecurityContext(ref context, authenticateMessage.GetBytes(), out outputToken);
                return result;
            }
            else
            {
                return NTStatus.SEC_E_SECPKG_NOT_FOUND;
            }
        }

        public IGSSMechanism FindMechanism(List<byte[]> mechanismIdentifiers)
        {
            foreach (byte[] identifier in mechanismIdentifiers)
            {
                IGSSMechanism mechanism = FindMechanism(identifier);
                if (mechanism != null)
                {
                    return mechanism;
                }
            }
            return null;
        }

        public IGSSMechanism FindMechanism(byte[] mechanismIdentifier)
        {
            foreach (IGSSMechanism mechanism in m_mechanisms)
            {
                if (ByteUtils.AreByteArraysEqual(mechanism.Identifier, mechanismIdentifier))
                {
                    return mechanism;
                }
            }
            return null;
        }

        private static byte[] GetSPNEGOTokenResponseBytes(byte[] mechanismOutput, NTStatus status, byte[] mechanismIdentifier)
        {
            SimpleProtectedNegotiationTokenResponse tokenResponse = new SimpleProtectedNegotiationTokenResponse();
            if (status == NTStatus.STATUS_SUCCESS)
            {
                tokenResponse.NegState = NegState.AcceptCompleted;
            }
            else if (status == NTStatus.SEC_I_CONTINUE_NEEDED)
            {
                tokenResponse.NegState = NegState.AcceptIncomplete;
            }
            else
            {
                tokenResponse.NegState = NegState.Reject;
            }
            tokenResponse.SupportedMechanism = mechanismIdentifier;
            tokenResponse.ResponseToken = mechanismOutput;
            return tokenResponse.GetBytes();
        }
    }
}
