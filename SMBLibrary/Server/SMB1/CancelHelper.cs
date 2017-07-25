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
    internal class CancelHelper
    {
        internal static void ProcessNTCancelRequest(SMB1Header header, NTCancelRequest request, ISMBShare share, SMB1ConnectionState state)
        {
        }
    }
}
