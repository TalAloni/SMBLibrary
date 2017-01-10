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
    /// TRANS_QUERY_NMPIPE_INFO Response
    /// </summary>
    public class TransactionQueryNamedPipeInfoResponse : TransactionSubcommand
    {
        // Parameters:
        public ushort OutputBufferSize;
        public ushort InputBufferSize;
        public byte MaximumInstances;
        public byte CurrentInstances;
        public byte PipeNameLength;
        public string PipeName; // SMB_STRING (this field WILL be aligned to start on a 2-byte boundary from the start of the SMB header)

        public TransactionQueryNamedPipeInfoResponse() : base()
        {}

        public TransactionQueryNamedPipeInfoResponse(byte[] parameters, bool isUnicode) : base()
        {
            OutputBufferSize = LittleEndianConverter.ToUInt16(parameters, 0);
            InputBufferSize = LittleEndianConverter.ToUInt16(parameters, 2);
            MaximumInstances = ByteReader.ReadByte(parameters, 4);
            CurrentInstances = ByteReader.ReadByte(parameters, 5);
            PipeNameLength = ByteReader.ReadByte(parameters, 6);
            // Note: Trans_Parameters is aligned to 4 byte boundary
            PipeName = SMB1Helper.ReadSMBString(parameters, 8, isUnicode);
        }

        public override byte[] GetParameters(bool isUnicode)
        {
            int length = 8;
            if (isUnicode)
            {
                length += PipeName.Length * 2 + 2;
            }
            else
            {
                length += PipeName.Length + 1;
            }
            byte[] parameters = new byte[length];
            LittleEndianWriter.WriteUInt16(parameters, 0, OutputBufferSize);
            LittleEndianWriter.WriteUInt16(parameters, 2, InputBufferSize);
            ByteWriter.WriteByte(parameters, 4, MaximumInstances);
            ByteWriter.WriteByte(parameters, 5, CurrentInstances);
            ByteWriter.WriteByte(parameters, 6, PipeNameLength);
            SMB1Helper.WriteSMBString(parameters, 8, isUnicode, PipeName);
            return parameters; ;
        }

        public override TransactionSubcommandName SubcommandName
        {
            get
            {
                return TransactionSubcommandName.TRANS_QUERY_NMPIPE_INFO;
            }
        }
    }
}
