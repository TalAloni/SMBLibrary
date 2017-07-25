/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Server.SMB2
{
    internal class ChangeNotifyHelper
    {
        internal static SMB2Command GetChangeNotifyInterimResponse(ChangeNotifyRequest request, ISMBShare share, SMB2ConnectionState state)
        {
            // [MS-SMB2] If the underlying object store does not support change notifications, the server MUST fail this request with STATUS_NOT_SUPPORTED
            ErrorResponse response = new ErrorResponse(request.CommandName, NTStatus.STATUS_NOT_SUPPORTED);
            // Windows 7 / 8 / 10 will infinitely retry sending ChangeNotify requests if the response does not have SMB2_FLAGS_ASYNC_COMMAND set.
            // Note: NoRemoteChangeNotify can be set in the registry to prevent the client from sending ChangeNotify requests altogether.
            response.Header.IsAsync = true;
            return response;
        }
    }
}
