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
    /// TRANS_QUERY_NMPIPE_STATE Response
    /// </summary>
    public class TransactionQueryNamedPipeStateResponse : TransactionSubcommand
    {
        public NamedPipeStatus NMPipeStatus;

        public override byte[] GetSetup()
        {
            return new byte[0];
        }

        public override byte[] GetParameters(bool isUnicode)
        {
            byte[] parameters = new byte[2];
            NMPipeStatus.WriteBytes(parameters, 0);
            return parameters;
        }

        public override TransactionSubcommandName SubcommandName
        {
            get
            {
                return TransactionSubcommandName.TRANS_QUERY_NMPIPE_STATE;
            }
        }
    }
}
