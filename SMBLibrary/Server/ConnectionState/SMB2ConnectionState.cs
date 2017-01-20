/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Server
{
    public delegate ulong? AllocatePersistentFileID();

    public class SMB2ConnectionState : ConnectionState
    {
        // Key is SessionID
        private Dictionary<ulong, SMB2Session> m_sessions = new Dictionary<ulong, SMB2Session>();
        private ulong m_nextSessionID = 1;
        public AllocatePersistentFileID AllocatePersistentFileID;

        public SMB2ConnectionState(ConnectionState state, AllocatePersistentFileID allocatePersistentFileID) : base(state)
        {
            AllocatePersistentFileID = allocatePersistentFileID;
        }

        public ulong? AllocateSessionID()
        {
            for (ulong offset = 0; offset < UInt64.MaxValue; offset++)
            {
                ulong sessionID = (ulong)(m_nextSessionID + offset);
                if (sessionID == 0 || sessionID == 0xFFFFFFFF)
                {
                    continue;
                }
                if (!m_sessions.ContainsKey(sessionID))
                {
                    m_nextSessionID = (ulong)(sessionID + 1);
                    return sessionID;
                }
            }
            return null;
        }

        public SMB2Session CreateSession(ulong sessionID, string userName)
        {
            SMB2Session session = new SMB2Session(this, sessionID, userName);
            m_sessions.Add(sessionID, session);
            return session;
        }

        public SMB2Session GetSession(ulong sessionID)
        {
            SMB2Session session;
            m_sessions.TryGetValue(sessionID, out session);
            return session;
        }

        public void RemoveSession(ulong sessionID)
        {
            m_sessions.Remove(sessionID);
        }

        public void ClearSessions()
        {
            m_sessions.Clear();
        }
    }
}
