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

namespace SMBLibrary.RPC
{
    public class NDRSID : INDRStructure
    {
        SID sid;

        public NDRSID()
        {
            sid = new SID();
        }

        public NDRSID(SID sid)
        {
            this.sid = sid;
        }

        public void Read(NDRParser parser)
        {
            uint subAuthorityCount = parser.ReadUInt32();
            byte[] buffer = new byte[SID.FixedLength + subAuthorityCount * 4];
            parser.ReadBytes(buffer);
            sid.Read(buffer, 0);
        }

        public void Write(NDRWriter writer)
        {
            byte[] data = new byte[sid.Length];
            int offset = 0;
            sid.WriteBytes(data, ref offset);
            writer.WriteUInt32((uint)sid.SubAuthority.Count);
            writer.WriteByteArray(data);
        }
    }
}
