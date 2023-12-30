/* Copyright (C) 2017-2023 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using SMBLibrary.Authentication.GSSAPI;
using System.Collections.Generic;
using Utilities;

namespace SMBLibrary.Client.Authentication
{
    public class NTLMAuthenticationClient : IAuthenticationClient
    {
        private string m_domainName;
        private string m_userName;
        private string m_password;
        private string m_spn;
        private byte[] m_sessionKey;
        private AuthenticationMethod m_authenticationMethod;

        private bool m_isNegotiationMessageAcquired = false;

        public NTLMAuthenticationClient(string domainName, string userName, string password, string spn, AuthenticationMethod authenticationMethod)
        {
            m_domainName = domainName;
            m_userName = userName;
            m_password = password;
            m_spn = spn;
            m_authenticationMethod = authenticationMethod;
        }

        public byte[] InitializeSecurityContext(byte[] securityBlob)
        {
            if (!m_isNegotiationMessageAcquired)
            {
                m_isNegotiationMessageAcquired = true;
                return GetNegotiateMessage(securityBlob);
            }
            else
            {
                return GetAuthenticateMessage(securityBlob);
            }
        }

        protected virtual byte[] GetNegotiateMessage(byte[] securityBlob)
        {
            bool useGSSAPI = false;
            if (securityBlob.Length > 0)
            {
                SimpleProtectedNegotiationTokenInit spnegoToken = null;
                try
                {
                    spnegoToken = SimpleProtectedNegotiationToken.ReadToken(securityBlob, 0, true) as SimpleProtectedNegotiationTokenInit;
                }
                catch
                {
                }

                if (spnegoToken == null || !ContainsMechanism(spnegoToken, GSSProvider.NTLMSSPIdentifier))
                {
                    return null;
                }
                useGSSAPI = true;
            }

            byte[] negotiateMessageBytes = NTLMAuthenticationHelper.GetNegotiateMessage(m_domainName, m_userName, m_password, m_authenticationMethod);
            if (useGSSAPI)
            {
                SimpleProtectedNegotiationTokenInit outputToken = new SimpleProtectedNegotiationTokenInit();
                outputToken.MechanismTypeList = new List<byte[]>();
                outputToken.MechanismTypeList.Add(GSSProvider.NTLMSSPIdentifier);
                outputToken.MechanismToken = negotiateMessageBytes;
                return outputToken.GetBytes(true);
            }
            else
            {
                return negotiateMessageBytes;
            }
        }

        protected virtual byte[] GetAuthenticateMessage(byte[] securityBlob)
        {
            bool useGSSAPI = false;
            SimpleProtectedNegotiationTokenResponse spnegoToken = null;
            try
            {
                spnegoToken = SimpleProtectedNegotiationToken.ReadToken(securityBlob, 0, false) as SimpleProtectedNegotiationTokenResponse;
            }
            catch
            {
            }

            byte[] challengeMessageBytes;
            if (spnegoToken != null)
            {
                challengeMessageBytes = spnegoToken.ResponseToken;
                useGSSAPI = true;
            }
            else
            {
                challengeMessageBytes = securityBlob;
            }

            byte[] authenticateMessageBytes = NTLMAuthenticationHelper.GetAuthenticateMessage(challengeMessageBytes, m_domainName, m_userName, m_password, m_spn, m_authenticationMethod, out m_sessionKey);
            if (useGSSAPI && authenticateMessageBytes != null)
            {
                SimpleProtectedNegotiationTokenResponse outputToken = new SimpleProtectedNegotiationTokenResponse();
                outputToken.ResponseToken = authenticateMessageBytes;
                return outputToken.GetBytes();
            }
            else
            {
                return authenticateMessageBytes;
            }
        }

        public virtual byte[] GetSessionKey()
        {
            return m_sessionKey;
        }

        private static bool ContainsMechanism(SimpleProtectedNegotiationTokenInit token, byte[] mechanismIdentifier)
        {
            for (int index = 0; index < token.MechanismTypeList.Count; index++)
            {
                if (ByteUtils.AreByteArraysEqual(token.MechanismTypeList[index], mechanismIdentifier))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
