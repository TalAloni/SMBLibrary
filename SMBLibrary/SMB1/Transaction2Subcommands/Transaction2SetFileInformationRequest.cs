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
    /// TRANS2_SET_FILE_INFORMATION Request
    /// </summary>
    public class Transaction2SetFileInformationRequest : Transaction2Subcommand
    {
        public const int ParametersLength = 6;
        // Parameters:
        public ushort FID;
        public SetInformationLevel InformationLevel;
        public ushort Reserved;
        // Data:
        public byte[] InformationBytes;

        public Transaction2SetFileInformationRequest() : base()
        {
        }

        public Transaction2SetFileInformationRequest(byte[] parameters, byte[] data, bool isUnicode) : base()
        {
            FID = LittleEndianConverter.ToUInt16(parameters, 0);
            InformationLevel = (SetInformationLevel)LittleEndianConverter.ToUInt16(parameters, 2);
            Reserved = LittleEndianConverter.ToUInt16(parameters, 4);

            InformationBytes = data;
        }

        public override byte[] GetSetup()
        {
            return LittleEndianConverter.GetBytes((ushort)SubcommandName);
        }

        public override byte[] GetParameters(bool isUnicode)
        {
            byte[] parameters = new byte[ParametersLength];
            LittleEndianWriter.WriteUInt16(parameters, 0, FID);
            LittleEndianWriter.WriteUInt16(parameters, 2, (ushort)InformationLevel);
            LittleEndianWriter.WriteUInt16(parameters, 4, Reserved);
            return parameters;
        }

        public override byte[] GetData(bool isUnicode)
        {
            return InformationBytes;
        }

        public void SetInformation(SetInformation information)
        {
            InformationBytes = information.GetBytes();
        }

        public override Transaction2SubcommandName SubcommandName
        {
            get
            {
                return Transaction2SubcommandName.TRANS2_SET_FILE_INFORMATION;
            }
        }
    }
}
