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
    internal class CancelHelper
    {
        internal static SMB2Command GetCancelResponse(CancelRequest request, SMB2ConnectionState state)
        {
            if (request.Header.IsAsync && request.Header.AsyncID == 0)
            {
                ErrorResponse response = new ErrorResponse(request.CommandName, NTStatus.STATUS_CANCELLED);
                response.Header.IsAsync = true;
                return response;
            }

            // [MS-SMB2] If a request is not found [..] no response is sent.
            // [MS-SMB2] If the target request is not successfully canceled [..] no response is sent.
            return null;
        }
    }
}
