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
    /// SMB_COM_TRANSACTION Request
    /// </summary>
    public class TransactionRequest : SMB1Command
    {
        public const int FixedSMBParametersLength = 28;
        // Parameters:
        public ushort TotalParameterCount;
        public ushort TotalDataCount;
        public ushort MaxParameterCount;
        public ushort MaxDataCount;
        public byte MaxSetupCount;
        public byte Reserved1;
        public TransactionFlags Flags;
        public uint Timeout;
        public ushort Reserved2;
        //ushort ParameterCount;
        //ushort ParameterOffset;
        //ushort DataCount;
        //ushort DataOffset;
        //byte SetupCount; // In 2-byte words
        public byte Reserved3;
        public byte[] Setup;
        // Data:
        public string Name; // SMB_STRING (If Unicode, this field MUST be aligned to start on a 2-byte boundary from the start of the SMB header)
        // Padding (alignment to 4 byte boundary)
        public byte[] TransParameters; // Trans_Parameters
        // Padding (alignment to 4 byte boundary)
        public byte[] TransData; // Trans_Data

        public TransactionRequest() : base()
        {
            Name = String.Empty;
        }

        public TransactionRequest(byte[] buffer, int offset, bool isUnicode) : base(buffer, offset, isUnicode)
        {
            TotalParameterCount = LittleEndianConverter.ToUInt16(this.SMBParameters, 0);
            TotalDataCount = LittleEndianConverter.ToUInt16(this.SMBParameters, 2);
            MaxParameterCount = LittleEndianConverter.ToUInt16(this.SMBParameters, 4);
            MaxDataCount = LittleEndianConverter.ToUInt16(this.SMBParameters, 6);
            MaxSetupCount = ByteReader.ReadByte(this.SMBParameters, 8);
            Reserved1 = ByteReader.ReadByte(this.SMBParameters, 9);
            Flags = (TransactionFlags)LittleEndianConverter.ToUInt16(this.SMBParameters, 10);
            Timeout = LittleEndianConverter.ToUInt32(this.SMBParameters, 12);
            Reserved2 = LittleEndianConverter.ToUInt16(this.SMBParameters, 16);
            ushort ParameterCount = LittleEndianConverter.ToUInt16(this.SMBParameters, 18);
            ushort ParameterOffset = LittleEndianConverter.ToUInt16(this.SMBParameters, 20);
            ushort DataCount = LittleEndianConverter.ToUInt16(this.SMBParameters, 22);
            ushort DataOffset = LittleEndianConverter.ToUInt16(this.SMBParameters, 24);
            byte SetupCount = ByteReader.ReadByte(this.SMBParameters, 26);
            Reserved3 = ByteReader.ReadByte(this.SMBParameters, 27);
            Setup = ByteReader.ReadBytes(this.SMBParameters, 28, SetupCount * 2);

            if (this.SMBData.Length > 0) // Workaround, Some SAMBA clients will set ByteCount to 0 (Popcorn Hour A-400)
            {
                int dataOffset = 0;
                if (this is Transaction2Request)
                {
                    Name = String.Empty;
                    dataOffset += 1;
                }
                else
                {
                    if (isUnicode)
                    {
                        int namePadding = 1;
                        dataOffset += namePadding;
                    }
                    Name = SMB1Helper.ReadSMBString(this.SMBData, ref dataOffset, isUnicode);
                }
            }
            TransParameters = ByteReader.ReadBytes(buffer, ParameterOffset, ParameterCount);
            TransData = ByteReader.ReadBytes(buffer, DataOffset, DataCount);
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            byte SetupCount = (byte)(Setup.Length / 2);
            ushort ParameterCount = (ushort)TransParameters.Length;
            ushort DataCount = (ushort)TransData.Length;

            // WordCount + ByteCount are additional 3 bytes
            ushort ParameterOffset = (ushort)(SMB1Header.Length + 3 + (FixedSMBParametersLength + Setup.Length));
            if (this is Transaction2Request)
            {
                ParameterOffset += 1;
            }
            else
            {
                if (isUnicode)
                {
                    ParameterOffset += (ushort)(Name.Length * 2 + 2);
                }
                else
                {
                    ParameterOffset += (ushort)(Name.Length + 1);
                }
            }
            int padding1 = (4 - (ParameterOffset % 4)) % 4;
            ParameterOffset += (ushort)padding1;
            ushort DataOffset = (ushort)(ParameterOffset + ParameterCount);
            int padding2 = (4 - (DataOffset % 4)) % 4;
            DataOffset += (ushort)padding2;

            this.SMBParameters = new byte[FixedSMBParametersLength + Setup.Length];
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 0, TotalParameterCount);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 2, TotalDataCount);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 4, MaxParameterCount);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 6, MaxDataCount);
            ByteWriter.WriteByte(this.SMBParameters, 8, MaxSetupCount);
            ByteWriter.WriteByte(this.SMBParameters, 9, Reserved1);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 10, (ushort)Flags);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, 12, Timeout);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 16, Reserved2);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 18, ParameterCount);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 20, ParameterOffset);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 22, DataCount);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 24, DataOffset);
            ByteWriter.WriteByte(this.SMBParameters, 26, SetupCount);
            ByteWriter.WriteByte(this.SMBParameters, 27, Reserved3);
            ByteWriter.WriteBytes(this.SMBParameters, 28, Setup);

            int offset;
            if (this is Transaction2Request)
            {
                offset = 0;
                this.SMBData = new byte[1 + ParameterCount + DataCount + padding1 + padding2];
            }
            else
            {
                if (isUnicode)
                {
                    int namePadding = 1;
                    offset = namePadding;
                    this.SMBData = new byte[namePadding + Name.Length * 2 + 2 + ParameterCount + DataCount + padding1 + padding2];
                }
                else
                {
                    offset = 0;
                    this.SMBData = new byte[Name.Length + 1 + ParameterCount + DataCount + padding1 + padding2];
                }
            }
            SMB1Helper.WriteSMBString(this.SMBData, ref offset, isUnicode, Name);
            ByteWriter.WriteBytes(this.SMBData, offset + padding1, TransParameters);
            ByteWriter.WriteBytes(this.SMBData, offset + padding1 + ParameterCount + padding2, TransData);

            return base.GetBytes(isUnicode);
        }

        public override CommandName CommandName
        {
            get
            {
                return CommandName.SMB_COM_TRANSACTION;
            }
        }
    }
}
