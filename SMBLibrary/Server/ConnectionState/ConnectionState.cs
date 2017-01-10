/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using SMBLibrary.NetBios;
using Utilities;

namespace SMBLibrary.Server
{
    public class ConnectionState
    {
        public Socket ClientSocket;
        public NBTConnectionReceiveBuffer ReceiveBuffer;

        public ConnectionState()
        {
            ReceiveBuffer = new NBTConnectionReceiveBuffer();
        }

        public ConnectionState(ConnectionState state)
        {
            ClientSocket = state.ClientSocket;
            ReceiveBuffer = state.ReceiveBuffer;
        }
    }
}
