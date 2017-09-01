/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;
using SMBLibrary.Authentication.NTLM;

namespace SMBLibrary
{
    /// <summary>
    /// [MS-NLMP] Tests
    /// </summary>
    public class NTLMAuthenticationTests
    {
        public static bool LMv1HashTest()
        {
            byte[] hash = NTLMCryptography.LMOWFv1("Password");
            byte[] expected = new byte[] { 0xe5, 0x2c, 0xac, 0x67, 0x41, 0x9a, 0x9a, 0x22, 0x4a, 0x3b, 0x10, 0x8f, 0x3f, 0xa6, 0xcb, 0x6d };
            bool success = ByteUtils.AreByteArraysEqual(hash, expected);
            return success;
        }

        public static bool NTv1HashTest()
        {
            byte[] hash = NTLMCryptography.NTOWFv1("Password");
            byte[] expected = new byte[] { 0xa4, 0xf4, 0x9c, 0x40, 0x65, 0x10, 0xbd, 0xca, 0xb6, 0x82, 0x4e, 0xe7, 0xc3, 0x0f, 0xd8, 0x52 };
            bool success = ByteUtils.AreByteArraysEqual(hash, expected);
            return success;
        }

        public static bool NTv2HashTest()
        {
            byte[] hash = NTLMCryptography.NTOWFv2("Password", "User", "Domain");
            byte[] expected = new byte[] { 0x0c, 0x86, 0x8a, 0x40, 0x3b, 0xfd, 0x7a, 0x93, 0xa3, 0x00, 0x1e, 0xf2, 0x2e, 0xf0, 0x2e, 0x3f };
            bool success = ByteUtils.AreByteArraysEqual(hash, expected);
            return success;
        }

        public static bool LMv1ResponseTest()
        {
            byte[] challenge = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef };
            byte[] response = NTLMCryptography.ComputeLMv1Response(challenge, "Password");
            byte[] expected = { 0x98, 0xde, 0xf7, 0xb8, 0x7f, 0x88, 0xaa, 0x5d, 0xaf, 0xe2, 0xdf, 0x77, 0x96, 0x88, 0xa1, 0x72, 0xde, 0xf1, 0x1c, 0x7d, 0x5c, 0xcd, 0xef, 0x13 };
            bool success = ByteUtils.AreByteArraysEqual(response, expected);
            return success;
        }

