/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using Utilities;

namespace SMBLibrary.Server
{
    internal class ConnectionManager
    {
        private List<ConnectionState> m_activeConnections = new List<ConnectionState>();

        public void AddConnection(ConnectionState connection)
        {
            lock (m_activeConnections)
            {
                m_activeConnections.Add(connection);
            }
        }

        public bool RemoveConnection(ConnectionState connection)
        {
            lock (m_activeConnections)
            {
                int connectionIndex = m_activeConnections.IndexOf(connection);
                if (connectionIndex >= 0)
                {
                    m_activeConnections.RemoveAt(connectionIndex);
                    return true;
                }
                return false;
            }
        }

        public void ReleaseConnection(ConnectionState connection)
        {
            connection.SendQueue.Stop();
            SocketUtils.ReleaseSocket(connection.ClientSocket);
            RemoveConnection(connection);
        }
    }
}
