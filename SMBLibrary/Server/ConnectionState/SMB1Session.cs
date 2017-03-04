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
    internal class SMB1Session
    {
        private const int MaxSearches = 2048; // Windows servers initialize Server.MaxSearches to 2048.

        private SMB1ConnectionState m_connection;
        private ushort m_userID;
        private SecurityContext m_securityContext;

        // Key is TID
        private Dictionary<ushort, ISMBShare> m_connectedTrees = new Dictionary<ushort, ISMBShare>();

        // Key is FID
        private Dictionary<ushort, OpenFileObject> m_openFiles = new Dictionary<ushort, OpenFileObject>();

        // Key is search handle a.k.a. Search ID
        private Dictionary<ushort, OpenSearch> m_openSearches = new Dictionary<ushort, OpenSearch>();
        private ushort m_nextSearchHandle = 1;

        public SMB1Session(SMB1ConnectionState connection, ushort userID, string userName, string machineName, object accessToken)
        {
            m_connection = connection;
            m_userID = userID;
            m_securityContext = new SecurityContext(userName, machineName, connection.ClientEndPoint, connection.AuthenticationContext, accessToken);
        }

        public ushort? AddConnectedTree(ISMBShare share)
        {
            ushort? treeID = m_connection.AllocateTreeID();
            if (treeID.HasValue)
            {
                m_connectedTrees.Add(treeID.Value, share);
            }
            return treeID;
        }

        public ISMBShare GetConnectedTree(ushort treeID)
        {
            ISMBShare share;
            m_connectedTrees.TryGetValue(treeID, out share);
            return share;
        }

        public void RemoveConnectedTree(ushort treeID)
        {
            m_connectedTrees.Remove(treeID);
        }

        public bool IsTreeConnected(ushort treeID)
        {
            return m_connectedTrees.ContainsKey(treeID);
        }

        /// <param name="relativePath">Should include the path relative to the share</param>
        /// <returns>FileID</returns>
        public ushort? AddOpenFile(string relativePath)
        {
            return AddOpenFile(relativePath, null);
        }

        public ushort? AddOpenFile(string relativePath, object handle)
        {
            ushort? fileID = m_connection.AllocateFileID();
            if (fileID.HasValue)
            {
                m_openFiles.Add(fileID.Value, new OpenFileObject(relativePath, handle));
            }
            return fileID;
        }

        public OpenFileObject GetOpenFileObject(ushort fileID)
        {
            OpenFileObject openFile;
            m_openFiles.TryGetValue(fileID, out openFile);
            return openFile;
        }

        public void RemoveOpenFile(ushort fileID)
        {
            m_openFiles.Remove(fileID);
        }

        private ushort? AllocateSearchHandle()
        {
            for (ushort offset = 0; offset < UInt16.MaxValue; offset++)
            {
                ushort searchHandle = (ushort)(m_nextSearchHandle + offset);
                if (searchHandle == 0 || searchHandle == 0xFFFF)
                {
                    continue;
                }
                if (!m_openSearches.ContainsKey(searchHandle))
                {
                    m_nextSearchHandle = (ushort)(searchHandle + 1);
                    return searchHandle;
                }
            }
            return null;
        }

        public ushort? AddOpenSearch(List<QueryDirectoryFileInformation> entries, int enumerationLocation)
        {
            ushort? searchHandle = AllocateSearchHandle();
            if (searchHandle.HasValue)
            {
                OpenSearch openSearch = new OpenSearch(entries, enumerationLocation);
                m_openSearches.Add(searchHandle.Value, openSearch);
            }
            return searchHandle;
        }

        public OpenSearch GetOpenSearch(ushort searchHandle)
        {
            OpenSearch openSearch;
            m_openSearches.TryGetValue(searchHandle, out openSearch);
            return openSearch;
        }

        public void RemoveOpenSearch(ushort searchHandle)
        {
            m_openSearches.Remove(searchHandle);
        }

        public ushort UserID
        {
            get
            {
                return m_userID;
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
