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
    /// TRANS2_QUERY_PATH_INFORMATION Response
    /// </summary>
    public class Transaction2QueryPathInformationResponse : Transaction2Subcommand
    {
        // Parameters:
        public ushort EaErrorOffset;
        // Data:
        public QueryInformation QueryInfo;

        public Transaction2QueryPathInformationResponse() : base()
        {

        }

        public Transaction2QueryPathInformationResponse(byte[] parameters, byte[] data, QueryInformationLevel informationLevel, bool isUnicode) : base()
        {
            if ((ushort)informationLevel > 0x0100)
            {
                EaErrorOffset = LittleEndianConverter.ToUInt16(parameters, 0);
            }
            QueryInfo = QueryInformation.GetQueryInformation(data, informationLevel);
        }

        public override byte[] GetParameters(bool isUnicode)
        {
            if ((ushort)QueryInfo.InformationLevel > 0x0100)
            {
                byte[] parameters = new byte[2];
                LittleEndianWriter.WriteUInt16(parameters, 0, EaErrorOffset);
                return parameters;
            }
            return new byte[0];
        }

        public override byte[] GetData(bool isUnicode)
        {
            if (QueryInfo == null)
            {
                // SMB_INFO_IS_NAME_VALID
                return new byte[0];
            }
            else
            {
                return QueryInfo.GetBytes();
            }
        }

        public override Transaction2SubcommandName SubcommandName
        {
            get
            {
                return Transaction2SubcommandName.TRANS2_QUERY_PATH_INFORMATION;
            }
        }
    }
}
