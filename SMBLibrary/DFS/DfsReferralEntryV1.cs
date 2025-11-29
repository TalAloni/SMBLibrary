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
    /// [MS-DFSC] 2.2.5.1 DFS_REFERRAL_V1
    /// </summary>
    public class DfsReferralEntryV1 : DfsReferralEntry
    {
        public const int FixedLength = 8;

        public ushort VersionNumber;
        public ushort Size;
        public DfsServerType ServerType;
        public DfsReferralEntryFlags ReferralEntryFlags;
        public string ShareName;

        public DfsReferralEntryV1()
        {
            VersionNumber = 1;
        }

        public DfsReferralEntryV1(byte[] buffer, ref int offset)
        {
            VersionNumber = LittleEndianConverter.ToUInt16(buffer, offset + 0);
            Size = LittleEndianConverter.ToUInt16(buffer, offset + 2);
            ServerType = (DfsServerType)LittleEndianConverter.ToUInt16(buffer, offset + 4);
            ReferralEntryFlags = (DfsReferralEntryFlags)LittleEndianConverter.ToUInt16(buffer, offset + 6);
            ShareName = ByteReader.ReadNullTerminatedUTF16String(buffer, offset + 8);

            offset += Size;
        }

        public override byte[] GetBytes()
        {
            byte[] buffer = new byte[this.Length];
            LittleEndianWriter.WriteUInt16(buffer, 0, VersionNumber);
            LittleEndianWriter.WriteUInt16(buffer, 2, (ushort)buffer.Length);
            LittleEndianWriter.WriteUInt16(buffer, 4, (ushort)ServerType);
            LittleEndianWriter.WriteUInt16(buffer, 6, (ushort)ReferralEntryFlags);
            ByteWriter.WriteNullTerminatedUTF16String(buffer, 8, ShareName);
            return buffer;
        }

        public override int Length
        {
            get
            {
                return FixedLength + (ShareName.Length + 1) * 2;
            }
        }
    }
}
