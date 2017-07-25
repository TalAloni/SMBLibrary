/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    internal class NotifyChangeHelper
    {
        internal static void ProcessNTTransactNotifyChangeRequest(SMB1Header header, uint maxParameterCount, NTTransactNotifyChangeRequest subcommand, ISMBShare share, SMB1ConnectionState state)
        {
            // [MS-CIFS] If the server does not support the NT_TRANSACT_NOTIFY_CHANGE subcommand, it can return an
            // error response with STATUS_NOT_IMPLEMENTED [..] in response to an NT_TRANSACT_NOTIFY_CHANGE Request.
            header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
        }
    }
}
