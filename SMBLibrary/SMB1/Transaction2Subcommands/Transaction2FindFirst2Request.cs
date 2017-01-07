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
    /// TRANS2_FIND_FIRST2 Request
    /// </summary>
    public class Transaction2FindFirst2Request : Transaction2Subcommand
    {
        // Parameters:
        public FileAttributes SearchAttributes;
        public ushort SearchCount;
        public FindFlags Flags;
        public FindInformationLevel InformationLevel;
        public SearchStorageType SearchStorageType;
        public string FileName; // SMB_STRING
        // Data:
        public FullExtendedAttributeList GetExtendedAttributeList; // Used with FindInformationLevel.SMB_INFO_QUERY_EAS_FROM_LIST

        public Transaction2FindFirst2Request() : base()
        {
            GetExtendedAttributeList = new FullExtendedAttributeList();
        }

        public Transaction2FindFirst2Request(byte[] parameters, byte[] data, bool isUnicode) : base()
        {
            SearchAttributes = (FileAttributes)LittleEndianConverter.ToUInt16(parameters, 0);
            SearchCount = LittleEndianConverter.ToUInt16(parameters, 2);
            Flags = (FindFlags)LittleEndianConverter.ToUInt16(parameters, 4);
            InformationLevel = (FindInformationLevel)LittleEndianConverter.ToUInt16(parameters, 6);
            SearchStorageType = (SearchStorageType)LittleEndianConverter.ToUInt32(parameters, 8);
            FileName = SMBHelper.ReadSMBString(parameters, 12, isUnicode);

            if (InformationLevel == FindInformationLevel.SMB_INFO_QUERY_EAS_FROM_LIST)
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
            int length = 12;
            if (isUnicode)
            {
                length += FileName.Length * 2 + 2;
            }
            else
            {
                length += FileName.Length + 1;
            }

            byte[] parameters = new byte[length];
            LittleEndianWriter.WriteUInt16(parameters, 0, (ushort)SearchAttributes);
            LittleEndianWriter.WriteUInt16(parameters, 2, SearchCount);
            LittleEndianWriter.WriteUInt16(parameters, 4, (ushort)Flags);
            LittleEndianWriter.WriteUInt16(parameters, 6, (ushort)InformationLevel);
            LittleEndianWriter.WriteUInt32(parameters, 8, (uint)SearchStorageType);
            SMBHelper.WriteSMBString(parameters, 12, isUnicode, FileName);

            return parameters;
        }

        public override byte[] GetData(bool isUnicode)
        {
            if (InformationLevel == FindInformationLevel.SMB_INFO_QUERY_EAS_FROM_LIST)
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
                return Transaction2SubcommandName.TRANS2_FIND_FIRST2;
            }
        }
    }
}
