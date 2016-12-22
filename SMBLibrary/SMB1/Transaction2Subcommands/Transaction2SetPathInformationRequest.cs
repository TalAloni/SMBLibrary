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
    /// TRANS2_SET_PATH_INFORMATION Request
    /// </summary>
    public class Transaction2SetPathInformationRequest : Transaction2Subcommand
    {
        public const int ParametersFixedLength = 6;
        // Parameters:
        public SetInformationLevel InformationLevel;
        public uint Reserved;
        public string FileName; // SMB_STRING
        // Data:
        public SetInformation SetInfo;

        public Transaction2SetPathInformationRequest() : base()
        {}

        public Transaction2SetPathInformationRequest(byte[] parameters, byte[] data, bool isUnicode) : base()
        {
            InformationLevel = (SetInformationLevel)LittleEndianConverter.ToUInt16(parameters, 0);
            Reserved = LittleEndianConverter.ToUInt32(parameters, 2);
            FileName = SMBHelper.ReadSMBString(parameters, 6, isUnicode);
        }

        public override byte[] GetSetup()
        {
            return LittleEndianConverter.GetBytes((ushort)SubcommandName);
        }

        public override byte[] GetParameters(bool isUnicode)
        {
            int length = ParametersFixedLength;
            if (isUnicode)
            {
                length += FileName.Length * 2 + 2;
            }
            else
            {
                length += FileName.Length + 1;
            }

            byte[] parameters = new byte[length];
            LittleEndianWriter.WriteUInt16(parameters, 0, (ushort)InformationLevel);
            LittleEndianWriter.WriteUInt32(parameters, 2, Reserved);
            SMBHelper.WriteSMBString(parameters, 6, isUnicode, FileName);
            return parameters;
        }

        public override byte[] GetData(bool isUnicode)
        {
            return SetInfo.GetBytes();
        }

        public override Transaction2SubcommandName SubcommandName
        {
            get
            {
                return Transaction2SubcommandName.TRANS2_SET_PATH_INFORMATION;
            }
        }
    }
}
