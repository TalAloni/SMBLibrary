/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Net;
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
            connection.CloseSessions();
            RemoveConnection(connection);
        }

        public void ReleaseConnection(IPEndPoint clientEndPoint)
        {
            ConnectionState connection = FindConnection(clientEndPoint);
            if (connection != null)
            {
                ReleaseConnection(connection);
            }
        }

        public void ReleaseAllConnections()
        {
            List<ConnectionState> connections = new List<ConnectionState>(m_activeConnections);
            foreach (ConnectionState connection in connections)
            {
                ReleaseConnection(connection);
            }
        }

        private ConnectionState FindConnection(IPEndPoint clientEndPoint)
        {
            lock (m_activeConnections)
            {
                for (int index = 0; index < m_activeConnections.Count; index++)
                {
                    if (m_activeConnections[index].ClientEndPoint.Equals(clientEndPoint))
                    {
                        return m_activeConnections[index];
                    }
                }
            }
            return null;
        }

        public List<SessionInformation> GetSessionsInformation()
        {
            List<SessionInformation> result = new List<SessionInformation>();
            lock (m_activeConnections)
            {
                foreach (ConnectionState connection in m_activeConnections)
                {
                    List<SessionInformation> sessions = connection.GetSessionsInformation();
                    result.AddRange(sessions);
                }
            }
            return result;
        }
    }
}
