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
    /// TRANS2_FIND_NEXT2 Response
    /// </summary>
    public class Transaction2FindNext2Response : Transaction2Subcommand
    {
        public const int ParametersLength = 8;
        // Parameters:
        public ushort SearchCount;
        public bool EndOfSearch;
        public ushort EaErrorOffset;
        public ushort LastNameOffset;
        // Data:
        public FindInformationList FindInfoList;

        public Transaction2FindNext2Response() : base()
        {
            FindInfoList = new FindInformationList();
        }

        public Transaction2FindNext2Response(byte[] parameters, byte[] data, FindInformationLevel informationLevel, bool isUnicode, bool returnResumeKeys) : base()
        {
            SearchCount = LittleEndianConverter.ToUInt16(parameters, 0);
            EndOfSearch = LittleEndianConverter.ToUInt16(parameters, 2) != 0;
            EaErrorOffset = LittleEndianConverter.ToUInt16(parameters, 4);
            LastNameOffset = LittleEndianConverter.ToUInt16(parameters, 6);

            FindInfoList = new FindInformationList(data, informationLevel, isUnicode, returnResumeKeys);
        }

        public override byte[] GetParameters(bool isUnicode)
        {
            byte[] parameters = new byte[ParametersLength];
            LittleEndianWriter.WriteUInt16(parameters, 0, SearchCount);
            LittleEndianWriter.WriteUInt16(parameters, 2, Convert.ToUInt16(EndOfSearch));
            LittleEndianWriter.WriteUInt16(parameters, 4, EaErrorOffset);
            LittleEndianWriter.WriteUInt16(parameters, 6, LastNameOffset);
            return parameters;
        }

        public override byte[] GetData(bool isUnicode)
        {
            return FindInfoList.GetBytes(isUnicode);
        }

        public override Transaction2SubcommandName SubcommandName
        {
            get
            {
                return Transaction2SubcommandName.TRANS2_FIND_NEXT2;
            }
        }
    }
}
