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
        // Data:
        public QueryFSInformation QueryFSInfo;

        public Transaction2QueryFSInformationResponse() : base()
        {

        }

        public Transaction2QueryFSInformationResponse(byte[] parameters, byte[] data, QueryFSInformationLevel informationLevel, bool isUnicode) : base()
        {
            QueryFSInfo = QueryFSInformation.GetQueryFSInformation(data, informationLevel, isUnicode);
        }

        public override byte[] GetData(bool isUnicode)
        {
            return QueryFSInfo.GetBytes(isUnicode);
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