        public static bool NTLMv1ResponseTest()
        {
            byte[] challenge = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef };
            byte[] response = NTLMCryptography.ComputeNTLMv1Response(challenge, "Password");
            byte[] expected = { 0x67, 0xc4, 0x30, 0x11, 0xf3, 0x02, 0x98, 0xa2, 0xad, 0x35, 0xec, 0xe6, 0x4f, 0x16, 0x33, 0x1c, 0x44, 0xbd, 0xbe, 0xd9, 0x27, 0x84, 0x1f, 0x94};
            bool success = ByteUtils.AreByteArraysEqual(response, expected);
            return success;
        }

        public static bool LMv2ResponseTest()
        {
            byte[] serverChallenge = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef };
            byte[] clientChallenge = new byte[] { 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa };
            byte[] response = NTLMCryptography.ComputeLMv2Response(serverChallenge, clientChallenge, "Password", "User", "Domain");
            byte[] expected = new byte[] { 0x86, 0xc3, 0x50, 0x97, 0xac, 0x9c, 0xec, 0x10, 0x25, 0x54, 0x76, 0x4a, 0x57, 0xcc, 0xcc, 0x19, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa};
            bool success = ByteUtils.AreByteArraysEqual(response, expected);
            return success;
        }

        public static bool NTLMv2ResponseTest()
        {
            byte[] serverChallenge = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef };
            byte[] clientChallenge = new byte[] { 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa };
            DateTime time = DateTime.FromFileTimeUtc(0); // same as new byte[8]
            NTLMv2ClientChallenge clientChallengeStructure = new NTLMv2ClientChallenge(time, clientChallenge, "Domain", "Server");
            byte[] clientChallengeStructurePadded = clientChallengeStructure.GetBytesPadded();
            byte[] clientNTProof = NTLMCryptography.ComputeNTLMv2Proof(serverChallenge, clientChallengeStructurePadded, "Password", "User", "Domain");

            byte[] expectedNTProof = new byte[] { 0x68, 0xcd, 0x0a, 0xb8, 0x51, 0xe5, 0x1c, 0x96, 0xaa, 0xbc, 0x92, 0x7b, 0xeb, 0xef, 0x6a, 0x1c };
            bool success = ByteUtils.AreByteArraysEqual(clientNTProof, expectedNTProof);
            return success;
        }

        public static bool NTLMv2ChallengeMessageTest()
        {
            byte[] expected = new byte[]{0x4e, 0x54, 0x4c, 0x4d, 0x53, 0x53, 0x50, 0x00, 0x02, 0x00, 0x00, 0x00, 0x0c, 0x00, 0x0c, 0x00,
                                         0x38, 0x00, 0x00, 0x00, 0x37, 0x82, 0x8a, 0xe2, 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
                                         0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x24, 0x00, 0x24, 0x00, 0x44, 0x00, 0x00, 0x00,
                                         0x06, 0x00, 0x70, 0x17, 0x00, 0x00, 0x00, 0x0f, 0x53, 0x00, 0x65, 0x00, 0x72, 0x00, 0x76, 0x00,
                                         0x65, 0x00, 0x72, 0x00, 0x02, 0x00, 0x0c, 0x00, 0x44, 0x00, 0x6f, 0x00, 0x6d, 0x00, 0x61, 0x00,
                                         0x69, 0x00, 0x6e, 0x00, 0x01, 0x00, 0x0c, 0x00, 0x53, 0x00, 0x65, 0x00, 0x72, 0x00, 0x76, 0x00,
                                         0x65, 0x00, 0x72, 0x00, 0x00, 0x00, 0x00, 0x00};

            byte[] serverChallenge = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef };
            ChallengeMessage message = new ChallengeMessage();
            message.ServerChallenge = serverChallenge;
            message.Version = new NTLMVersion(6, 0, 6000, NTLMVersion.NTLMSSP_REVISION_W2K3);
            message.NegotiateFlags = NegotiateFlags.UnicodeEncoding | NegotiateFlags.OEMEncoding | NegotiateFlags.TargetNameSupplied | NegotiateFlags.Sign | NegotiateFlags.Seal | NegotiateFlags.NTLMSessionSecurity | NegotiateFlags.AlwaysSign | NegotiateFlags.TargetTypeServer | NegotiateFlags.ExtendedSessionSecurity | NegotiateFlags.TargetInfo | NegotiateFlags.Version | NegotiateFlags.Use128BitEncryption | NegotiateFlags.KeyExchange | NegotiateFlags.Use56BitEncryption;
            message.TargetName = "Server";
            message.TargetInfo = AVPairUtils.GetAVPairSequence("Domain", "Server");

            byte[] messageBytes = message.GetBytes();
            bool success = ByteUtils.AreByteArraysEqual(expected, messageBytes);
            return success;
        }

        public static bool NTLMv2AuthenticateMessageTest()
        {
            byte[] expected = new byte[] {  0x4e, 0x54, 0x4c, 0x4d, 0x53, 0x53, 0x50, 0x00, 0x03, 0x00, 0x00, 0x00, 0x18, 0x00, 0x18, 0x00,
                                            0x6c, 0x00, 0x00, 0x00, 0x54, 0x00, 0x54, 0x00, 0x84, 0x00, 0x00, 0x00, 0x0c, 0x00, 0x0c, 0x00,
                                            0x48, 0x00, 0x00, 0x00, 0x08, 0x00, 0x08, 0x00, 0x54, 0x00, 0x00, 0x00, 0x10, 0x00, 0x10, 0x00,
                                            0x5c, 0x00, 0x00, 0x00, 0x10, 0x00, 0x10, 0x00, 0xd8, 0x00, 0x00, 0x00, 0x35, 0x82, 0x88, 0xe2,
                                            0x05, 0x01, 0x28, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x44, 0x00, 0x6f, 0x00, 0x6d, 0x00, 0x61, 0x00,
                                            0x69, 0x00, 0x6e, 0x00, 0x55, 0x00, 0x73, 0x00, 0x65, 0x00, 0x72, 0x00, 0x43, 0x00, 0x4f, 0x00,
                                            0x4d, 0x00, 0x50, 0x00, 0x55, 0x00, 0x54, 0x00, 0x45, 0x00, 0x52, 0x00, 0x86, 0xc3, 0x50, 0x97,
                                            0xac, 0x9c, 0xec, 0x10, 0x25, 0x54, 0x76, 0x4a, 0x57, 0xcc, 0xcc, 0x19, 0xaa, 0xaa, 0xaa, 0xaa,
                                            0xaa, 0xaa, 0xaa, 0xaa, 0x68, 0xcd, 0x0a, 0xb8, 0x51, 0xe5, 0x1c, 0x96, 0xaa, 0xbc, 0x92, 0x7b,
                                            0xeb, 0xef, 0x6a, 0x1c, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                            0x00, 0x00, 0x00, 0x00, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0x00, 0x00, 0x00, 0x00,
                                            0x02, 0x00, 0x0c, 0x00, 0x44, 0x00, 0x6f, 0x00, 0x6d, 0x00, 0x61, 0x00, 0x69, 0x00, 0x6e, 0x00,
                                            0x01, 0x00, 0x0c, 0x00, 0x53, 0x00, 0x65, 0x00, 0x72, 0x00, 0x76, 0x00, 0x65, 0x00, 0x72, 0x00,
                                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc5, 0xda, 0xd2, 0x54, 0x4f, 0xc9, 0x79, 0x90,
                                            0x94, 0xce, 0x1c, 0xe9, 0x0b, 0xc9, 0xd0, 0x3e};

            AuthenticateMessage cmp = new AuthenticateMessage(expected);

            byte[] sessionKey = {0xc5, 0xda, 0xd2, 0x54, 0x4f, 0xc9, 0x79, 0x90, 0x94, 0xce, 0x1c, 0xe9, 0x0b, 0xc9, 0xd0, 0x3e};
            byte[] serverChallenge = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef };
            byte[] clientChallenge = new byte[] { 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa };
            DateTime time = DateTime.FromFileTimeUtc(0); // same as new byte[8]
            NTLMv2ClientChallenge clientChallengeStructure = new NTLMv2ClientChallenge(time, clientChallenge, "Domain", "Server");
            byte[] clientChallengeStructurePadded = clientChallengeStructure.GetBytesPadded();
            byte[] clientNTProof = NTLMCryptography.ComputeNTLMv2Proof(serverChallenge, clientChallengeStructurePadded, "Password", "User", "Domain");
            
            AuthenticateMessage message = new AuthenticateMessage();
            message.EncryptedRandomSessionKey = sessionKey;
            message.Version = new NTLMVersion(5, 1, 2600, NTLMVersion.NTLMSSP_REVISION_W2K3);
            message.NegotiateFlags = NegotiateFlags.UnicodeEncoding | NegotiateFlags.TargetNameSupplied | NegotiateFlags.Sign | NegotiateFlags.Seal | NegotiateFlags.NTLMSessionSecurity | NegotiateFlags.AlwaysSign | NegotiateFlags.ExtendedSessionSecurity | NegotiateFlags.TargetInfo | NegotiateFlags.Version | NegotiateFlags.Use128BitEncryption | NegotiateFlags.KeyExchange | NegotiateFlags.Use56BitEncryption;
            message.DomainName = "Domain";
            message.WorkStation = "COMPUTER";
            message.UserName = "User";
            message.LmChallengeResponse = NTLMCryptography.ComputeLMv2Response(serverChallenge, clientChallenge, "Password", "User", "Domain");
            message.NtChallengeResponse = ByteUtils.Concatenate(clientNTProof, clientChallengeStructurePadded);
            
            byte[] messageBytes = message.GetBytes();
            // The payload entries may be distributed differently so we use cmp.GetBytes()
            bool success = ByteUtils.AreByteArraysEqual(messageBytes, cmp.GetBytes());
            return success;
        }

        public static bool TestAll()
        {
            bool success = (LMv1HashTest() &&
                            NTv1HashTest() &&
                            NTv2HashTest() &&
                            LMv1ResponseTest() &&
                            NTLMv1ResponseTest() &&
                            LMv2ResponseTest() &&
                            NTLMv2ResponseTest() &&
                            NTLMv2ChallengeMessageTest() &&
                            NTLMv2AuthenticateMessageTest());
            return success;
        }
        
    }
}
