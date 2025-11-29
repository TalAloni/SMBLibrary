/* Copyright (C) 2025 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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
    /// [MS-DFSC] 2.2.5.3 DFS_REFERRAL_V3
    /// V3 supports both normal referrals and NameListReferrals (for SYSVOL/NETLOGON).
    /// </summary>
    public class DfsReferralEntryV3 : DfsReferralEntry
    {
        private const int FixedLength = 12;

        public ushort VersionNumber;
        public ushort Size;
        public DfsServerType ServerType;
        public DfsReferralEntryFlags ReferralEntryFlags;
        public uint TimeToLive;

        // Normal referral fields (when IsNameListReferral is false)
        public string DfsPath;
        public string DfsAlternatePath;
        public string NetworkAddress;
        public Guid ServiceSiteGuid;

        // NameListReferral fields (when IsNameListReferral is true)
        public string SpecialName;
        public List<string> ExpandedNames;

        public DfsReferralEntryV3()
        {
            VersionNumber = 3;
        }

        public DfsReferralEntryV3(byte[] buffer, ref int offset)
        {
            VersionNumber = LittleEndianConverter.ToUInt16(buffer, offset + 0);
            Size = LittleEndianConverter.ToUInt16(buffer, offset + 2);
            ServerType = (DfsServerType)LittleEndianConverter.ToUInt16(buffer, offset + 4);
            ReferralEntryFlags = (DfsReferralEntryFlags)LittleEndianConverter.ToUInt16(buffer, offset + 6);

            TimeToLive = LittleEndianConverter.ToUInt32(buffer, offset + 8);

            bool isNameListReferral = (ReferralEntryFlags & DfsReferralEntryFlags.NameListReferral) != 0;
            if (!isNameListReferral)
            {
                ushort dfsPathOffset = LittleEndianConverter.ToUInt16(buffer, offset + 12);
                ushort dfsAlternatePathOffset = LittleEndianConverter.ToUInt16(buffer, offset + 14);
                ushort networkAddressOffset = LittleEndianConverter.ToUInt16(buffer, offset + 16);
                ServiceSiteGuid = LittleEndianConverter.ToGuid(buffer, offset + 18);

                DfsPath = ByteReader.ReadNullTerminatedUTF16String(buffer, offset + dfsPathOffset);
                DfsAlternatePath = ByteReader.ReadNullTerminatedUTF16String(buffer, offset + dfsAlternatePathOffset);
                NetworkAddress = ByteReader.ReadNullTerminatedUTF16String(buffer, offset + networkAddressOffset);
            }
            else
            {
                ushort specialNameOffset = LittleEndianConverter.ToUInt16(buffer, offset + 12);
                ushort numberOfExpandedNames = LittleEndianConverter.ToUInt16(buffer, offset + 14);
                ushort expandedNameOffset = LittleEndianConverter.ToUInt16(buffer, offset + 16);

                SpecialName = ByteReader.ReadNullTerminatedUTF16String(buffer, specialNameOffset);
                ExpandedNames = new List<string>();
                int currentOffset = expandedNameOffset;
                for (int nameIndex = 0; nameIndex < numberOfExpandedNames; nameIndex++)
                {
                    if (currentOffset >= buffer.Length)
                    {
                        break;
                    }

                    string expandedName = ByteReader.ReadNullTerminatedUTF16String(buffer, currentOffset);
                    if (expandedName != null)
                    {
                        ExpandedNames.Add(expandedName);
                        currentOffset += (expandedName.Length + 1) * 2;
                    }
                    else
                    {
                        currentOffset += 2;
                    }
                }
            }

            offset += Size;
        }

        public override byte[] GetBytes()
        {
            byte[] buffer = new byte[Length];
            LittleEndianWriter.WriteUInt16(buffer, 0, VersionNumber);
            LittleEndianWriter.WriteUInt16(buffer, 2, (ushort)buffer.Length);
            LittleEndianWriter.WriteUInt16(buffer, 4, (ushort)ServerType);
            LittleEndianWriter.WriteUInt16(buffer, 6, (ushort)ReferralEntryFlags);
            LittleEndianWriter.WriteUInt32(buffer, 8, TimeToLive);

            if (!IsNameListReferral)
            {
                ushort dfsPathOffset = FixedLength + 6 + 16;
                ushort dfsAlternatePathOffset = (ushort)(dfsPathOffset + (DfsPath.Length + 1) * 2);
                ushort networkAddressOffset = (ushort)(dfsAlternatePathOffset + (DfsAlternatePath.Length + 1) * 2);
                LittleEndianWriter.WriteUInt16(buffer, 12, dfsPathOffset);
                LittleEndianWriter.WriteUInt16(buffer, 14, dfsAlternatePathOffset);
                LittleEndianWriter.WriteUInt16(buffer, 16, networkAddressOffset);
                LittleEndianWriter.WriteGuid(buffer, 18, ServiceSiteGuid);

                ByteWriter.WriteNullTerminatedUTF16String(buffer, dfsPathOffset, DfsPath);
                ByteWriter.WriteNullTerminatedUTF16String(buffer, dfsAlternatePathOffset, DfsAlternatePath);
                ByteWriter.WriteNullTerminatedUTF16String(buffer, networkAddressOffset, NetworkAddress);
            }
            else
            {
                ushort specialNameOffset = FixedLength + 6;
                ushort expandedNameOffset = (ushort)(specialNameOffset + (SpecialName.Length + 1) * 2);
                LittleEndianWriter.WriteUInt16(buffer, 12, specialNameOffset);
                LittleEndianWriter.WriteUInt16(buffer, 14, (ushort)ExpandedNames.Count);
                LittleEndianWriter.WriteUInt16(buffer, 16, expandedNameOffset);

                ByteWriter.WriteNullTerminatedUTF16String(buffer, specialNameOffset, SpecialName);
                int currentOffset = expandedNameOffset;
                for (int nameIndex = 0; nameIndex < ExpandedNames.Count; nameIndex++)
                {
                    ByteWriter.WriteNullTerminatedUTF16String(buffer, currentOffset, ExpandedNames[nameIndex]);
                    currentOffset += (ExpandedNames[nameIndex].Length + 1) * 2;
                }
            }

            return buffer;
        }

        public override int Length
        {
            get
            {
                if (!IsNameListReferral)
                {
                    return FixedLength + 6 + 16 + (DfsPath.Length + 1 + DfsAlternatePath.Length + 1 + NetworkAddress.Length + 1) * 2;
                }
                else
                {
                    int length = FixedLength + 6 + (SpecialName.Length + 1) * 2;
                    for (int nameIndex = 0; nameIndex < ExpandedNames.Count; nameIndex++)
                    {
                        length += (ExpandedNames[nameIndex].Length + 1) * 2;
                    }
                    return length;
                }
            }
        }

        /// <summary>
        /// Returns true if this is a NameListReferral (used for SYSVOL/NETLOGON DC lists).
        /// </summary>
        public bool IsNameListReferral
        {
            get
            {
                return (ReferralEntryFlags & DfsReferralEntryFlags.NameListReferral) != 0;
            }
        }
    }
}