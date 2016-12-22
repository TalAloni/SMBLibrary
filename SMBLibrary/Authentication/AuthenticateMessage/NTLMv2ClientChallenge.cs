/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace SMBLibrary.Authentication
{
    /// <summary>
    /// NTLMv2_CLIENT_CHALLENGE
    /// </summary>
    public class NTLMv2ClientChallengeStructure
    {
        public static readonly DateTime EpochTime = DateTime.FromFileTimeUtc(0);

        public byte ResponseVersion;
        public byte ResponseVersionHigh;
        public DateTime Time;
        public byte[] ClientChallenge;
        public KeyValuePairList<AVPairKey, byte[]> AVPairs;

        public NTLMv2ClientChallengeStructure()
        {
        }

        public NTLMv2ClientChallengeStructure(DateTime time, byte[] clientChallenge, string domainName, string computerName)
        {
            ResponseVersion = 1;
            ResponseVersionHigh = 1;
            Time = time;
            ClientChallenge = clientChallenge;
            AVPairs = new KeyValuePairList<AVPairKey, byte[]>();
            AVPairs.Add(AVPairKey.NbDomainName, UnicodeEncoding.Unicode.GetBytes(domainName));
            AVPairs.Add(AVPairKey.NbComputerName, UnicodeEncoding.Unicode.GetBytes(computerName));
        }

        public NTLMv2ClientChallengeStructure(byte[] buffer) : this(buffer, 0)
        {
        }

        public NTLMv2ClientChallengeStructure(byte[] buffer, int offset)
        {
            ResponseVersion = ByteReader.ReadByte(buffer, offset + 0);
            ResponseVersionHigh = ByteReader.ReadByte(buffer, offset + 1);
            long temp = LittleEndianConverter.ToInt64(buffer, offset + 8);
            Time = DateTime.FromFileTimeUtc(temp);
            ClientChallenge = ByteReader.ReadBytes(buffer, offset + 16, 8);
            AVPairs = AVPairUtils.ReadAVPairSequence(buffer, offset + 28);
        }

        public byte[] GetBytes()
        {
            byte[] sequenceBytes = AVPairUtils.GetAVPairSequenceBytes(AVPairs);
            byte[] timeBytes = LittleEndianConverter.GetBytes((ulong)Time.ToFileTimeUtc());

            byte[] buffer = new byte[28 + sequenceBytes.Length];
            ByteWriter.WriteByte(buffer, 0, ResponseVersion);
            ByteWriter.WriteByte(buffer, 1, ResponseVersionHigh);
            ByteWriter.WriteBytes(buffer, 8, timeBytes);
            ByteWriter.WriteBytes(buffer, 16, ClientChallenge, 8);
            ByteWriter.WriteBytes(buffer, 28, sequenceBytes);
            return buffer;
        }

        /// <summary>
        /// [MS-NLMP] Page 60, Response key calculation algorithm:
        /// To create 'temp', 4 zero bytes will be appended to NTLMv2_CLIENT_CHALLENGE
        /// </summary>
        public byte[] GetBytesPadded()
        {
            return ByteUtils.Concatenate(GetBytes(), new byte[4]);
        }
    }
}
