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
    internal class SMB2Session
    {
        private SMB2ConnectionState m_connection;
        private ulong m_sessionID;
        private byte[] m_sessionKey;
        private SecurityContext m_securityContext;

        // Key is TreeID
        private Dictionary<uint, ISMBShare> m_connectedTrees = new Dictionary<uint, ISMBShare>();
        private uint m_nextTreeID = 1; // TreeID uniquely identifies a tree connect within the scope of the session

        // Key is the persistent portion of the FileID
        private Dictionary<ulong, OpenFileObject> m_openFiles = new Dictionary<ulong, OpenFileObject>();

        // Key is the persistent portion of the FileID
        private Dictionary<ulong, OpenSearch> m_openSearches = new Dictionary<ulong, OpenSearch>();

        public SMB2Session(SMB2ConnectionState connection, ulong sessionID, string userName, string machineName, byte[] sessionKey, object accessToken)
        {
            m_connection = connection;
            m_sessionID = sessionID;
            m_sessionKey = sessionKey;
            m_securityContext = new SecurityContext(userName, machineName, connection.ClientEndPoint, connection.AuthenticationContext, accessToken);
        }

        private uint? AllocateTreeID()
        {
            for (uint offset = 0; offset < UInt32.MaxValue; offset++)
            {
                uint treeID = (uint)(m_nextTreeID + offset);
                if (treeID == 0 || treeID == 0xFFFFFFFF)
                {
                    continue;
                }
                if (!m_connectedTrees.ContainsKey(treeID))
                {
                    m_nextTreeID = (uint)(treeID + 1);
                    return treeID;
                }
            }
            return null;
        }

        public uint? AddConnectedTree(ISMBShare share)
        {
            uint? treeID = AllocateTreeID();
            if (treeID.HasValue)
            {
                m_connectedTrees.Add(treeID.Value, share);
            }
            return treeID;
        }

        public ISMBShare GetConnectedTree(uint treeID)
        {
            if (m_connectedTrees.ContainsKey(treeID))
            {
                return m_connectedTrees[treeID];
            }
            else
            {
                return null;
            }
        }

        public void RemoveConnectedTree(uint treeID)
        {
            m_connectedTrees.Remove(treeID);
        }

        public void RemoveConnectedTrees()
        {
            m_connectedTrees.Clear();
        }

        public bool IsTreeConnected(uint treeID)
        {
            return m_connectedTrees.ContainsKey(treeID);
        }

        /// <returns>The persistent portion of the FileID</returns>
        public ulong? AddOpenFile(string relativePath, object handle)
        {
            ulong? persistentID = m_connection.AllocatePersistentFileID();
            if (persistentID.HasValue)
            {
                m_openFiles.Add(persistentID.Value, new OpenFileObject(relativePath, handle));
            }
            return persistentID;
        }

        public OpenFileObject GetOpenFileObject(ulong fileID)
        {
            if (m_openFiles.ContainsKey(fileID))
            {
                return m_openFiles[fileID];
            }
            else
            {
                return null;
            }
        }

        public void RemoveOpenFile(ulong fileID)
        {
            m_openFiles.Remove(fileID);
            m_openSearches.Remove(fileID);
        }

        public OpenSearch AddOpenSearch(ulong fileID, List<QueryDirectoryFileInformation> entries, int enumerationLocation)
        {
            OpenSearch openSearch = new OpenSearch(entries, enumerationLocation);
            m_openSearches.Add(fileID, openSearch);
            return openSearch;
        }

        public OpenSearch GetOpenSearch(ulong fileID)
        {
            OpenSearch openSearch;
            m_openSearches.TryGetValue(fileID, out openSearch);
            return openSearch;
        }

        public void RemoveOpenSearch(ulong fileID)
        {
            m_openSearches.Remove(fileID);
        }

        public byte[] SessionKey
        {
            get
            {
                return m_sessionKey;
            }
        }

        public SecurityContext SecurityContext
        {
            get
            {
                return m_securityContext;
            }
        }

        public string UserName
        {
            get
            {
                return m_securityContext.UserName;
            }
        }
    }
}
