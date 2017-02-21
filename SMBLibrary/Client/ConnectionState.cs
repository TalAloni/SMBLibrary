/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using SMBLibrary.NetBios;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Client
{
    public class ConnectionState
    {
        public NBTConnectionReceiveBuffer ReceiveBuffer = new NBTConnectionReceiveBuffer();
    }
}
