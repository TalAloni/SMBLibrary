/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using Utilities;

namespace SMBLibrary.Authentication
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
                    List<TokenInitEntry> tokens = ((SimpleProtectedNegotiationTokenInit)token).Tokens;
                    foreach (TokenInitEntry entry in tokens)
                    {
                        foreach (byte[] identifier in entry.MechanismTypeList)
                        {
                            if (ByteUtils.AreByteArraysEqual(identifier, NTLMSSPIdentifier))
                            {
                                return entry.MechanismToken;
                            }
                        }
                    }
                }
                else
                {
                    List<TokenResponseEntry> tokens = ((SimpleProtectedNegotiationTokenResponse)token).Tokens;
                    if (tokens.Count > 0)
                    {
                        return tokens[0].ResponseToken;
                    }
                }
            }
            return null;
        }

        public static byte[] GetGSSTokenInitNTLMSSPBytes()
        {
            SimpleProtectedNegotiationTokenInit token = new SimpleProtectedNegotiationTokenInit();
            TokenInitEntry entry = new TokenInitEntry();
            entry.MechanismTypeList = new List<byte[]>();
            entry.MechanismTypeList.Add(NTLMSSPIdentifier);
            token.Tokens.Add(entry);
            return SimpleProtectedNegotiationToken.GetTokenBytes(token);
        }

        public static byte[] GetGSSTokenResponseBytesFromNTLMSSPMessage(byte[] messageBytes)
        {
            SimpleProtectedNegotiationTokenResponse token = new SimpleProtectedNegotiationTokenResponse();
            TokenResponseEntry entry = new TokenResponseEntry();
            entry.NegState = NegState.AcceptIncomplete;
            entry.SupportedMechanism = NTLMSSPIdentifier;
            entry.ResponseToken = messageBytes;
            token.Tokens.Add(entry);
            return token.GetBytes();
        }

        public static byte[] GetGSSTokenAcceptCompletedResponse()
        {
            SimpleProtectedNegotiationTokenResponse token = new SimpleProtectedNegotiationTokenResponse();
            TokenResponseEntry entry = new TokenResponseEntry();
            entry.NegState = NegState.AcceptCompleted;
            token.Tokens.Add(entry);
            return token.GetBytes();
        }
    }
}
