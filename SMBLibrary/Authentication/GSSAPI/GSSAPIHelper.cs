/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using Utilities;

namespace SMBLibrary.Authentication.GSSAPI
{
    public class GSSAPIHelper
    {
        public static readonly byte[] NTLMSSPIdentifier = new byte[] { 0x2b, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x02, 0x02, 0x0a };

        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/ms995330.aspx
        /// </summary>
        public static byte[] GetNTLMSSPMessage(byte[] tokenBytes)
        {
            SimpleProtectedNegotiationToken token = SimpleProtectedNegotiationToken.ReadToken(tokenBytes, 0);
            if (token != null)
            {
                if (token is SimpleProtectedNegotiationTokenInit)
                {
                    SimpleProtectedNegotiationTokenInit tokenInit = (SimpleProtectedNegotiationTokenInit)token;
                    foreach (byte[] identifier in tokenInit.MechanismTypeList)
                    {
                        if (ByteUtils.AreByteArraysEqual(identifier, NTLMSSPIdentifier))
                        {
                            return tokenInit.MechanismToken;
                        }
                    }
                }
                else
                {
                    SimpleProtectedNegotiationTokenResponse tokenResponse = (SimpleProtectedNegotiationTokenResponse)token;
                    return tokenResponse.ResponseToken;
                }
            }
            return null;
        }

        public static byte[] GetGSSTokenInitNTLMSSPBytes()
        {
            SimpleProtectedNegotiationTokenInit token = new SimpleProtectedNegotiationTokenInit();
            token.MechanismTypeList = new List<byte[]>();
            token.MechanismTypeList.Add(NTLMSSPIdentifier);
            return SimpleProtectedNegotiationToken.GetTokenBytes(token);
        }

        public static byte[] GetGSSTokenResponseBytesFromNTLMSSPMessage(byte[] messageBytes)
        {
            SimpleProtectedNegotiationTokenResponse token = new SimpleProtectedNegotiationTokenResponse();
            token.NegState = NegState.AcceptIncomplete;
            token.SupportedMechanism = NTLMSSPIdentifier;
            token.ResponseToken = messageBytes;
            return token.GetBytes();
        }

        public static byte[] GetGSSTokenAcceptCompletedResponse()
        {
            SimpleProtectedNegotiationTokenResponse token = new SimpleProtectedNegotiationTokenResponse();
            token.NegState = NegState.AcceptCompleted;
            return token.GetBytes();
        }
    }
}
