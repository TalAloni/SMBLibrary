/* Copyright (C) 2014-2025 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using Utilities;

namespace SMBLibrary.DFS
{
    /// <summary>
    /// [MS-DFSC] RESP_GET_DFS_REFERRAL
    /// </summary>
    public class ResponseGetDfsReferral
    {
        private const int HeaderSize = 8;
        private const int MinReferralEntryHeaderSize = 16;

        public ushort PathConsumed;
        // ushort NumberOfReferrals;
        public DfsReferralHeaderFlags ReferralHeaderFlags;
        public List<DfsReferralEntry> ReferralEntries;
        // StringBuffer;
        // Padding

        public ResponseGetDfsReferral()
        {
            ReferralEntries = new List<DfsReferralEntry>();
        }

        public ResponseGetDfsReferral(byte[] buffer)
        {
            if (buffer.Length < HeaderSize)
            {
                throw new ArgumentException("Buffer too small for DFS referral response header", nameof(buffer));
            }

            PathConsumed = LittleEndianConverter.ToUInt16(buffer, 0);
            ushort numberOfReferrals = LittleEndianConverter.ToUInt16(buffer, 2);
            ReferralHeaderFlags = (DfsReferralHeaderFlags)LittleEndianConverter.ToUInt32(buffer, 4);

            if (numberOfReferrals > 0 && buffer.Length == HeaderSize)
            {
                throw new ArgumentException("Buffer too small for DFS referral entries", nameof(buffer));
            }

            ReferralEntries = new List<DfsReferralEntry>();
            int entryOffset = HeaderSize;
            for (int index = 0; index < numberOfReferrals; index++)
            {
                if (buffer.Length < entryOffset + MinReferralEntryHeaderSize)
                {
                    throw new ArgumentException("Buffer too small for DFS referral entry header", nameof(buffer));
                }

                DfsReferralEntry entry = DfsReferralEntry.ReadEntry(buffer, ref entryOffset);
                ReferralEntries.Add(entry);

                if (entryOffset > buffer.Length)
                {
                    throw new ArgumentException("Buffer too small for next DFS referral", nameof(buffer));
                }
            }
        }

        public byte[] GetBytes()
        {
            int length = HeaderSize;
            foreach (DfsReferralEntry entry in ReferralEntries)
            {
                length += entry.Length;
            }

            int stringsOffset = length;
            foreach (DfsReferralEntry entry in ReferralEntries)
            {
                length += entry.StringsLength;
            }

            byte[] buffer = new byte[length];
            LittleEndianWriter.WriteUInt16(buffer, 0, PathConsumed);
            LittleEndianWriter.WriteUInt16(buffer, 2, (ushort)ReferralEntries.Count);
            LittleEndianWriter.WriteUInt32(buffer, 4, (uint)ReferralHeaderFlags);

            int offset = HeaderSize;
            foreach (DfsReferralEntry entry in ReferralEntries)
            {
                entry.WriteBytes(buffer, offset, stringsOffset);
                offset += entry.Length;
                stringsOffset += entry.StringsLength;
            }
            return buffer;
        }
    }
}
