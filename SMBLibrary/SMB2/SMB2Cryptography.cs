/* Copyright (C) 2020-2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Security.Cryptography;
using Utilities;

namespace SMBLibrary.SMB2
{
    internal class SMB2Cryptography
    {
        private const int AesCcmNonceLength = 11;

        public static byte[] CalculateSignature(byte[] signingKey, SMB2Dialect dialect, byte[] buffer, int offset, int paddedLength)
        {
            byte[] hash;
            if (dialect == SMB2Dialect.SMB202 || dialect == SMB2Dialect.SMB210)
            {
                hash = new HMACSHA256(signingKey).ComputeHash(buffer, offset, paddedLength);
            }
            else
            {
                hash = AesCmac.CalculateAesCmac(signingKey, buffer, offset, paddedLength);
            }

            // [MS-SMB2] The first 16 bytes of the hash MUST be copied into the 16-byte signature field of the SMB2 Header.
            return ByteReader.ReadBytes(hash, 0, SMB2Header.SignatureLength);
        }

        public static bool VerifySignature(byte[] messageBytes, SMB2Dialect dialect, byte[] signingKey)
        {
            byte[] signature = ByteReader.ReadBytes(messageBytes, SMB2Header.SignatureOffset, SMB2Header.SignatureLength);
            Array.Clear(messageBytes, SMB2Header.SignatureOffset, SMB2Header.SignatureLength);
            byte[] expectedSignature = CalculateSignature(signingKey, dialect, messageBytes, 0, messageBytes.Length);
            return ByteUtils.AreByteArraysEqual(signature, expectedSignature);
        }

        public static byte[] GenerateSigningKey(byte[] sessionKey, SMB2Dialect dialect, byte[] preauthIntegrityHashValue)
        {
            if (dialect == SMB2Dialect.SMB202 || dialect == SMB2Dialect.SMB210)
            {
                return sessionKey;
            }

            if (dialect == SMB2Dialect.SMB311 && preauthIntegrityHashValue == null)
            {
                throw new ArgumentNullException("preauthIntegrityHashValue");
            }

            string labelString = (dialect == SMB2Dialect.SMB311) ? "SMBSigningKey" : "SMB2AESCMAC";
            byte[] label = GetNullTerminatedAnsiString(labelString);
            byte[] context = (dialect == SMB2Dialect.SMB311) ? preauthIntegrityHashValue : GetNullTerminatedAnsiString("SmbSign");

            HMACSHA256 hmac = new HMACSHA256(sessionKey);
            return SP800_1008.DeriveKey(hmac, label, context, 128);
        }

        public static byte[] GenerateClientEncryptionKey(byte[] sessionKey, SMB2Dialect dialect, byte[] preauthIntegrityHashValue)
        {
            if (dialect == SMB2Dialect.SMB311 && preauthIntegrityHashValue == null)
            {
                throw new ArgumentNullException("preauthIntegrityHashValue");
            }

            string labelString = (dialect == SMB2Dialect.SMB311) ? "SMBC2SCipherKey" : "SMB2AESCCM";
            byte[] label = GetNullTerminatedAnsiString(labelString);
            byte[] context = (dialect == SMB2Dialect.SMB311) ? preauthIntegrityHashValue : GetNullTerminatedAnsiString("ServerIn ");

            HMACSHA256 hmac = new HMACSHA256(sessionKey);
            return SP800_1008.DeriveKey(hmac, label, context, 128);
        }

        public static byte[] GenerateClientDecryptionKey(byte[] sessionKey, SMB2Dialect dialect, byte[] preauthIntegrityHashValue)
        {
            if (dialect == SMB2Dialect.SMB311 && preauthIntegrityHashValue == null)
            {
                throw new ArgumentNullException("preauthIntegrityHashValue");
            }

            string labelString = (dialect == SMB2Dialect.SMB311) ? "SMBS2CCipherKey" : "SMB2AESCCM";
            byte[] label = GetNullTerminatedAnsiString(labelString);
            byte[] context = (dialect == SMB2Dialect.SMB311) ? preauthIntegrityHashValue : GetNullTerminatedAnsiString("ServerOut");

            HMACSHA256 hmac = new HMACSHA256(sessionKey);
            return SP800_1008.DeriveKey(hmac, label, context, 128);
        }

        /// <summary>
        /// Encyrpt message and prefix with SMB2 TransformHeader
        /// </summary>
        public static byte[] TransformMessage(byte[] key, byte[] message, ulong sessionID)
        {
            byte[] nonce = GenerateAesCcmNonce();
            byte[] signature;
            byte[] encryptedMessage = EncryptMessage(key, nonce, message, sessionID, out signature);
            SMB2TransformHeader transformHeader = CreateTransformHeader(nonce, message.Length, sessionID);
            transformHeader.Signature = signature;

            byte[] buffer = new byte[SMB2TransformHeader.Length + message.Length];
            transformHeader.WriteBytes(buffer, 0);
            ByteWriter.WriteBytes(buffer, SMB2TransformHeader.Length, encryptedMessage);
            return buffer;
        }
        
        public static byte[] EncryptMessage(byte[] key, byte[] nonce, byte[] message, ulong sessionID, out byte[] signature)
        {
            SMB2TransformHeader transformHeader = CreateTransformHeader(nonce, message.Length, sessionID);
            byte[] associatedata = transformHeader.GetAssociatedData();
            return AesCcm.Encrypt(key, nonce, message, associatedata, SMB2TransformHeader.SignatureLength, out signature);
        }

        public static byte[] DecryptMessage(byte[] key, SMB2TransformHeader transformHeader, byte[] encryptedMessage)
        {
            byte[] associatedData = transformHeader.GetAssociatedData();
            byte[] aesCcmNonce = ByteReader.ReadBytes(transformHeader.Nonce, 0, AesCcmNonceLength);
            return AesCcm.DecryptAndAuthenticate(key, aesCcmNonce, encryptedMessage, associatedData, transformHeader.Signature);
        }

        public static byte[] ComputeHash(HashAlgorithm hashAlgorithm, byte[] buffer)
        {
            if (hashAlgorithm == HashAlgorithm.SHA512)
            {
                return SHA512.Create().ComputeHash(buffer);
            }
            else
            {
                throw new NotSupportedException($"Hash algorithm {hashAlgorithm} is not supported");
            }
        }

        private static SMB2TransformHeader CreateTransformHeader(byte[] nonce, int originalMessageLength, ulong sessionID)
        {
            byte[] nonceWithPadding = new byte[SMB2TransformHeader.NonceLength];
            Array.Copy(nonce, nonceWithPadding, nonce.Length);

            SMB2TransformHeader transformHeader = new SMB2TransformHeader();
            transformHeader.Nonce = nonceWithPadding;
            transformHeader.OriginalMessageSize = (uint)originalMessageLength;
            transformHeader.Flags = SMB2TransformHeaderFlags.Encrypted;
            transformHeader.SessionId = sessionID;

            return transformHeader;
        }

        private static byte[] GenerateAesCcmNonce()
        {
            byte[] aesCcmNonce = new byte[AesCcmNonceLength];
            new Random().NextBytes(aesCcmNonce);
            return aesCcmNonce;
        }

        private static byte[] GetNullTerminatedAnsiString(string value)
        {
            byte[] result = new byte[value.Length + 1];
            ByteWriter.WriteNullTerminatedAnsiString(result, 0, value);
            return result;
        }
    }
}
