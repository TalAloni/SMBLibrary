/* Copyright (C) 2025 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using Utilities;

namespace SMBLibrary.SMB1
{
    /// <summary>
    /// SMB_COM_QUERY_INFORMATION_DISK Response.
    /// This command is deprecated.
    /// This command is used by Windows 98 SE.
    /// </summary>
    public class QueryInformationDiskResponse : SMB1Command
    {
        public const int ParameterLength = 10;
        // Parameters:
        public ushort TotalUnits;
        public ushort BlocksPerUnit;
        public ushort BlockSize;
        public ushort FreeUnits;
        public ushort Reserved;

        public QueryInformationDiskResponse()
        {
        }

        public QueryInformationDiskResponse(byte[] buffer, int offset) : base(buffer, offset, false)
        {
            TotalUnits = LittleEndianConverter.ToUInt16(this.SMBParameters, 0);
            BlocksPerUnit = LittleEndianConverter.ToUInt16(this.SMBParameters, 2);
            BlockSize = LittleEndianConverter.ToUInt16(this.SMBParameters, 4);
            FreeUnits = LittleEndianConverter.ToUInt16(this.SMBParameters, 6);
            Reserved = LittleEndianConverter.ToUInt16(this.SMBParameters, 8);
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            this.SMBParameters = new byte[ParameterLength];
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 0, TotalUnits);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 2, BlocksPerUnit);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 4, BlockSize);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 6, FreeUnits);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 8, Reserved);

            return base.GetBytes(isUnicode);
        }

        public override CommandName CommandName
        {
            get
            {
                return CommandName.SMB_COM_QUERY_INFORMATION_DISK;
            }
        }
    }
}
