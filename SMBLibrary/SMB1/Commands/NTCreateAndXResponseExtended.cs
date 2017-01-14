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

namespace SMBLibrary.SMB1
{
    /// <summary>
    /// SMB_COM_NT_CREATE_ANDX Extended Response
    /// </summary>
    public class NTCreateAndXResponseExtended : SMBAndXCommand
    {
        public const int ParametersLength = 100;
        // Parameters:
        //CommandName AndXCommand;
        //byte AndXReserved;
        //ushort AndXOffset;
        public OpLockLevel OpLockLevel;
        public ushort FID;
        public CreateDisposition CreateDisposition;
        public DateTime? CreateTime;
        public DateTime? LastAccessTime;
        public DateTime? LastWriteTime;
        public DateTime? LastChangeTime;
        public ExtendedFileAttributes ExtFileAttributes;
        public ulong AllocationSize;
        public ulong EndOfFile;
        public ResourceType ResourceType;
        public ushort NMPipeStatus_or_FileStatusFlags;
        public bool Directory;
        public Guid VolumeGuid;
        public ulong FileID;
        public AccessMask MaximalAccessRights;
        public AccessMask GuestMaximalAccessRights;
        
        public NTCreateAndXResponseExtended() : base()
        {
        }

        public NTCreateAndXResponseExtended(byte[] buffer, int offset) : base(buffer, offset, false)
        {
            int parametersOffset = 4;
            OpLockLevel = (OpLockLevel)ByteReader.ReadByte(this.SMBParameters, ref parametersOffset);
            FID = LittleEndianReader.ReadUInt16(this.SMBParameters, ref parametersOffset);
            CreateDisposition = (CreateDisposition)LittleEndianReader.ReadUInt32(this.SMBParameters, ref parametersOffset);
            CreateTime = FileTimeHelper.ReadNullableFileTime(buffer, ref parametersOffset);
            LastAccessTime = FileTimeHelper.ReadNullableFileTime(buffer, ref parametersOffset);
            LastWriteTime = FileTimeHelper.ReadNullableFileTime(buffer, ref parametersOffset);
            LastChangeTime = FileTimeHelper.ReadNullableFileTime(buffer, ref parametersOffset);
            ExtFileAttributes = (ExtendedFileAttributes)LittleEndianReader.ReadUInt32(this.SMBParameters, ref parametersOffset);
            AllocationSize = LittleEndianReader.ReadUInt64(buffer, ref parametersOffset);
            EndOfFile = LittleEndianReader.ReadUInt64(buffer, ref parametersOffset);
            ResourceType = (ResourceType)LittleEndianReader.ReadUInt16(this.SMBParameters, ref parametersOffset);
            NMPipeStatus_or_FileStatusFlags = LittleEndianReader.ReadUInt16(this.SMBParameters, ref parametersOffset);
            Directory = (ByteReader.ReadByte(this.SMBParameters, ref parametersOffset) > 0);
            VolumeGuid = LittleEndianReader.ReadGuid(this.SMBParameters, ref parametersOffset);
            FileID = LittleEndianReader.ReadUInt64(this.SMBParameters, ref parametersOffset);
            MaximalAccessRights = new AccessMask(this.SMBParameters, ref parametersOffset);
            GuestMaximalAccessRights = new AccessMask(this.SMBParameters, ref parametersOffset);
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            this.SMBParameters = new byte[ParametersLength];
            int parametersOffset = 4;
            ByteWriter.WriteByte(this.SMBParameters, ref parametersOffset, (byte)OpLockLevel);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, ref parametersOffset, FID);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, ref parametersOffset, (uint)CreateDisposition);
            FileTimeHelper.WriteFileTime(this.SMBParameters, ref parametersOffset, CreateTime);
            FileTimeHelper.WriteFileTime(this.SMBParameters, ref parametersOffset, LastAccessTime);
            FileTimeHelper.WriteFileTime(this.SMBParameters, ref parametersOffset, LastWriteTime);
            FileTimeHelper.WriteFileTime(this.SMBParameters, ref parametersOffset, LastChangeTime);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, ref parametersOffset, (uint)ExtFileAttributes);
            LittleEndianWriter.WriteUInt64(this.SMBParameters, ref parametersOffset, AllocationSize);
            LittleEndianWriter.WriteUInt64(this.SMBParameters, ref parametersOffset, EndOfFile);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, ref parametersOffset, (ushort)ResourceType);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, ref parametersOffset, NMPipeStatus_or_FileStatusFlags);
            ByteWriter.WriteByte(this.SMBParameters, ref parametersOffset, Convert.ToByte(Directory));
            LittleEndianWriter.WriteGuidBytes(this.SMBParameters, ref parametersOffset, VolumeGuid);
            LittleEndianWriter.WriteUInt64(this.SMBParameters, ref parametersOffset, FileID);
            MaximalAccessRights.WriteBytes(this.SMBParameters, ref parametersOffset);
            GuestMaximalAccessRights.WriteBytes(this.SMBParameters, ref parametersOffset);
            return base.GetBytes(isUnicode);
        }

        public NamedPipeStatus NMPipeStatus
        {
            get
            {
                return new NamedPipeStatus(NMPipeStatus_or_FileStatusFlags);
            }
            set
            {
                NMPipeStatus_or_FileStatusFlags = value.ToUInt16();
            }
        }

        public FileStatus FileStatus
        {
            get
            {
                return (FileStatus)NMPipeStatus_or_FileStatusFlags;
            }
            set
            {
                NMPipeStatus_or_FileStatusFlags = (ushort)value;
            }
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
