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

namespace SMBLibrary
{
    /// <summary>
    /// [MS-DFSC] RESP_GET_DFS_REFERRAL
    /// </summary>
    public class ResponseGetDfsReferral
    {
        public ushort PathConsumed;
        public ushort NumberOfReferrals;
        public uint ReferralHeaderFlags;
        public List<DfsReferralEntry> ReferralEntries;
        public List<string> StringBuffer;
        // This implementation currently handles only the fixed 8-byte header.
        // Referral entries and string buffers are not yet parsed or serialized.

        public ResponseGetDfsReferral()
        {
            ReferralEntries = new List<DfsReferralEntry>();
            StringBuffer = new List<string>();
        }

        public ResponseGetDfsReferral(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (buffer.Length < 8)
            {
                throw new ArgumentException("Buffer too small for DFS referral response header", "buffer");
            }

            PathConsumed = LittleEndianConverter.ToUInt16(buffer, 0);
            NumberOfReferrals = LittleEndianConverter.ToUInt16(buffer, 2);
            ReferralHeaderFlags = LittleEndianConverter.ToUInt32(buffer, 4);

            ReferralEntries = new List<DfsReferralEntry>();
            StringBuffer = new List<string>();
        }

        public byte[] GetBytes()
        {
            byte[] buffer = new byte[8];
            LittleEndianWriter.WriteUInt16(buffer, 0, PathConsumed);
            LittleEndianWriter.WriteUInt16(buffer, 2, NumberOfReferrals);
            LittleEndianWriter.WriteUInt32(buffer, 4, ReferralHeaderFlags);
            return buffer;
        }
    }
}
