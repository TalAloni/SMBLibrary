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
    /// SMB_COM_DELETE Request
    /// </summary>
    public class DeleteRequest : SMB1Command
    {
        public const int SupportedBufferFormat = 0x04;
        public const int ParametersLength = 2;
        // Parameters;
        public FileAttributes SearchAttributes;
        // Data:
        public byte BufferFormat;
        public string FileName; // SMB_STRING

        public DeleteRequest() : base()
        {
            BufferFormat = SupportedBufferFormat;
        }

        public DeleteRequest(byte[] buffer, int offset, bool isUnicode) : base(buffer, offset, isUnicode)
        {
            SearchAttributes = (FileAttributes)LittleEndianConverter.ToUInt16(this.SMBParameters, 0);

            BufferFormat = ByteReader.ReadByte(this.SMBData, 0);
            if (BufferFormat != SupportedBufferFormat)
            {
                throw new InvalidRequestException("Unsupported Buffer Format");
            }
            FileName = SMBHelper.ReadSMBString(this.SMBData, 1, isUnicode);
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            throw new NotImplementedException();
        }

        public override CommandName CommandName
        {
            get
            {
                return CommandName.SMB_COM_DELETE;
            }
        }
    }
}
