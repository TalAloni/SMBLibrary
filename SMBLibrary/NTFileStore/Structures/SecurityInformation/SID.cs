/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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
        public static readonly byte[] WORLD_SID_AUTHORITY = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
        public static readonly byte[] LOCAL_SID_AUTHORITY = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x02 };
        public static readonly byte[] CREATOR_SID_AUTHORITY = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x02 };
        public static readonly byte[] SECURITY_NT_AUTHORITY = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x05 };

        public const int FixedLength = 8;

        public byte Revision;
        // byte SubAuthorityCount;
        public byte[] IdentifierAuthority; // 6 bytes
        public List<uint> SubAuthority = new List<uint>();

        public SID()
        {
            Revision = 0x01;
        }

        public SID(string sidString)
        {
            if (!sidString.StartsWith("S-", StringComparison.InvariantCultureIgnoreCase))
                throw new ApplicationException("The SID " + sidString + " does not start with S-");
            string[] s = sidString.Split('-');
            if (s.Length < 4)
                throw new ApplicationException("The SID " + sidString + " cannot be splitted in subauthorities");
            Revision = (byte) int.Parse(s[1]);
            if (int.Parse(s[2]) != 5)
                throw new ApplicationException("The SID " + sidString + " has an unsupported Authority (<> 5)");
            IdentifierAuthority = SECURITY_NT_AUTHORITY;
            for (int i = 3; i < s.Length; i++)
            {
                SubAuthority.Add(uint.Parse(s[i]));
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("S-");
            sb.Append(Revision);
            sb.Append("-");
            sb.Append(IdentifierAuthority[IdentifierAuthority.Length - 1]);
            foreach (uint subA in SubAuthority)
            {
                sb.Append("-");
                sb.Append(subA);
            }
            return sb.ToString();
        }

        public SID(byte[] buffer, int offset)
        {
            Read(buffer, offset);
        }

        public void Read(byte[] buffer, int offset)
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

        // Build a SID from an existing SID and its relative ID.
        // Avoid to perform a ToString, concat, then Constructor again
        public SID ChildSID(uint RelativeId)
        {
            SID sid = new SID();
            sid.Revision = Revision;
            sid.IdentifierAuthority = IdentifierAuthority;
            sid.SubAuthority = new List<uint>(SubAuthority);
            sid.SubAuthority.Add(RelativeId);
            return sid;
        }

        public int Length
        {
            get
            {
                return FixedLength + SubAuthority.Count * 4;
            }
        }

        public static SID Everyone
        {
            get
            {
                SID sid = new SID();
                sid.IdentifierAuthority = WORLD_SID_AUTHORITY;
                sid.SubAuthority.Add(0);
                return sid;
            }
        }

        public static SID LocalSystem
        {
            get
            {
                SID sid = new SID();
                sid.IdentifierAuthority = SECURITY_NT_AUTHORITY;
                sid.SubAuthority.Add(18);
                return sid;
            }
        }
    }
}
