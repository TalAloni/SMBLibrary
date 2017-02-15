/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace SMBLibrary.Authentication.GSSAPI
{
    public abstract class SimpleProtectedNegotiationToken
    {
        public const byte ApplicationTag = 0x60;

        public static readonly byte[] SPNEGOIdentifier = new byte[] { 0x2b, 0x06, 0x01, 0x05, 0x05, 0x02 };

        public abstract byte[] GetBytes();

        /// <summary>
        /// https://tools.ietf.org/html/rfc2743
        /// </summary>
        public static SimpleProtectedNegotiationToken ReadToken(byte[] tokenBytes, int offset)
        {
            byte tag = ByteReader.ReadByte(tokenBytes, ref offset);
            if (tag == ApplicationTag)
            {
                // when an InitToken is sent, it is prepended by an Application Constructed Object specifier (0x60),
                // and the OID for SPNEGO (see value in OID table above). This is the generic GSSAPI header.
                int tokenLength = DerEncodingHelper.ReadLength(tokenBytes, ref offset);
                tag = ByteReader.ReadByte(tokenBytes, ref offset);
                if (tag == (byte)DerEncodingTag.ObjectIdentifier)
                {
                    int objectIdentifierLength = DerEncodingHelper.ReadLength(tokenBytes, ref offset);
                    byte[] objectIdentifier = ByteReader.ReadBytes(tokenBytes, ref offset, objectIdentifierLength);
                    if (ByteUtils.AreByteArraysEqual(objectIdentifier, SPNEGOIdentifier))
                    {
                        tag = ByteReader.ReadByte(tokenBytes, ref offset);
                        if (tag == SimpleProtectedNegotiationTokenInit.NegTokenInitTag)
                        {
                            return new SimpleProtectedNegotiationTokenInit(tokenBytes, offset);
                        }
                    }
                }
            }
            else if (tag == SimpleProtectedNegotiationTokenResponse.NegTokenRespTag)
            {
                return new SimpleProtectedNegotiationTokenResponse(tokenBytes, offset);
            }
            return null;
        }

        /// <summary>
        /// Will append the generic GSSAPI header.
        /// </summary>
        public static byte[] GetTokenBytes(SimpleProtectedNegotiationToken token)
        {
            if (token is SimpleProtectedNegotiationTokenInit)
            {
                byte[] tokenBytes = token.GetBytes();
                int objectIdentifierFieldSize = DerEncodingHelper.GetLengthFieldSize(SPNEGOIdentifier.Length);
                int tokenLength = 1 + objectIdentifierFieldSize + SPNEGOIdentifier.Length + tokenBytes.Length;
                int tokenLengthFieldSize = DerEncodingHelper.GetLengthFieldSize(tokenLength);
                int headerLength = 1 + tokenLengthFieldSize + 1 + objectIdentifierFieldSize + SPNEGOIdentifier.Length;
                byte[] buffer = new byte[headerLength + tokenBytes.Length];
                int offset = 0;
                ByteWriter.WriteByte(buffer, ref offset, ApplicationTag);
                DerEncodingHelper.WriteLength(buffer, ref offset, tokenLength);
                ByteWriter.WriteByte(buffer, ref offset, (byte)DerEncodingTag.ObjectIdentifier);
                DerEncodingHelper.WriteLength(buffer, ref offset, SPNEGOIdentifier.Length);
                ByteWriter.WriteBytes(buffer, ref offset, SPNEGOIdentifier);
                ByteWriter.WriteBytes(buffer, ref offset, tokenBytes);
                return buffer;
            }
            else
            {
                return token.GetBytes();
            }
        }
    }
}
