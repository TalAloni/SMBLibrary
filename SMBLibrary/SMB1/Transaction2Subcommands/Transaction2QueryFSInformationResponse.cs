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
    /// TRANS2_QUERY_FS_INFORMATION Response
    /// </summary>
    public class Transaction2QueryFSInformationResponse : Transaction2Subcommand
    {
        public const int ParametersLength = 0;
        // Data:
        private byte[] QueryFSInformationBytes;

        public Transaction2QueryFSInformationResponse() : base()
        {
        }

        public Transaction2QueryFSInformationResponse(byte[] parameters, byte[] data, bool isUnicode) : base()
        {
            QueryFSInformationBytes = data;
        }

        public override byte[] GetData(bool isUnicode)
        {
            return QueryFSInformationBytes;
        }

        public QueryFSInformation GetQueryFSInformation(QueryFSInformationLevel informationLevel, bool isUnicode)
        {
            return QueryFSInformation.GetQueryFSInformation(QueryFSInformationBytes, informationLevel, isUnicode);
        }

        public void SetQueryFSInformation(QueryFSInformation queryFSInformation, bool isUnicode)
        {
            QueryFSInformationBytes = queryFSInformation.GetBytes(isUnicode);
        }

        public override Transaction2SubcommandName SubcommandName
        {
            get
            {
                return Transaction2SubcommandName.TRANS2_QUERY_FS_INFORMATION;
            }
        }
    }
}
