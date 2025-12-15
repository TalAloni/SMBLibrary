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
    /// [MS-DFSC] 2.2.3 REQ_GET_DFS_REFERRAL_EX
    /// Extended DFS referral request that supports site-aware referrals.
    /// </summary>
    public class RequestGetDfsReferralEx
    {
        public ushort MaxReferralLevel;
        public RequestGetDfsReferralExFlags Flags;
        public string RequestFileName; // Unicode
        public string SiteName;        // Optional, Unicode, null-terminated (when flag is set)

        public RequestGetDfsReferralEx()
        {
        }

        public RequestGetDfsReferralEx(byte[] buffer)
        {
            MaxReferralLevel = LittleEndianConverter.ToUInt16(buffer, 0);
            Flags = (RequestGetDfsReferralExFlags)LittleEndianConverter.ToUInt16(buffer, 2);
            uint requestDataLength = LittleEndianConverter.ToUInt32(buffer, 4);
            ushort requestFileNameLength = LittleEndianConverter.ToUInt16(buffer, 8);
            int dataOffset = 10;
            RequestFileName = ByteReader.ReadNullTerminatedUTF16String(buffer, ref dataOffset);
            if ((Flags & RequestGetDfsReferralExFlags.SiteName) != 0)
            {
                ushort siteNameLength = LittleEndianReader.ReadUInt16(buffer, ref dataOffset);
                SiteName = ByteReader.ReadNullTerminatedUTF16String(buffer, ref dataOffset);
            }
        }

        public byte[] GetBytes()
        {
            int length = 10 + RequestFileName.Length * 2 + 2;
            if ((Flags & RequestGetDfsReferralExFlags.SiteName) != 0)
            {
                length += 2 + SiteName.Length * 2 + 2;
            }

            byte[] buffer = new byte[length];
            LittleEndianWriter.WriteUInt16(buffer, 0, MaxReferralLevel);
            LittleEndianWriter.WriteUInt16(buffer, 2, (ushort)Flags);
            LittleEndianWriter.WriteUInt32(buffer, 4, (uint)(length - 8));
            LittleEndianWriter.WriteUInt16(buffer, 8, (ushort)RequestFileName.Length);
            int dataOffset = 10;
            ByteWriter.WriteNullTerminatedUTF16String(buffer, ref dataOffset, RequestFileName);
            if ((Flags & RequestGetDfsReferralExFlags.SiteName) != 0)
            {
                LittleEndianWriter.WriteUInt16(buffer, ref dataOffset, (ushort)SiteName.Length);
                ByteWriter.WriteNullTerminatedUTF16String(buffer, ref dataOffset, SiteName);
            }

            return buffer;
        }
    }
}
