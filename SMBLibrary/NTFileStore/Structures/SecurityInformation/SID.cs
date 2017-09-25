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
    /// [MS-DTYP] 2.4.2.2 - SID (Packet Representation)
    /// </summary>
    public class SID
    {
        public byte Revision;
        //byte SubAuthorityCount;
        public byte[] IdentifierAuthority;
        public List<uint> SubAuthority = new List<uint>();

        public SID()
        {
            Revision = 0x01;
            IdentifierAuthority = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x05 };
        }

        public SID(byte[] buffer, int offset)
        {
            Revision = ByteReader.ReadByte(buffer, ref offset);
            byte subAuthorityCount = ByteReader.ReadByte(buffer, ref offset);
            IdentifierAuthority = ByteReader.ReadBytes(buffer, ref offset, 6);
            for (int index = 0; index < subAuthorityCount; index++)
            {
                uint entry = LittleEndianReader.ReadUInt32(buffer, ref offset);
                SubAuthority.Add(entry);
            }
        }

        public void WriteBytes(byte[] buffer, ref int offset)
        {
            byte subAuthorityCount = (byte)SubAuthority.Count;
            ByteWriter.WriteByte(buffer, ref offset, Revision);
            ByteWriter.WriteByte(buffer, ref offset, subAuthorityCount);
            ByteWriter.WriteBytes(buffer, ref offset, IdentifierAuthority, 6);
            for (int index = 0; index < SubAuthority.Count; index++)
            {
                LittleEndianWriter.WriteUInt32(buffer, ref offset, SubAuthority[index]);
            }
        }

        public int Length
        {
            get
            {
                return 8 + SubAuthority.Count * 4;
            }
        }
    }
}
