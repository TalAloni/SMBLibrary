/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using Utilities;

namespace SMBLibrary
{
    /// <summary>
    /// [MS-FSCC] 2.4.6 - FileAttributeTagInformation
    /// </summary>
    public class FileAttributeTagInformation : FileInformation
    {
        public const int FixedLength = 8;

        public FileAttributes FileAttributes;
        public uint ReparsePointTag;

        public FileAttributeTagInformation()
        {
        }

        public FileAttributeTagInformation(byte[] buffer, int offset)
        {
            FileAttributes = (FileAttributes)LittleEndianConverter.ToUInt32(buffer, offset + 0);
            ReparsePointTag = LittleEndianConverter.ToUInt32(buffer, offset + 4);
        }

        public override void WriteBytes(byte[] buffer, int offset)
        {
            LittleEndianWriter.WriteUInt32(buffer, offset + 0, (uint)FileAttributes);
            LittleEndianWriter.WriteUInt32(buffer, offset + 4, ReparsePointTag);
        }

        public override FileInformationClass FileInformationClass
        {
            get
            {
                return FileInformationClass.FileAttributeTagInformation;
            }
        }

        public override int Length
        {
            get
            {
                return FixedLength;
            }
        }
    }
}
