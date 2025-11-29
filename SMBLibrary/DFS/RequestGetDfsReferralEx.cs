/* Copyright (C) 2014-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using Utilities;

namespace SMBLibrary.DFS
{
    /// <summary>
    /// [MS-DFSC] 2.2.3 REQ_GET_DFS_REFERRAL_EX
    /// Extended DFS referral request that supports site-aware referrals.
    /// </summary>
    public class RequestGetDfsReferralEx
    {
        private const ushort SiteNameFlag = 0x0001;

        public ushort MaxReferralLevel;
        public string RequestFileName; // Unicode, null-terminated
        public string SiteName;        // Optional, Unicode, null-terminated (when flag is set)

        public RequestGetDfsReferralEx()
        {
        }

        public RequestGetDfsReferralEx(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (buffer.Length < 8)
            {
                throw new ArgumentException("Buffer too small for REQ_GET_DFS_REFERRAL_EX header", "buffer");
            }

            MaxReferralLevel = LittleEndianConverter.ToUInt16(buffer, 0);
            ushort requestFlags = LittleEndianConverter.ToUInt16(buffer, 2);
            uint requestDataLength = LittleEndianConverter.ToUInt32(buffer, 4);

            // RequestData starts at offset 8
            int dataOffset = 8;
            RequestFileName = ByteReader.ReadNullTerminatedUTF16String(buffer, dataOffset);

            if ((requestFlags & SiteNameFlag) != 0 && RequestFileName != null)
            {
                // SiteName follows RequestFileName (after its null terminator)
                int siteNameOffset = dataOffset + (RequestFileName.Length + 1) * 2;
                if (siteNameOffset < buffer.Length)
                {
                    SiteName = ByteReader.ReadNullTerminatedUTF16String(buffer, siteNameOffset);
                }
            }
        }

        public byte[] GetBytes()
        {
            bool hasSiteName = !string.IsNullOrEmpty(SiteName);
            ushort requestFlags = hasSiteName ? SiteNameFlag : (ushort)0;

            // Calculate RequestData length
            int requestFileNameBytes = (RequestFileName != null ? RequestFileName.Length + 1 : 1) * 2;
            int siteNameBytes = hasSiteName ? (SiteName.Length + 1) * 2 : 0;
            uint requestDataLength = (uint)(requestFileNameBytes + siteNameBytes);

            int totalLength = 8 + (int)requestDataLength;
            byte[] buffer = new byte[totalLength];

            // Header
            LittleEndianWriter.WriteUInt16(buffer, 0, MaxReferralLevel);
            LittleEndianWriter.WriteUInt16(buffer, 2, requestFlags);
            LittleEndianWriter.WriteUInt32(buffer, 4, requestDataLength);

            // RequestData
            int offset = 8;
            if (RequestFileName != null)
            {
                ByteWriter.WriteUTF16String(buffer, offset, RequestFileName);
                offset += (RequestFileName.Length + 1) * 2;
            }
            else
            {
                // Write null terminator only
                offset += 2;
            }

            if (hasSiteName)
            {
                ByteWriter.WriteUTF16String(buffer, offset, SiteName);
            }

            return buffer;
        }
    }
}
