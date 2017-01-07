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
    /// TRANS2_QUERY_FILE_INFORMATION Response
    /// </summary
    public class Transaction2QueryFileInformationResponse : Transaction2Subcommand
    {
        // Parameters:
        public ushort EaErrorOffset; // Meaningful only when request's InformationLevel is SMB_INFO_QUERY_EAS_FROM_LIST
        // Data:
        private byte[] QueryInformationBytes = new byte[0];

        public Transaction2QueryFileInformationResponse() : base()
        {

        }

        public Transaction2QueryFileInformationResponse(byte[] parameters, byte[] data, bool isUnicode) : base()
        {
            EaErrorOffset = LittleEndianConverter.ToUInt16(parameters, 0);
            QueryInformationBytes = data;
        }

        public override byte[] GetParameters(bool isUnicode)
        {
            return LittleEndianConverter.GetBytes(EaErrorOffset);
        }

        public override byte[] GetData(bool isUnicode)
        {
            return QueryInformationBytes;
        }

        public QueryInformation GetQueryInformation(QueryInformationLevel queryInformationLevel)
        {
            return QueryInformation.GetQueryInformation(QueryInformationBytes, queryInformationLevel);
        }

        public void SetQueryInformation(QueryInformation queryInformation)
        {
            QueryInformationBytes = queryInformation.GetBytes();
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
