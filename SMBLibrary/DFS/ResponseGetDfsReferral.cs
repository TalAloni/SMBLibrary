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
        private const int HeaderSize = 8;
        private const int MinEntryHeaderSize = 16;

        public ushort PathConsumed;
        public ushort NumberOfReferrals;
        public uint ReferralHeaderFlags;
        public List<DfsReferralEntry> ReferralEntries;
        public List<string> StringBuffer;

        public ResponseGetDfsReferral()
        {
            ReferralEntries = new List<DfsReferralEntry>();
            StringBuffer = new List<string>();
        }

        public ResponseGetDfsReferral(byte[] buffer)
        {
            ValidateBuffer(buffer);
            ParseHeader(buffer);
            ReferralEntries = new List<DfsReferralEntry>();
            StringBuffer = new List<string>();
            ParseReferralEntries(buffer);
        }

        private void ValidateBuffer(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (buffer.Length < HeaderSize)
            {
                throw new ArgumentException("Buffer too small for DFS referral response header", "buffer");
            }
        }

        private void ParseHeader(byte[] buffer)
        {
            PathConsumed = LittleEndianConverter.ToUInt16(buffer, 0);
            NumberOfReferrals = LittleEndianConverter.ToUInt16(buffer, 2);
            ReferralHeaderFlags = LittleEndianConverter.ToUInt32(buffer, 4);

            if (NumberOfReferrals > 0 && buffer.Length == HeaderSize)
            {
                throw new ArgumentException("Buffer too small for DFS referral entries", "buffer");
            }
        }

        private void ParseReferralEntries(byte[] buffer)
        {
            if (NumberOfReferrals == 0 || buffer.Length <= HeaderSize)
            {
                return;
            }

            int entryOffset = HeaderSize;
            for (int index = 0; index < NumberOfReferrals; index++)
            {
                if (buffer.Length < entryOffset + MinEntryHeaderSize)
                {
                    throw new ArgumentException("Buffer too small for DFS referral entry header", "buffer");
                }

                ushort versionNumber = LittleEndianConverter.ToUInt16(buffer, entryOffset);
                ushort size = LittleEndianConverter.ToUInt16(buffer, entryOffset + 2);
                uint timeToLive = LittleEndianConverter.ToUInt32(buffer, entryOffset + 8);

                if (size == 0 || entryOffset + size > buffer.Length)
                {
                    throw new ArgumentException("Buffer too small for DFS referral entry", "buffer");
                }

                DfsReferralEntry entry = ParseSingleEntry(buffer, entryOffset, versionNumber, size, timeToLive);
                ReferralEntries.Add(entry);

                entryOffset += size;
                if (entryOffset >= buffer.Length)
                {
                    break;
                }
            }
        }

        private DfsReferralEntry ParseSingleEntry(byte[] buffer, int entryOffset, ushort versionNumber, ushort size, uint timeToLive)
        {
            switch (versionNumber)
            {
                case 1:
                    return ParseV1Entry(buffer, entryOffset, size, timeToLive);
                case 2:
                    return ParseV2Entry(buffer, entryOffset, size, timeToLive);
                case 3:
                case 4:
                    return ParseV3V4Entry(buffer, entryOffset, versionNumber, size, timeToLive);
                default:
                    return new StubDfsReferralEntry();
            }
        }

        private DfsReferralEntryV1 ParseV1Entry(byte[] buffer, int entryOffset, ushort size, uint timeToLive)
        {
            DfsReferralEntryV1 v1 = new DfsReferralEntryV1();
            v1.VersionNumber = 1;
            v1.Size = size;
            v1.ServerType = (DfsServerType)LittleEndianConverter.ToUInt16(buffer, entryOffset + 4);
            v1.ReferralEntryFlags = (DfsReferralEntryFlags)LittleEndianConverter.ToUInt16(buffer, entryOffset + 6);
            v1.TimeToLive = timeToLive;

            if (size >= 16)
            {
                ushort dfsPathOffset = LittleEndianConverter.ToUInt16(buffer, entryOffset + 12);
                ushort networkAddressOffset = LittleEndianConverter.ToUInt16(buffer, entryOffset + 14);

                v1.DfsPath = ReadStringAtOffset(buffer, entryOffset, dfsPathOffset, "DFS path");
                v1.NetworkAddress = ReadStringAtOffset(buffer, entryOffset, networkAddressOffset, "DFS network address");
            }

            return v1;
        }

        private DfsReferralEntryV2 ParseV2Entry(byte[] buffer, int entryOffset, ushort size, uint timeToLive)
        {
            DfsReferralEntryV2 v2 = new DfsReferralEntryV2();
            v2.VersionNumber = 2;
            v2.Size = size;
            v2.ServerType = (DfsServerType)LittleEndianConverter.ToUInt16(buffer, entryOffset + 4);
            v2.ReferralEntryFlags = (DfsReferralEntryFlags)LittleEndianConverter.ToUInt16(buffer, entryOffset + 6);
            v2.Proximity = LittleEndianConverter.ToUInt32(buffer, entryOffset + 8);
            v2.TimeToLive = timeToLive;

            if (size >= 18)
            {
                ushort dfsPathOffset = LittleEndianConverter.ToUInt16(buffer, entryOffset + 12);
                ushort dfsAlternatePathOffset = LittleEndianConverter.ToUInt16(buffer, entryOffset + 14);
                ushort networkAddressOffset = LittleEndianConverter.ToUInt16(buffer, entryOffset + 16);

                v2.DfsPath = ReadStringAtOffset(buffer, entryOffset, dfsPathOffset, "DFS path");
                v2.DfsAlternatePath = ReadStringAtOffset(buffer, entryOffset, dfsAlternatePathOffset, "DFS alternate path");
                v2.NetworkAddress = ReadStringAtOffset(buffer, entryOffset, networkAddressOffset, "DFS network address");
            }

            return v2;
        }

        private DfsReferralEntryV3 ParseV3V4Entry(byte[] buffer, int entryOffset, ushort versionNumber, ushort size, uint timeToLive)
        {
            DfsReferralEntryV3 v3 = (versionNumber == 4) ? new DfsReferralEntryV4() : new DfsReferralEntryV3();
            v3.VersionNumber = versionNumber;
            v3.Size = size;
            v3.ServerType = (DfsServerType)LittleEndianConverter.ToUInt16(buffer, entryOffset + 4);
            v3.ReferralEntryFlags = (DfsReferralEntryFlags)LittleEndianConverter.ToUInt16(buffer, entryOffset + 6);
            v3.TimeToLive = timeToLive;

            bool isNameListReferral = (v3.ReferralEntryFlags & DfsReferralEntryFlags.NameListReferral) != 0;
            if (isNameListReferral)
            {
                ParseNameListReferral(v3, buffer, entryOffset, size);
            }
            else
            {
                ParseNormalV3Referral(v3, buffer, entryOffset, size);
            }

            return v3;
        }

        private void ParseNameListReferral(DfsReferralEntryV3 v3, byte[] buffer, int entryOffset, ushort size)
        {
            if (size < 32)
            {
                return;
            }

            if (entryOffset + 28 > buffer.Length)
            {
                throw new ArgumentException("Buffer too small for NameListReferral ServiceSiteGuid", "buffer");
            }

            byte[] guidBytes = new byte[16];
            Array.Copy(buffer, entryOffset + 12, guidBytes, 0, 16);
            v3.ServiceSiteGuid = new Guid(guidBytes);

            ushort numberOfExpandedNames = LittleEndianConverter.ToUInt16(buffer, entryOffset + 28);
            ushort expandedNameOffset = LittleEndianConverter.ToUInt16(buffer, entryOffset + 30);

            int specialNameOffset = entryOffset + 32;
            if (specialNameOffset < buffer.Length)
            {
                v3.SpecialName = ByteReader.ReadNullTerminatedUTF16String(buffer, specialNameOffset);
                AddToStringBuffer(v3.SpecialName);
            }

            v3.ExpandedNames = new List<string>();
            if (numberOfExpandedNames > 0 && expandedNameOffset > 0)
            {
                ParseExpandedNames(v3, buffer, entryOffset + expandedNameOffset, numberOfExpandedNames);
            }
        }

        private void ParseExpandedNames(DfsReferralEntryV3 v3, byte[] buffer, int startOffset, ushort count)
        {
            int currentOffset = startOffset;
            for (int i = 0; i < count; i++)
            {
                if (currentOffset >= buffer.Length)
                {
                    break;
                }

                string expandedName = ByteReader.ReadNullTerminatedUTF16String(buffer, currentOffset);
                if (expandedName != null)
                {
                    v3.ExpandedNames.Add(expandedName);
                    AddToStringBuffer(expandedName);
                    currentOffset += (expandedName.Length + 1) * 2;
                }
                else
                {
                    currentOffset += 2;
                }
            }
        }

        private void ParseNormalV3Referral(DfsReferralEntryV3 v3, byte[] buffer, int entryOffset, ushort size)
        {
            if (size < 18)
            {
                return;
            }

            ushort dfsPathOffset = LittleEndianConverter.ToUInt16(buffer, entryOffset + 12);
            ushort dfsAlternatePathOffset = LittleEndianConverter.ToUInt16(buffer, entryOffset + 14);
            ushort networkAddressOffset = LittleEndianConverter.ToUInt16(buffer, entryOffset + 16);

            v3.DfsPath = ReadStringAtOffset(buffer, entryOffset, dfsPathOffset, "DFS path");
            v3.DfsAlternatePath = ReadStringAtOffset(buffer, entryOffset, dfsAlternatePathOffset, "DFS alternate path");
            v3.NetworkAddress = ReadStringAtOffset(buffer, entryOffset, networkAddressOffset, "DFS network address");
        }

        private string ReadStringAtOffset(byte[] buffer, int entryOffset, ushort relativeOffset, string fieldName)
        {
            int absoluteOffset = entryOffset + relativeOffset;
            if (absoluteOffset < 0 || absoluteOffset >= buffer.Length)
            {
                throw new ArgumentException(fieldName + " offset outside buffer", "buffer");
            }

            string value = ByteReader.ReadNullTerminatedUTF16String(buffer, absoluteOffset);
            AddToStringBuffer(value);
            return value;
        }

        private void AddToStringBuffer(string value)
        {
            if (value != null)
            {
                StringBuffer.Add(value);
            }
        }

        public byte[] GetBytes()
        {
            byte[] buffer = new byte[HeaderSize];
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
