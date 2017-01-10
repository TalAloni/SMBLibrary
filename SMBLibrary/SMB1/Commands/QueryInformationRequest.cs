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
    /// SMB_COM_QUERY_INFORMATION Request.
    /// This command is deprecated.
    /// This command is used by Windows NT4 SP6.
    /// </summary>
    public class QueryInformationRequest : SMB1Command
    {
        public const byte SupportedBufferFormat = 0x04;
        // Data:
        public byte BufferFormat;
        public string FileName; // SMB_STRING

        public QueryInformationRequest() : base()
        {
            BufferFormat = SupportedBufferFormat;
            FileName = String.Empty;
        }

        public QueryInformationRequest(byte[] buffer, int offset, bool isUnicode) : base(buffer, offset, isUnicode)
        {
            BufferFormat = ByteReader.ReadByte(this.SMBData, 0);
            if (BufferFormat != SupportedBufferFormat)
            {
                throw new InvalidRequestException("Unsupported Buffer Format");
            }
            FileName = SMBHelper.ReadSMBString(this.SMBData, 1, isUnicode);
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            int length = 1;
            if (isUnicode)
            {
                length += FileName.Length * 2 + 2;
            }
            else
            {
                length += FileName.Length + 1;
            }
            this.SMBData = new byte[1 + length];
            ByteWriter.WriteByte(this.SMBData, 0, BufferFormat);
            SMBHelper.WriteSMBString(this.SMBData, 1, isUnicode, FileName);

            return base.GetBytes(isUnicode);
        }

        public override CommandName CommandName
        {
            get
            {
                return CommandName.SMB_COM_QUERY_INFORMATION;
            }
        }
    }
}
