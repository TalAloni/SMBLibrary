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
    /// SMB_INFO_STANDARD
    /// </summary>
    public class SetInfoStandard : SetInformation
    {
        public const int Length = 22;

        public DateTime CreationDateTime;
        public DateTime LastAccessDateTime;
        public DateTime LastWriteDateTime;
        public byte[] Reserved; // 10 bytes

        public SetInfoStandard()
        {
            Reserved = new byte[10];
        }

        public SetInfoStandard(byte[] buffer) : this(buffer, 0)
        {
        }

        public SetInfoStandard(byte[] buffer, int offset)
        {
            CreationDateTime = SMB1Helper.ReadSMBDateTime(buffer, offset + 0);
            LastAccessDateTime = SMB1Helper.ReadSMBDateTime(buffer, offset + 4);
            LastWriteDateTime = SMB1Helper.ReadSMBDateTime(buffer, offset + 8);
            Reserved = ByteReader.ReadBytes(buffer, offset + 12, 10);
        }

        public override byte[] GetBytes()
        {
            byte[] buffer = new byte[Length];
            SMB1Helper.WriteSMBDateTime(buffer, 0, CreationDateTime);
            SMB1Helper.WriteSMBDateTime(buffer, 4, LastAccessDateTime);
            SMB1Helper.WriteSMBDateTime(buffer, 8, LastWriteDateTime);
            ByteWriter.WriteBytes(buffer, 12, Reserved);
            return buffer;
        }

        public override SetInformationLevel InformationLevel
        {
            get
            {
                return SetInformationLevel.SMB_INFO_STANDARD;
            }
        }
    }
}
