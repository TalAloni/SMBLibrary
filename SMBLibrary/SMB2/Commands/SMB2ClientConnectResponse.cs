/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using Utilities;

namespace SMBLibrary.SMB2
{
    /// <summary>
    /// SMB2 CANCEL Request
    /// </summary>
    public class SMB2ClientConnectResponse
    {
        public bool Successful { get; set; }
        public List<SMB2Command> ConnectCommandsHistories { get; set; }
    }
}
