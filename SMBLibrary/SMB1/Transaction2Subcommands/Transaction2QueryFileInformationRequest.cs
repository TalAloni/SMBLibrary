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
    /// TRANS2_QUERY_FILE_INFORMATION Request
    /// </summary>
    public class Transaction2QueryFileInformationRequest : Transaction2Subcommand
    {
        public int ParametersLength = 4;
        // Parameters:
        public ushort FID;
        public QueryInformationLevel InformationLevel;
        // Data:
        FullExtendedAttributeList GetExtendedAttributeList; // Used with QueryInformationLevel.SMB_INFO_QUERY_EAS_FROM_LIST

        public Transaction2QueryFileInformationRequest() : base()
        {
            GetExtendedAttributeList = new FullExtendedAttributeList();
        }

        public Transaction2QueryFileInformationRequest(byte[] parameters, byte[] data, bool isUnicode) : base()
        {
            FID = LittleEndianConverter.ToUInt16(parameters, 0);
            InformationLevel = (QueryInformationLevel)LittleEndianConverter.ToUInt16(parameters, 2);

            if (InformationLevel == QueryInformationLevel.SMB_INFO_QUERY_EAS_FROM_LIST)
            {
                GetExtendedAttributeList = new FullExtendedAttributeList(data, 0);
            }
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
            return parameters;
        }

        public override byte[] GetData(bool isUnicode)
        {
            if (InformationLevel == QueryInformationLevel.SMB_INFO_QUERY_EAS_FROM_LIST)
            {
                return GetExtendedAttributeList.GetBytes();
            }
            else
            {
                return new byte[0];
            }
        }

        public override Transaction2SubcommandName SubcommandName
        {
            get
            {
                return Transaction2SubcommandName.TRANS2_QUERY_FILE_INFORMATION;
            }
        }
    }
}
