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
            // Per MS-DFSC 2.2.3.1, RequestData contains:
            //   RequestFileNameLength (2 bytes)
            //   RequestFileName (variable, Unicode null-terminated)
            //   SiteNameLength (2 bytes) - only if SiteName flag set
            //   SiteName (variable, Unicode null-terminated) - only if SiteName flag set
            int offset = 8;
            
            if (buffer.Length < offset + 2)
            {
                throw new ArgumentException("Buffer too small for RequestFileNameLength", "buffer");
            }
            
            ushort requestFileNameLength = LittleEndianConverter.ToUInt16(buffer, offset);
            offset += 2;
            
            // Read RequestFileName (null-terminated Unicode string)
            RequestFileName = ByteReader.ReadNullTerminatedUTF16String(buffer, offset);
            // Advance past the string including null terminator
            int fileNameBytesWithNull = (RequestFileName != null ? RequestFileName.Length + 1 : 1) * 2;
            offset += fileNameBytesWithNull;

            if ((requestFlags & SiteNameFlag) != 0)
            {
                if (buffer.Length >= offset + 2)
                {
                    ushort siteNameLength = LittleEndianConverter.ToUInt16(buffer, offset);
                    offset += 2;
                    
                    if (offset < buffer.Length)
                    {
                        SiteName = ByteReader.ReadNullTerminatedUTF16String(buffer, offset);
                    }
                }
            }
        }

        public byte[] GetBytes()
        {
            bool hasSiteName = !string.IsNullOrEmpty(SiteName);
            ushort requestFlags = hasSiteName ? SiteNameFlag : (ushort)0;

            // Calculate RequestData length per MS-DFSC 2.2.3.1:
            //   RequestFileNameLength (2 bytes)
            //   RequestFileName (variable, Unicode null-terminated)
            //   SiteNameLength (2 bytes) - only if SiteName flag set
            //   SiteName (variable, Unicode null-terminated) - only if SiteName flag set
            int requestFileNameBytes = (RequestFileName != null ? RequestFileName.Length + 1 : 1) * 2;
            int siteNameBytes = hasSiteName ? (SiteName.Length + 1) * 2 : 0;
            
            // Include length fields in RequestDataLength
            uint requestDataLength = (uint)(2 + requestFileNameBytes + (hasSiteName ? 2 + siteNameBytes : 0));

            int totalLength = 8 + (int)requestDataLength;
            byte[] buffer = new byte[totalLength];

            // Header
            LittleEndianWriter.WriteUInt16(buffer, 0, MaxReferralLevel);
            LittleEndianWriter.WriteUInt16(buffer, 2, requestFlags);
            LittleEndianWriter.WriteUInt32(buffer, 4, requestDataLength);

            // RequestData
            int offset = 8;
            
            // Write RequestFileNameLength
            LittleEndianWriter.WriteUInt16(buffer, offset, (ushort)requestFileNameBytes);
            offset += 2;
            
            // Write RequestFileName (null-terminated Unicode)
            if (RequestFileName != null)
            {
                ByteWriter.WriteUTF16String(buffer, offset, RequestFileName);
                offset += requestFileNameBytes;
            }
            else
            {
                // Write null terminator only
                offset += 2;
            }

            if (hasSiteName)
            {
                // Write SiteNameLength
                LittleEndianWriter.WriteUInt16(buffer, offset, (ushort)siteNameBytes);
                offset += 2;
                
                // Write SiteName (null-terminated Unicode)
                ByteWriter.WriteUTF16String(buffer, offset, SiteName);
            }

            return buffer;
        }
    }
}
