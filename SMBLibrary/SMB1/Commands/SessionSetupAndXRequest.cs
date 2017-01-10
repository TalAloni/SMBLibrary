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
    /// SMB_COM_SESSION_SETUP_ANDX Request
    /// </summary>
    public class SessionSetupAndXRequest : SMBAndXCommand
    {
        public const int ParametersLength = 26;
        // Parameters:
        public ushort MaxBufferSize;
        public ushort MaxMpxCount;
        public ushort VcNumber;
        public uint SessionKey;
        //ushort OEMPasswordLength;
        //ushort UnicodePasswordLength;
        public uint Reserved;
        public ServerCapabilities Capabilities;
        // Data:
        public byte[] OEMPassword;
        public byte[] UnicodePassword;
        // Padding
        public string AccountName;   // SMB_STRING (If Unicode, this field MUST be aligned to start on a 2-byte boundary from the start of the SMB header)
        public string PrimaryDomain; // SMB_STRING (this field WILL be aligned to start on a 2-byte boundary from the start of the SMB header)
        public string NativeOS;      // SMB_STRING (this field WILL be aligned to start on a 2-byte boundary from the start of the SMB header)
        public string NativeLanMan;  // SMB_STRING (this field WILL be aligned to start on a 2-byte boundary from the start of the SMB header)

        public SessionSetupAndXRequest(byte[] buffer, int offset, bool isUnicode) : base(buffer, offset, isUnicode)
        {
            MaxBufferSize = LittleEndianConverter.ToUInt16(this.SMBParameters, 4);
            MaxMpxCount = LittleEndianConverter.ToUInt16(this.SMBParameters, 6);
            VcNumber = LittleEndianConverter.ToUInt16(this.SMBParameters, 8);
            SessionKey = LittleEndianConverter.ToUInt32(this.SMBParameters, 10);
            ushort OEMPasswordLength = LittleEndianConverter.ToUInt16(this.SMBParameters, 14);
            ushort UnicodePasswordLength = LittleEndianConverter.ToUInt16(this.SMBParameters, 16);
            Reserved = LittleEndianConverter.ToUInt32(this.SMBParameters, 18);
            Capabilities = (ServerCapabilities)LittleEndianConverter.ToUInt32(this.SMBParameters, 22);

            OEMPassword = ByteReader.ReadBytes(this.SMBData, 0, OEMPasswordLength);
            UnicodePassword = ByteReader.ReadBytes(this.SMBData, OEMPasswordLength, UnicodePasswordLength);

            int dataOffset = OEMPasswordLength + UnicodePasswordLength;
            if (isUnicode)
            {
                // wordCount is 1 byte
                int padding = (1 + OEMPasswordLength + UnicodePasswordLength) % 2;
                dataOffset += padding;
            }
            AccountName = SMB1Helper.ReadSMBString(this.SMBData, ref dataOffset, isUnicode);
            PrimaryDomain = SMB1Helper.ReadSMBString(this.SMBData, ref dataOffset, isUnicode);
            NativeOS = SMB1Helper.ReadSMBString(this.SMBData, ref dataOffset, isUnicode);
            NativeLanMan = SMB1Helper.ReadSMBString(this.SMBData, ref dataOffset, isUnicode);
        }

        public override CommandName CommandName
        {
            get
            {
                return CommandName.SMB_COM_SESSION_SETUP_ANDX;
            }
        }
    }
}
