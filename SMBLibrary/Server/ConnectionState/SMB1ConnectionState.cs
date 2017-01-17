/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using Utilities;

namespace SMBLibrary.Server
{
    public class SMB1ConnectionState : ConnectionState
    {
        public int MaxBufferSize;
        public bool LargeRead;
        public bool LargeWrite;

        // Key is UID
        private Dictionary<ushort, SMB1Session> m_sessions = new Dictionary<ushort, SMB1Session>();
        private ushort m_nextUID = 1; // UID MUST be unique within an SMB connection
        private ushort m_nextTID = 1; // TID MUST be unique within an SMB connection
        private ushort m_nextFID = 1; // FID MUST be unique within an SMB connection

        // Key is PID (PID MUST be unique within an SMB connection)
        private Dictionary<uint, ProcessStateObject> m_processStateList = new Dictionary<uint, ProcessStateObject>();

        public SMB1ConnectionState(ConnectionState state) : base(state)
        {
        }

        /// <summary>
        /// An open UID MUST be unique within an SMB connection.
        /// The value of 0xFFFE SHOULD NOT be used as a valid UID. All other possible values for a UID, excluding zero (0x0000), are valid.
        /// </summary>
        public ushort? AllocateUserID()
        {
            for (ushort offset = 0; offset < UInt16.MaxValue; offset++)
            {
                ushort userID = (ushort)(m_nextUID + offset);
                if (userID == 0 || userID == 0xFFFE || userID == 0xFFFF)
                {
                    continue;
                }
                if (!m_sessions.ContainsKey(userID))
                {
                    m_nextUID = (ushort)(userID + 1);
                    return userID;
                }
            }
            return null;
        }

        public SMB1Session CreateSession(ushort userID, string userName)
        {
            SMB1Session session = new SMB1Session(this, userID, userName);
            m_sessions.Add(userID, session);
            return session;
        }

        /// <returns>null if all UserID values have already been allocated</returns>
        public SMB1Session CreateSession(string userName)
        {
            ushort? userID = AllocateUserID();
            if (userID.HasValue)
            {
                return CreateSession(userID.Value, userName);
            }
            return null;
        }

        public SMB1Session GetSession(ushort userID)
        {
            SMB1Session session;
            m_sessions.TryGetValue(userID, out session);
            return session;
        }

        public void RemoveSession(ushort userID)
        {
            m_sessions.Remove(userID);
        }

        /// <summary>
        /// An open TID MUST be unique within an SMB connection.
        /// The value 0xFFFF MUST NOT be used as a valid TID. All other possible values for TID, including zero (0x0000), are valid.
        /// </summary>
        public ushort? AllocateTreeID()
        {
            for (ushort offset = 0; offset < UInt16.MaxValue; offset++)
            {
                ushort treeID = (ushort)(m_nextTID + offset);
                if (treeID == 0 || treeID == 0xFFFF)
                {
                    continue;
                }
                if (!IsTreeIDAllocated(treeID))
                {
                    m_nextTID = (ushort)(treeID + 1);
                    return treeID;
                }
            }
            return null;
        }

        private bool IsTreeIDAllocated(ushort treeID)
        {
            foreach (SMB1Session session in m_sessions.Values)
            {
                if (session.GetConnectedTree(treeID) != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// A FID returned from an Open or Create operation MUST be unique within an SMB connection.
        /// The value 0xFFFF MUST NOT be used as a valid FID. All other possible values for FID, including zero (0x0000) are valid.
        /// </summary>
        /// <returns></returns>
        public ushort? AllocateFileID()
        {
            for (ushort offset = 0; offset < UInt16.MaxValue; offset++)
            {
                ushort fileID = (ushort)(m_nextFID + offset);
                if (fileID == 0 || fileID == 0xFFFF)
                {
                    continue;
                }
                if (!IsFileIDAllocated(fileID))
                {
                    m_nextFID = (ushort)(fileID + 1);
                    return fileID;
                }
            }
            return null;
        }

        private bool IsFileIDAllocated(ushort fileID)
        {
            foreach (SMB1Session session in m_sessions.Values)
            {
                if (session.GetOpenFileObject(fileID) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public ProcessStateObject GetProcessState(uint processID)
        {
            if (m_processStateList.ContainsKey(processID))
            {
                return m_processStateList[processID];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get or Create process state
        /// </summary>
        public ProcessStateObject ObtainProcessState(uint processID)
        {
            if (m_processStateList.ContainsKey(processID))
            {
                return m_processStateList[processID];
            }
            else
            {
                ProcessStateObject processState = new ProcessStateObject();
                m_processStateList[processID] = processState;
                return processState;
            }
        }

        public uint? GetMaxDataCount(uint processID)
        {
            ProcessStateObject processState = GetProcessState(processID);
            if (processState != null)
            {
                return processState.MaxDataCount;
            }
            else
            {
                return null;
            }
        }
    }
}
