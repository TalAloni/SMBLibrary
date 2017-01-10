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
    /// SMB_INFO_VOLUME
    /// </summary>
    public class QueryFSInfoVolume : QueryFSInformation
    {
        public uint VolumeSerialNumber;
        //byte CharCount;
        public string VolumeLabel; // SMB_STRING

        public QueryFSInfoVolume()
        {
        }

        public QueryFSInfoVolume(bool isUnicode, byte[] buffer, int offset)
        {
            VolumeSerialNumber = LittleEndianConverter.ToUInt32(buffer, offset + 0);
            byte charCount = ByteReader.ReadByte(buffer, offset + 4);
            VolumeLabel = SMB1Helper.ReadSMBString(buffer, offset + 5, isUnicode);
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            byte charCount = (byte)VolumeLabel.Length;

            int length = GetLength(isUnicode);
            byte[] buffer = new byte[length];
            LittleEndianWriter.WriteUInt32(buffer, 0, VolumeSerialNumber);
            ByteWriter.WriteByte(buffer, 4, charCount);
            SMB1Helper.WriteSMBString(buffer, 5, isUnicode, VolumeLabel);
            return buffer;
        }

        public int GetLength(bool isUnicode)
        {
            int length = 5;
            if (isUnicode)
            {
                length += VolumeLabel.Length * 2;
            }
            else
            {
                length += VolumeLabel.Length;
            }
            return length;
        }
    }
}
