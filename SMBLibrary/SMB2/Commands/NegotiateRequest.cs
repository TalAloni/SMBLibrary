/* Copyright (C) 2017-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using Utilities;

namespace SMBLibrary.SMB2
{
    /// <summary>
    /// SMB2 NEGOTIATE Request
    /// </summary>
    public class NegotiateRequest : SMB2Command
    {
        public const int DeclaredSize = 36;

        private ushort StructureSize;
        // ushort DialectCount;
        public SecurityMode SecurityMode;
        public ushort Reserved;
        public Capabilities Capabilities; // If the client does not implements the SMB 3.x dialect family, this field MUST be set to 0.
        public Guid ClientGuid;
        public DateTime ClientStartTime; // If Dialects does not contain SMB311
        public List<SMB2Dialect> Dialects = new List<SMB2Dialect>();
        public List<NegotiateContext> NegotiateContextList = new List<NegotiateContext>(); // If Dialects contains SMB311

        public NegotiateRequest() : base(SMB2CommandName.Negotiate)
        {
            StructureSize = DeclaredSize;
        }

        public NegotiateRequest(byte[] buffer, int offset) : base(buffer, offset)
        {
            StructureSize = LittleEndianConverter.ToUInt16(buffer, offset + SMB2Header.Length + 0);
            ushort dialectCount = LittleEndianConverter.ToUInt16(buffer, offset + SMB2Header.Length + 2);
            SecurityMode = (SecurityMode)LittleEndianConverter.ToUInt16(buffer, offset + SMB2Header.Length + 4);
            Reserved = LittleEndianConverter.ToUInt16(buffer, offset + SMB2Header.Length + 6);
            Capabilities = (Capabilities)LittleEndianConverter.ToUInt32(buffer, offset + SMB2Header.Length + 8);
            ClientGuid = LittleEndianConverter.ToGuid(buffer, offset + SMB2Header.Length + 12);

            bool containsNegotiateContextList = false;
            for (int index = 0; index < dialectCount; index++)
            {
                SMB2Dialect dialect = (SMB2Dialect)LittleEndianConverter.ToUInt16(buffer, offset + SMB2Header.Length + 36 + index * 2);
                Dialects.Add(dialect);

                if (dialect == SMB2Dialect.SMB311)
                {
                    containsNegotiateContextList = true;
                }
            }

            if (containsNegotiateContextList)
            {
                uint negotiateContextOffset = LittleEndianConverter.ToUInt32(buffer, offset + SMB2Header.Length + 28);
                ushort NegotiateContextCount = LittleEndianConverter.ToUInt16(buffer, offset + SMB2Header.Length + 32);
                NegotiateContextList = NegotiateContext.ReadNegotiateContextList(buffer, offset + (int)negotiateContextOffset, NegotiateContextCount);
            }
            else
            {
                ClientStartTime = DateTime.FromFileTimeUtc(LittleEndianConverter.ToInt64(buffer, offset + SMB2Header.Length + 28));
            }
        }

        public override void WriteCommandBytes(byte[] buffer, int offset)
        {
            LittleEndianWriter.WriteUInt16(buffer, offset + 0, StructureSize);
            LittleEndianWriter.WriteUInt16(buffer, offset + 2, (ushort)Dialects.Count);
            LittleEndianWriter.WriteUInt16(buffer, offset + 4, (ushort)SecurityMode);
            LittleEndianWriter.WriteUInt16(buffer, offset + 6, Reserved);
            LittleEndianWriter.WriteUInt32(buffer, offset + 8, (uint)Capabilities);
            LittleEndianWriter.WriteGuid(buffer, offset + 12, ClientGuid);

            bool containsNegotiateContextList = false;
            for (int index = 0; index < Dialects.Count; index++)
            {
                SMB2Dialect dialect = Dialects[index];
                LittleEndianWriter.WriteUInt16(buffer, offset + 36 + index * 2, (ushort)dialect);

                if (dialect == SMB2Dialect.SMB311)
                {
                    containsNegotiateContextList = true;
                }
            }

            int paddingLength = (8 - ((36 + Dialects.Count * 2) % 8)) % 8;
            if (containsNegotiateContextList)
            {
                uint negotiateContextOffset = (uint)(SMB2Header.Length + 36 + Dialects.Count * 2 + paddingLength);
                ushort negotiateContextCount = (ushort)NegotiateContextList.Count;
                LittleEndianWriter.WriteUInt32(buffer, offset + 28, negotiateContextOffset);
                LittleEndianWriter.WriteUInt16(buffer, offset + 32, negotiateContextCount);
                NegotiateContext.WriteNegotiateContextList(buffer, offset - SMB2Header.Length + (int)negotiateContextOffset, NegotiateContextList);
            }
            else
            {
                LittleEndianWriter.WriteInt64(buffer, offset + 28, ClientStartTime.ToFileTimeUtc());
            }
        }

        public override int CommandLength
        {
            get
            {
                bool containsNegotiateContextList = Dialects.Contains(SMB2Dialect.SMB311);
                if (containsNegotiateContextList)
                {
                    int paddingLength = (8 - ((36 + Dialects.Count * 2) % 8)) % 8;
                    int negotiateContextListLength = NegotiateContext.GetNegotiateContextListLength(NegotiateContextList);
                    return 36 + Dialects.Count * 2 + paddingLength + negotiateContextListLength;
                }
                else
                {
                    return 36 + Dialects.Count * 2;
                }
            }
        }
    }
}
