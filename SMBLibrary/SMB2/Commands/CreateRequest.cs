/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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
    /// SMB2 CREATE Request
    /// </summary>
    public class CreateRequest : SMB2Command
    {
        public const int FixedLength = 56;
        public const int DeclaredSize = 57;

        private ushort StructureSize;
        public byte SecurityFlags; // Reserved
        public OplockLevel RequestedOplockLevel;
        public ImpersonationLevel ImpersonationLevel;
        public ulong SmbCreateFlags;
        public ulong Reserved;
        public AccessMask DesiredAccess;
        public FileAttributes FileAttributes;
        public ShareAccess ShareAccess;
        public CreateDisposition CreateDisposition;
        public CreateOptions CreateOptions;
        private ushort NameOffset;
        private ushort NameLength;
        public string Name;
        private uint CreateContextsOffsets;
        private uint CreateContextsLength;
        public List<CreateContext> CreateContexts = new List<CreateContext>();

        public CreateRequest() : base(SMB2CommandName.Create)
        {
            StructureSize = DeclaredSize;
        }

        public CreateRequest(byte[] buffer, int offset) : base(buffer, offset)
        {
            StructureSize = LittleEndianConverter.ToUInt16(buffer, offset + SMB2Header.Length + 0);
            SecurityFlags = ByteReader.ReadByte(buffer, offset + SMB2Header.Length + 2);
            RequestedOplockLevel = (OplockLevel)ByteReader.ReadByte(buffer, offset + SMB2Header.Length + 3);
            ImpersonationLevel = (ImpersonationLevel)LittleEndianConverter.ToUInt32(buffer, offset + SMB2Header.Length + 4);
            SmbCreateFlags = LittleEndianConverter.ToUInt64(buffer, offset + SMB2Header.Length + 8);
            Reserved = LittleEndianConverter.ToUInt64(buffer, offset + SMB2Header.Length + 16);
            DesiredAccess = (AccessMask)LittleEndianConverter.ToUInt32(buffer, offset + SMB2Header.Length + 24);
            FileAttributes = (FileAttributes)LittleEndianConverter.ToUInt32(buffer, offset + SMB2Header.Length + 28);
            ShareAccess = (ShareAccess)LittleEndianConverter.ToUInt32(buffer, offset + SMB2Header.Length + 32);
            CreateDisposition = (CreateDisposition)LittleEndianConverter.ToUInt32(buffer, offset + SMB2Header.Length + 36);
            CreateOptions = (CreateOptions)LittleEndianConverter.ToUInt32(buffer, offset + SMB2Header.Length + 40);
            NameOffset = LittleEndianConverter.ToUInt16(buffer, offset + SMB2Header.Length + 44);
            NameLength = LittleEndianConverter.ToUInt16(buffer, offset + SMB2Header.Length + 46);
            CreateContextsOffsets = LittleEndianConverter.ToUInt32(buffer, offset + SMB2Header.Length + 48);
            CreateContextsLength = LittleEndianConverter.ToUInt32(buffer, offset + SMB2Header.Length + 52);
            Name = ByteReader.ReadUTF16String(buffer, offset + NameOffset, NameLength / 2);
            if (CreateContextsLength > 0)
            {
                CreateContexts = CreateContext.ReadCreateContextList(buffer, (int)CreateContextsOffsets);
            }
        }

        public override void WriteCommandBytes(byte[] buffer, int offset)
        {
            NameOffset = 0;
            NameLength = (ushort)(Name.Length * 2);
            if (Name.Length > 0)
            {
                NameOffset = SMB2Header.Length + FixedLength;
            }
            CreateContextsOffsets = 0;
            CreateContextsLength = 0;
            int paddedNameLength = (int)Math.Ceiling((double)(Name.Length * 2) / 8) * 8;
            if (CreateContexts.Count > 0)
            {
                CreateContextsOffsets = (uint)(SMB2Header.Length + 56 + paddedNameLength);
                CreateContextsLength = (uint)CreateContext.GetCreateContextListLength(CreateContexts);
            }
            LittleEndianWriter.WriteUInt16(buffer, offset + 0, StructureSize);
            ByteWriter.WriteByte(buffer, offset + 2, SecurityFlags);
            ByteWriter.WriteByte(buffer, offset + 3, (byte)RequestedOplockLevel);
            LittleEndianWriter.WriteUInt32(buffer, offset + 4, (uint)ImpersonationLevel);
            LittleEndianWriter.WriteUInt64(buffer, offset + 8, (ulong)SmbCreateFlags);
            LittleEndianWriter.WriteUInt64(buffer, offset + 16, (ulong)Reserved);
            LittleEndianWriter.WriteUInt32(buffer, offset + 24, (uint)DesiredAccess);
            LittleEndianWriter.WriteUInt32(buffer, offset + 28, (uint)FileAttributes);
            LittleEndianWriter.WriteUInt32(buffer, offset + 32, (uint)ShareAccess);
            LittleEndianWriter.WriteUInt32(buffer, offset + 36, (uint)CreateDisposition);
            LittleEndianWriter.WriteUInt32(buffer, offset + 40, (uint)CreateOptions);
            LittleEndianWriter.WriteUInt16(buffer, offset + 44, NameOffset);
            LittleEndianWriter.WriteUInt16(buffer, offset + 46, NameLength);
            LittleEndianWriter.WriteUInt32(buffer, offset + 48, CreateContextsOffsets);
            LittleEndianWriter.WriteUInt32(buffer, offset + 52, CreateContextsLength);
            ByteWriter.WriteUTF16String(buffer, offset + 56, Name);
            CreateContext.WriteCreateContextList(buffer, offset + 56 + paddedNameLength, CreateContexts);
        }

        public override int CommandLength
        {
            get
            {
                if (CreateContexts.Count == 0)
                {
                    return FixedLength + Name.Length * 2;
                }
                else
                {
                    int paddedNameLength = (int)Math.Ceiling((double)(Name.Length * 2) / 8) * 8;
                    return FixedLength + paddedNameLength + CreateContext.GetCreateContextListLength(CreateContexts);
                }
            }
        }
    }
}
