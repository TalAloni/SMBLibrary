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

            if (NumberOfReferrals > 0 && buffer.Length == 8)
            {
                throw new ArgumentException("Buffer too small for DFS referral entries", "buffer");
            }

            ReferralEntries = new List<DfsReferralEntry>();
            StringBuffer = new List<string>();
            if (NumberOfReferrals > 0 && buffer.Length > 8)
            {
                int entryOffset = 8;

                for (int index = 0; index < NumberOfReferrals; index++)
                {
                    // Minimal v1 header is 16 bytes; stop if we do not have enough bytes.
                    if (buffer.Length < entryOffset + 16)
                    {
                        throw new ArgumentException("Buffer too small for DFS referral entry header", "buffer");
                    }

                    ushort versionNumber = LittleEndianConverter.ToUInt16(buffer, entryOffset + 0);
                    ushort size = LittleEndianConverter.ToUInt16(buffer, entryOffset + 2);
                    // ServerType (2) and ReferralEntryFlags (2) are currently ignored.
                    uint timeToLive = LittleEndianConverter.ToUInt32(buffer, entryOffset + 8);

                    // Size is declared per-entry; ensure it does not exceed the remaining buffer.
                    if (size == 0 || entryOffset + size > buffer.Length)
                    {
                        throw new ArgumentException("Buffer too small for DFS referral entry", "buffer");
                    }

                    DfsReferralEntry entry;
                    if (versionNumber == 1)
                    {
                        DfsReferralEntryV1 v1 = new DfsReferralEntryV1();
                        v1.VersionNumber = versionNumber;
                        v1.Size = size;
                        v1.TimeToLive = timeToLive;

                        // For v1, DFSPathOffset and NetworkAddressOffset are 2-byte offsets
                        // from the beginning of the referral entry to the corresponding
                        // null-terminated UTF-16 strings.
                        if (size >= 16)
                        {
                            ushort dfsPathOffset = LittleEndianConverter.ToUInt16(buffer, entryOffset + 12);
                            ushort networkAddressOffset = LittleEndianConverter.ToUInt16(buffer, entryOffset + 14);

                            int dfsPathAbsoluteOffset = entryOffset + dfsPathOffset;
                            int networkAddressAbsoluteOffset = entryOffset + networkAddressOffset;

                            if (dfsPathAbsoluteOffset < 0 || dfsPathAbsoluteOffset >= buffer.Length)
                            {
                                throw new ArgumentException("DFS path offset outside buffer", "buffer");
                            }

                            if (networkAddressAbsoluteOffset < 0 || networkAddressAbsoluteOffset >= buffer.Length)
                            {
                                throw new ArgumentException("DFS network address offset outside buffer", "buffer");
                            }

                            v1.DfsPath = ByteReader.ReadNullTerminatedUTF16String(buffer, dfsPathAbsoluteOffset);
                            if (v1.DfsPath != null)
                            {
                                StringBuffer.Add(v1.DfsPath);
                            }

                            v1.NetworkAddress = ByteReader.ReadNullTerminatedUTF16String(buffer, networkAddressAbsoluteOffset);
                            if (v1.NetworkAddress != null)
                            {
                                StringBuffer.Add(v1.NetworkAddress);
                            }
                        }

                        entry = v1;
                    }
                    else
                    {
                        entry = new StubDfsReferralEntry();
                    }

                    ReferralEntries.Add(entry);

                    // Advance by the entry Size; at this point size has been validated.
                    entryOffset += size;
                    if (entryOffset >= buffer.Length)
                    {
                        break;
                    }
                }
            }
        }

        public byte[] GetBytes()
        {
            byte[] buffer = new byte[8];
            LittleEndianWriter.WriteUInt16(buffer, 0, PathConsumed);
            LittleEndianWriter.WriteUInt16(buffer, 2, NumberOfReferrals);
            LittleEndianWriter.WriteUInt32(buffer, 4, ReferralHeaderFlags);
            return buffer;
        }

        private class StubDfsReferralEntry : DfsReferralEntry
        {
        }
    }
}
