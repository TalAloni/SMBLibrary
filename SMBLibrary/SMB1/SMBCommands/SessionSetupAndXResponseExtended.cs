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
    public class SessionSetupAndXResponseExtended : SMBAndXCommand
    {
        public const int ParametersLength = 8;
        // Parameters:
        //CommandName AndXCommand;
        //byte AndXReserved;
        //ushort AndXOffset;
        public SessionSetupAction Action;
        //ushort SecurityBlobLength;
        // Data:
        public byte[] SecurityBlob;
        public string NativeOS;     // SMB_STRING (If Unicode, this field MUST be aligned to start on a 2-byte boundary from the start of the SMB header)
        public string NativeLanMan; // SMB_STRING (this field WILL be aligned to start on a 2-byte boundary from the start of the SMB header)

        public SessionSetupAndXResponseExtended() : base()
        {
            SecurityBlob = new byte[0];
            NativeOS = String.Empty;
            NativeLanMan = String.Empty;
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            ushort securityBlobLength = (ushort)SecurityBlob.Length;

            this.SMBParameters = new byte[ParametersLength];
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 4, (ushort)Action);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 6, securityBlobLength);

            int padding = 0;
            if (isUnicode)
            {
                // wordCount is 1 byte
                padding = (1 + securityBlobLength) % 2;
                this.SMBData = new byte[SecurityBlob.Length + padding + NativeOS.Length * 2 + NativeLanMan.Length * 2 + 4];
            }
            else
            {
                this.SMBData = new byte[SecurityBlob.Length + NativeOS.Length + NativeLanMan.Length + 2];
            }
            int offset = 0;
            ByteWriter.WriteBytes(this.SMBData, ref offset, SecurityBlob);
            offset += padding;
            SMBHelper.WriteSMBString(this.SMBData, ref offset, isUnicode, NativeOS);
            SMBHelper.WriteSMBString(this.SMBData, ref offset, isUnicode, NativeLanMan);

            return base.GetBytes(isUnicode);
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
