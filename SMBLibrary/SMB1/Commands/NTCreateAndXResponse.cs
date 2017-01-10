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

namespace SMBLibrary.SMB1
{
    /// <summary>
    /// SMB_COM_NT_CREATE_ANDX Response
    /// </summary>
    public class NTCreateAndXResponse : SMBAndXCommand
    {
        public const int ParametersLength = 68;
        // Parameters:
        //CommandName AndXCommand;
        //byte AndXReserved;
        //ushort AndXOffset;
        public OpLockLevel OpLockLevel;
        public ushort FID;
        public CreateDisposition CreateDisposition;
        public DateTime CreateTime;
        public DateTime LastAccessTime;
        public DateTime LastWriteTime;
        public DateTime LastChangeTime;
        public ExtendedFileAttributes ExtFileAttributes;
        public ulong AllocationSize;
        public ulong EndOfFile;
        public ResourceType ResourceType;
        public NamedPipeStatus NMPipeStatus;
        public bool Directory;

        public NTCreateAndXResponse() : base()
        {
            CreateTime = SMB1Helper.FileTimeNotSpecified;
            LastAccessTime = SMB1Helper.FileTimeNotSpecified;
            LastWriteTime = SMB1Helper.FileTimeNotSpecified;
            LastChangeTime = SMB1Helper.FileTimeNotSpecified;
        }

        public NTCreateAndXResponse(byte[] buffer, int offset) : base(buffer, offset, false)
        {
            int parametersOffset = 4;
            OpLockLevel = (OpLockLevel)ByteReader.ReadByte(this.SMBParameters, ref parametersOffset);
            FID = LittleEndianReader.ReadUInt16(this.SMBParameters, ref parametersOffset);
            CreateDisposition = (CreateDisposition)LittleEndianReader.ReadUInt32(this.SMBParameters, ref parametersOffset);
            CreateTime = SMB1Helper.ReadFileTime(buffer, ref parametersOffset);
            LastAccessTime = SMB1Helper.ReadFileTime(buffer, ref parametersOffset);
            LastWriteTime = SMB1Helper.ReadFileTime(buffer, ref parametersOffset);
            LastChangeTime = SMB1Helper.ReadFileTime(buffer, ref parametersOffset);
            ExtFileAttributes = (ExtendedFileAttributes)LittleEndianReader.ReadUInt32(this.SMBParameters, ref parametersOffset);
            AllocationSize = LittleEndianReader.ReadUInt64(buffer, ref parametersOffset);
            EndOfFile = LittleEndianReader.ReadUInt64(buffer, ref parametersOffset);
            ResourceType = (ResourceType)LittleEndianReader.ReadUInt16(this.SMBParameters, ref parametersOffset);
            NMPipeStatus = NamedPipeStatus.Read(this.SMBParameters, ref parametersOffset);
            Directory = (ByteReader.ReadByte(this.SMBParameters, ref parametersOffset) > 0);
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            this.SMBParameters = new byte[ParametersLength];
            int parametersOffset = 4;
            ByteWriter.WriteByte(this.SMBParameters, ref parametersOffset, (byte)OpLockLevel);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, ref parametersOffset, FID);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, ref parametersOffset, (uint)CreateDisposition);
            SMB1Helper.WriteFileTime(this.SMBParameters, ref parametersOffset, CreateTime);
            SMB1Helper.WriteFileTime(this.SMBParameters, ref parametersOffset, LastAccessTime);
            SMB1Helper.WriteFileTime(this.SMBParameters, ref parametersOffset, LastWriteTime);
            SMB1Helper.WriteFileTime(this.SMBParameters, ref parametersOffset, LastChangeTime);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, ref parametersOffset, (uint)ExtFileAttributes);
            LittleEndianWriter.WriteUInt64(this.SMBParameters, ref parametersOffset, AllocationSize);
            LittleEndianWriter.WriteUInt64(this.SMBParameters, ref parametersOffset, EndOfFile);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, ref parametersOffset, (ushort)ResourceType);
            NMPipeStatus.WriteBytes(this.SMBParameters, ref parametersOffset);
            ByteWriter.WriteByte(this.SMBParameters, ref parametersOffset, Convert.ToByte(Directory));
            return base.GetBytes(isUnicode);
        }

        public override CommandName CommandName
        {
            get
            {
                return CommandName.SMB_COM_NT_CREATE_ANDX;
            }
        }
    }
}
