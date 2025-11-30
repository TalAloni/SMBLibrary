/* Copyright (C) 2025 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using Utilities;

namespace SMBLibrary.DFS
{
    /// <summary>
    /// [MS-DFSC] 2.2.5.2 DFS_REFERRAL_V2
    /// </summary>
    public class DfsReferralEntryV2 : DfsReferralEntry
    {
        public const int FixedLength = 22;

        public ushort VersionNumber;
        public ushort Size;
        public DfsServerType ServerType;
        public DfsReferralEntryFlags ReferralEntryFlags;
        public uint Proximity;
        public uint TimeToLive;
        public string DfsPath;
        public string DfsAlternatePath;
        public string NetworkAddress;

        public DfsReferralEntryV2()
        {
            VersionNumber = 2;
        }

        public DfsReferralEntryV2(byte[] buffer, ref int offset)
        {
            VersionNumber = LittleEndianConverter.ToUInt16(buffer, offset + 0);
            Size = LittleEndianConverter.ToUInt16(buffer, offset + 2);
            ServerType = (DfsServerType)LittleEndianConverter.ToUInt16(buffer, offset + 4);
            ReferralEntryFlags = (DfsReferralEntryFlags)LittleEndianConverter.ToUInt16(buffer, offset + 6);

            Proximity = LittleEndianConverter.ToUInt32(buffer, offset + 8);
            TimeToLive = LittleEndianConverter.ToUInt32(buffer, offset + 12);

            ushort dfsPathOffset = LittleEndianConverter.ToUInt16(buffer, offset + 16);
            ushort dfsAlternatePathOffset = LittleEndianConverter.ToUInt16(buffer, offset + 18);
            ushort networkAddressOffset = LittleEndianConverter.ToUInt16(buffer, offset + 20);

            DfsPath = ByteReader.ReadNullTerminatedUTF16String(buffer, offset + dfsPathOffset);
            DfsAlternatePath = ByteReader.ReadNullTerminatedUTF16String(buffer, offset + dfsAlternatePathOffset);
            NetworkAddress = ByteReader.ReadNullTerminatedUTF16String(buffer, offset + networkAddressOffset);

            offset += Size;
        }

        public override byte[] WriteBytes(byte[] buffer, int offset, int stringsOffset)
        {
            LittleEndianWriter.WriteUInt16(buffer, offset + 0, VersionNumber);
            LittleEndianWriter.WriteUInt16(buffer, offset + 2, (ushort)this.Length);
            LittleEndianWriter.WriteUInt16(buffer, offset + 4, (ushort)ServerType);
            LittleEndianWriter.WriteUInt16(buffer, offset + 6, (ushort)ReferralEntryFlags);
            LittleEndianWriter.WriteUInt32(buffer, offset + 8, Proximity);
            LittleEndianWriter.WriteUInt32(buffer, offset + 12, TimeToLive);

            int dfsPathOffset = stringsOffset;
            int dfsAlternatePathOffset = (ushort)(dfsPathOffset + (DfsPath.Length + 1) * 2);
            int networkAddressOffset = (ushort)(dfsAlternatePathOffset + (DfsAlternatePath.Length + 1) * 2);
            // offsets are relative to the start of the referral entry
            LittleEndianWriter.WriteUInt16(buffer, 16, (ushort)(dfsPathOffset - offset));
            LittleEndianWriter.WriteUInt16(buffer, 18, (ushort)(dfsAlternatePathOffset - offset));
            LittleEndianWriter.WriteUInt16(buffer, 20, (ushort)(networkAddressOffset - offset));

            ByteWriter.WriteNullTerminatedUTF16String(buffer, dfsPathOffset, DfsPath);
            ByteWriter.WriteNullTerminatedUTF16String(buffer, dfsAlternatePathOffset, DfsAlternatePath);
            ByteWriter.WriteNullTerminatedUTF16String(buffer, networkAddressOffset, NetworkAddress);

            return buffer;
        }

        public override int Length
        {
            get
            {
                return FixedLength;
            }
        }

        public override int StringsLength
        {
            get
            {
                return (DfsPath.Length + 1 + DfsAlternatePath.Length + 1 + NetworkAddress.Length + 1) * 2;
            }
        }
    }
}
