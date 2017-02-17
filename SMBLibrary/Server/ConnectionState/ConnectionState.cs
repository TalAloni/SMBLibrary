/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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
using Utilities;

namespace SMBLibrary.Server
{
    public delegate void LogDelegate(Severity severity, string message);

    public enum SMBDialect
    {
        NotSet,
        NTLM012, // NT LM 0.12
        SMB202,  // SMB 2.0.2
        SMB210,  // SMB 2.1
    }

    public class ConnectionState
    {
        public Socket ClientSocket;
        public IPEndPoint ClientEndPoint;
        public NBTConnectionReceiveBuffer ReceiveBuffer;
        protected LogDelegate LogToServerHandler;
        public SMBDialect ServerDialect;
        public object AuthenticationContext;

        public ConnectionState(LogDelegate logToServerHandler)
        {
            ReceiveBuffer = new NBTConnectionReceiveBuffer();
            LogToServerHandler = logToServerHandler;
            ServerDialect = SMBDialect.NotSet;
        }

        public ConnectionState(ConnectionState state)
        {
            ClientSocket = state.ClientSocket;
            ClientEndPoint = state.ClientEndPoint;
            ReceiveBuffer = state.ReceiveBuffer;
            LogToServerHandler = state.LogToServerHandler;
            ServerDialect = state.ServerDialect;
        }

        public void LogToServer(Severity severity, string message)
        {
            message = String.Format("[{0}] {1}", ConnectionIdentifier, message);
            if (LogToServerHandler != null)
            {
                LogToServerHandler(severity, message);
            }
        }

        public void LogToServer(Severity severity, string message, params object[] args)
        {
            LogToServer(severity, String.Format(message, args));
        }

        public string ConnectionIdentifier
        {
            get
            {
                if (ClientEndPoint != null)
                {
                    return ClientEndPoint.Address + ":" + ClientEndPoint.Port;
                }
                return String.Empty;
            }
        }
    }
}
