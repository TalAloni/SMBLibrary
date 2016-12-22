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
    /// [MS-DTYP] ACL (Access Control List)
    /// </summary>
    public class ACL : List<ACE>
    {
        public byte AclRevision;
        public byte Sbz1;
        //ushort AclSize;
        //ushort AceCount;
        public ushort Sbz2;

        public ACL()
        {
        }

        public ACL(byte[] buffer, int offset)
        {
            AclRevision = ByteReader.ReadByte(buffer, offset + 0);
            Sbz1 = ByteReader.ReadByte(buffer, offset + 1);
            ushort AclSize = LittleEndianConverter.ToUInt16(buffer, offset + 2);
            ushort AceCount = LittleEndianConverter.ToUInt16(buffer, offset + 4);
            Sbz2 = LittleEndianConverter.ToUInt16(buffer, offset + 6);

            offset += 8;
            for (int index = 0; index < AceCount; index++)
            {
                ACE ace = ACE.GetAce(buffer, offset);
                this.Add(ace);
                offset += ace.Length;
            }
        }

        public void WriteBytes(byte[] buffer, ref int offset)
        {
            throw new NotImplementedException();
        }

        public int Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
