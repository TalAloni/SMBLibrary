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
    /// [MS-DTYP] SECURITY_DESCRIPTOR
    /// </summary>
    public class SecurityDescriptor
    {
        public byte Revision;
        public byte Sbz1;
        public ushort Control;
        //uint OffsetOwner;
        //uint OffsetGroup;
        //uint OffsetSacl;
        //uint OffsetDacl;
        public SID OwnerSid;
        public SID GroupSid;
        public ACL Sacl;
        public ACL Dacl;

        public SecurityDescriptor()
        {
            Revision = 0x01;
        }

        public SecurityDescriptor(byte[] buffer, int offset)
        {
            Revision = ByteReader.ReadByte(buffer, ref offset);
            Sbz1 = ByteReader.ReadByte(buffer, ref offset);
            Control = LittleEndianReader.ReadUInt16(buffer, ref offset);
            uint offsetOwner = LittleEndianReader.ReadUInt32(buffer, ref offset);
            uint offsetGroup = LittleEndianReader.ReadUInt32(buffer, ref offset);
            uint offsetSacl = LittleEndianReader.ReadUInt32(buffer, ref offset);
            uint offsetDacl = LittleEndianReader.ReadUInt32(buffer, ref offset);
            if (offsetOwner != 0)
            {
                OwnerSid = new SID(buffer, (int)offsetOwner);
            }

            if (offsetGroup != 0)
            {
                GroupSid = new SID(buffer, (int)offsetGroup);
            }

            if (offsetSacl != 0)
            {
                Sacl = new ACL(buffer, (int)offsetSacl);
            }

            if (offsetDacl != 0)
            {
                Dacl = new ACL(buffer, (int)offsetDacl);
            }
        }

        public byte[] GetBytes()
        {
            throw new NotImplementedException();
        }
    }
}
