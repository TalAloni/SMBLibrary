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
        private Dictionary<ushort, string> m_connectedUsers = new Dictionary<ushort, string>();
        private ushort m_nextUID = 1;

        // Key is TID
        private Dictionary<ushort, string> m_connectedTrees = new Dictionary<ushort, string>();
        private ushort m_nextTID = 1;

        // Key is FID
        private Dictionary<ushort, OpenedFileObject> m_openedFiles = new Dictionary<ushort, OpenedFileObject>();
        private ushort m_nextFID = 1;
        // Key is FID
        private Dictionary<ushort, byte[]> m_namedPipeResponse = new Dictionary<ushort, byte[]>();

        // Key is PID
        public Dictionary<uint, ProcessStateObject> ProcessStateList = new Dictionary<uint, ProcessStateObject>();
        public const int MaxSearches = 2048; // Windows servers initialize Server.MaxSearches to 2048.
        public Dictionary<ushort, List<FileSystemEntry>> OpenSearches = new Dictionary<ushort, List<FileSystemEntry>>();
        private ushort m_nextSearchHandle = 1;

        public SMB1ConnectionState(ConnectionState state) : base(state)
        {
        }

        /// <summary>
        /// An open UID MUST be unique within an SMB connection.
        /// The value of 0xFFFE SHOULD NOT be used as a valid UID. All other possible values for a UID, excluding zero (0x0000), are valid.
        /// </summary>
        private ushort? AllocateUserID()
        {
            for (ushort offset = 0; offset < UInt16.MaxValue; offset++)
            {
                ushort userID = (ushort)(m_nextUID + offset);
                if (userID == 0 || userID == 0xFFFE || userID == 0xFFFF)
                {
                    continue;
                }
                if (!m_connectedUsers.ContainsKey(userID))
                {
                    m_nextUID = (ushort)(userID + 1);
                    return userID;
                }
            }
            return null;
        }

        public ushort? AddConnectedUser(string userName)
        {
            ushort? userID = AllocateUserID();
            if (userID.HasValue)
            {
                m_connectedUsers.Add(userID.Value, userName);
            }
            return userID;
        }

        public string GetConnectedUserName(ushort userID)
        {
            if (m_connectedUsers.ContainsKey(userID))
            {
                return m_connectedUsers[userID];
            }
            else
            {
                return null;
            }
        }

        public bool IsAuthenticated(ushort userID)
        {
            return m_connectedUsers.ContainsKey(userID);
        }

        public void RemoveConnectedUser(ushort userID)
        {
            m_connectedUsers.Remove(userID);
        }

        /// <summary>
        /// An open TID MUST be unique within an SMB connection.
        /// The value 0xFFFF MUST NOT be used as a valid TID. All other possible values for TID, including zero (0x0000), are valid.
        /// </summary>
        private ushort? AllocateTreeID()
        {
            for (ushort offset = 0; offset < UInt16.MaxValue; offset++)
            {
                ushort treeID = (ushort)(m_nextTID + offset);
                if (treeID == 0 || treeID == 0xFFFF)
                {
                    continue;
                }
                if (!m_connectedTrees.ContainsKey(treeID))
                {
                    m_nextTID = (ushort)(treeID + 1);
                    return treeID;
                }
            }
            return null;
        }

        public ushort? AddConnectedTree(string relativePath)
        {
            ushort? treeID = AllocateTreeID();
            if (treeID.HasValue)
            {
                m_connectedTrees.Add(treeID.Value, relativePath);
            }
            return treeID;
        }

        public string GetConnectedTreePath(ushort treeID)
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

        public void RemoveConnectedTree(ushort treeID)
        {
            m_connectedTrees.Remove(treeID);
        }

        public bool IsTreeConnected(ushort treeID)
        {
            return m_connectedTrees.ContainsKey(treeID);
        }

        public bool IsIPC(ushort treeID)
        {
            string relativePath = GetConnectedTreePath(treeID);
            return String.Equals(relativePath, "\\IPC$", StringComparison.InvariantCultureIgnoreCase);
        }

        public ProcessStateObject GetProcessState(uint processID)
        {
            if (ProcessStateList.ContainsKey(processID))
            {
                return ProcessStateList[processID];
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
            if (ProcessStateList.ContainsKey(processID))
            {
                return ProcessStateList[processID];
            }
            else
            {
                ProcessStateObject processState = new ProcessStateObject();
                ProcessStateList[processID] = processState;
                return processState;
            }
        }

        /// <summary>
        /// The value 0xFFFF MUST NOT be used as a valid FID. All other possible values for FID, including zero (0x0000) are valid.
        /// </summary>
        /// <returns></returns>
        private ushort? AllocateFileID()
        {
            for (ushort offset = 0; offset < UInt16.MaxValue; offset++)
            {
                ushort fileID = (ushort)(m_nextFID + offset);
                if (fileID == 0 || fileID == 0xFFFF)
                {
                    continue;
                }
                if (!m_openedFiles.ContainsKey(fileID))
                {
                    m_nextFID = (ushort)(fileID + 1);
                    return fileID;
                }
            }
            return null;
        }

        /// <param name="relativePath">Should include the path relative to the file system</param>
        /// <returns>FileID</returns>
        public ushort? AddOpenedFile(string relativePath)
        {
            return AddOpenedFile(relativePath, null);
        }

        public ushort? AddOpenedFile(string relativePath, Stream stream)
        {
            return AddOpenedFile(relativePath, stream, false);
        }

        public ushort? AddOpenedFile(string relativePath, Stream stream, bool deleteOnClose)
        {
            ushort? fileID = AllocateFileID();
            if (fileID.HasValue)
            {
                m_openedFiles.Add(fileID.Value, new OpenedFileObject(relativePath, stream, deleteOnClose));
            }
            return fileID;
        }

        public string GetOpenedFilePath(ushort fileID)
        {
            if (m_openedFiles.ContainsKey(fileID))
            {
                return m_openedFiles[fileID].Path;
            }
            else
            {
                return null;
            }
        }

        public OpenedFileObject GetOpenedFileObject(ushort fileID)
        {
            if (m_openedFiles.ContainsKey(fileID))
            {
                return m_openedFiles[fileID];
            }
            else
            {
                return null;
            }
        }

        public bool IsFileOpen(ushort fileID)
        {
            return m_openedFiles.ContainsKey(fileID);
        }

        public void RemoveOpenedFile(ushort fileID)
        {
            Stream stream = m_openedFiles[fileID].Stream;
            if (stream != null)
            {
                LogToServer(Severity.Verbose, "Closing file '{0}'", m_openedFiles[fileID].Path);
                stream.Close();
            }
            m_openedFiles.Remove(fileID);
        }

        public void StoreNamedPipeReply(ushort fileID, byte[] response)
        {
            m_namedPipeResponse.Add(fileID, response);
        }

        public byte[] RetrieveNamedPipeReply(ushort fileID)
        {
            if (m_namedPipeResponse.ContainsKey(fileID))
            {
                byte[] result = m_namedPipeResponse[fileID];
                m_namedPipeResponse.Remove(fileID);
                return result;
            }
            else
            {
                return new byte[0];
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

        public ushort? AllocateSearchHandle()
        {
            for (ushort offset = 0; offset < UInt16.MaxValue; offset++)
            {
                ushort searchHandle = (ushort)(m_nextSearchHandle + offset);
                if (searchHandle == 0 || searchHandle == 0xFFFF)
                {
                    continue;
                }
                if (!OpenSearches.ContainsKey(searchHandle))
                {
                    m_nextSearchHandle = (ushort)(searchHandle + 1);
                    return searchHandle;
                }
            }
            return null;
        }

        public void ReleaseSearchHandle(ushort searchHandle)
        {
            if (OpenSearches.ContainsKey(searchHandle))
            {
                OpenSearches.Remove(searchHandle);
            }
        }
    }
}
