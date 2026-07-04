/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.DFS;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// ISMBFileStore wrapper for a DFS root share.
    /// When a CreateFile is not covered by the DFS root (STATUS_PATH_NOT_COVERED), a DFS referral is
    /// requested and the operation is retried against a referral target, connecting to another server
    /// when necessary (reusing the authentication client via IAuthenticationClient.ResetSecurityContext).
    /// </summary>
    internal class SMB2DfsFileStore : ISMBFileStore
    {
        // Guards against referral loops when chaining interlinks.
        private const int MaxReferralHopCount = 8;

        private SMB2Client m_client;
        private string m_serverName;
        private string m_shareName;
        private ISMBFileStore m_dfsFileStore;

        // Connections and tree connections established while following referrals to other targets.
        private Dictionary<string, SMB2Client> m_targetClients;
        private Dictionary<string, ISMBFileStore> m_targetFileStores;

        internal SMB2DfsFileStore(SMB2Client client, string serverName, string shareName, ISMBFileStore dfsFileStore)
        {
            m_client = client;
            m_serverName = serverName;
            m_shareName = shareName;
            m_dfsFileStore = dfsFileStore;
            m_targetClients = new Dictionary<string, SMB2Client>(StringComparer.OrdinalIgnoreCase);
            m_targetFileStores = new Dictionary<string, ISMBFileStore>(StringComparer.OrdinalIgnoreCase);
        }

        public NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, FileAttributes fileAttributes, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext)
        {
            handle = null;
            fileStatus = FileStatus.FILE_DOES_NOT_EXIST;

            ISMBFileStore fileStore = m_dfsFileStore;
            string currentServer = m_serverName;
            string currentShare = m_shareName;
            string effectivePath = path;

            for (int hopCount = 0; ; hopCount++)
            {
                object innerHandle;
                NTStatus status = fileStore.CreateFile(out innerHandle, out fileStatus, effectivePath, desiredAccess, fileAttributes, shareAccess, createDisposition, createOptions, securityContext);
                if (status != NTStatus.STATUS_PATH_NOT_COVERED)
                {
                    if (status == NTStatus.STATUS_SUCCESS)
                    {
                        handle = new DfsHandle(fileStore, innerHandle);
                    }
                    return status;
                }

                if (hopCount >= MaxReferralHopCount)
                {
                    return status;
                }

                string dfsPath = BuildUncPath(currentServer, currentShare, effectivePath);
                List<DfsPath> targets;
                if (!TryGetReferralTargets(fileStore, dfsPath, out targets))
                {
                    return status;
                }

                // MS-DFSC 3.1.5.4.3: targets are listed in order of preference, try the next target when a target is unreachable
                ISMBFileStore targetFileStore = null;
                DfsPath target = null;
                foreach (DfsPath referralTarget in targets)
                {
                    targetFileStore = GetOrConnectFileStore(referralTarget.ServerName, referralTarget.ShareName);
                    if (targetFileStore != null)
                    {
                        target = referralTarget;
                        break;
                    }
                }

                if (targetFileStore == null)
                {
                    return status;
                }

                fileStore = targetFileStore;
                currentServer = target.ServerName;
                currentShare = target.ShareName;
                effectivePath = target.PathWithinShare;
            }
        }

        private static bool TryGetReferralTargets(ISMBFileStore fileStore, string dfsPath, out List<DfsPath> targets)
        {
            targets = new List<DfsPath>();
            ResponseGetDfsReferral referralResponse;
            try
            {
                NTStatus status = DfsReferralHelper.GetDfsReferral(fileStore, dfsPath, out referralResponse);
                if (status != NTStatus.STATUS_SUCCESS || referralResponse == null)
                {
                    return false;
                }
            }
            catch
            {
                // e.g. a malformed referral response buffer
                return false;
            }

            DfsPath requestedPath = new DfsPath(dfsPath);
            foreach (DfsReferralEntry referralEntry in referralResponse.ReferralEntries)
            {
                // A STATUS_PATH_NOT_COVERED referral is always a V1-V4 link/root referral; only V3/V4 are handled.
                DfsReferralEntryV3 entry = referralEntry as DfsReferralEntryV3;
                if (entry == null || String.IsNullOrEmpty(entry.DfsPath) || String.IsNullOrEmpty(entry.NetworkAddress))
                {
                    continue;
                }

                DfsPath target = requestedPath.ReplacePrefix(new DfsPath(entry.DfsPath), new DfsPath(entry.NetworkAddress));
                // ReplacePrefix returns the path unchanged when the referral does not cover it
                if (ReferenceEquals(target, requestedPath) ||
                    String.IsNullOrEmpty(target.ShareName) ||
                    String.Equals(target.ToUncPath(), requestedPath.ToUncPath(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                targets.Add(target);
            }

            return targets.Count > 0;
        }

        private ISMBFileStore GetOrConnectFileStore(string serverName, string shareName)
        {
            string key = BuildUncPath(serverName, shareName, null);
            ISMBFileStore fileStore;
            if (m_targetFileStores.TryGetValue(key, out fileStore))
            {
                return fileStore;
            }

            fileStore = ConnectToTarget(serverName, shareName);
            if (fileStore == null)
            {
                return null;
            }

            if (fileStore is SMB2DfsFileStore dfsFileStore)
            {
                // Use the underlying file store, referrals from the target are followed (and hop-limited) by this instance
                fileStore = dfsFileStore.m_dfsFileStore;
            }

            m_targetFileStores.Add(key, fileStore);
            return fileStore;
        }

        /// <summary>
        /// Tree connects to a referral target share, reusing an existing connection to the target server when possible.
        /// </summary>
        protected virtual ISMBFileStore ConnectToTarget(string serverName, string shareName)
        {
            SMB2Client client = GetOrConnectClient(serverName);
            if (client == null)
            {
                return null;
            }

            NTStatus status;
            ISMBFileStore fileStore = client.TreeConnect(shareName, out status);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                return null;
            }
            return fileStore;
        }

        private SMB2Client GetOrConnectClient(string serverName)
        {
            if (String.Equals(serverName, m_serverName, StringComparison.OrdinalIgnoreCase))
            {
                return m_client;
            }

            SMB2Client client;
            if (m_targetClients.TryGetValue(serverName, out client))
            {
                return client;
            }

            client = m_client.ConnectAndLoginToDfsTarget(serverName);
            if (client == null)
            {
                return null;
            }

            m_targetClients.Add(serverName, client);
            return client;
        }

        private static string BuildUncPath(string serverName, string shareName, string pathWithinShare)
        {
            string uncPath = @"\\" + serverName + @"\" + shareName;
            if (!String.IsNullOrEmpty(pathWithinShare))
            {
                uncPath += @"\" + pathWithinShare;
            }
            return uncPath;
        }

        public NTStatus CloseFile(object handle)
        {
            DfsHandle dfsHandle = GetDfsHandle(handle);
            return dfsHandle.FileStore.CloseFile(dfsHandle.Handle);
        }

        public NTStatus ReadFile(out byte[] data, object handle, long offset, int maxCount)
        {
            DfsHandle dfsHandle = GetDfsHandle(handle);
            return dfsHandle.FileStore.ReadFile(out data, dfsHandle.Handle, offset, maxCount);
        }

        public NTStatus WriteFile(out int numberOfBytesWritten, object handle, long offset, byte[] data)
        {
            DfsHandle dfsHandle = GetDfsHandle(handle);
            return dfsHandle.FileStore.WriteFile(out numberOfBytesWritten, dfsHandle.Handle, offset, data);
        }

        public NTStatus FlushFileBuffers(object handle)
        {
            DfsHandle dfsHandle = GetDfsHandle(handle);
            return dfsHandle.FileStore.FlushFileBuffers(dfsHandle.Handle);
        }

        public NTStatus LockFile(object handle, long byteOffset, long length, bool exclusiveLock)
        {
            DfsHandle dfsHandle = GetDfsHandle(handle);
            return dfsHandle.FileStore.LockFile(dfsHandle.Handle, byteOffset, length, exclusiveLock);
        }

        public NTStatus UnlockFile(object handle, long byteOffset, long length)
        {
            DfsHandle dfsHandle = GetDfsHandle(handle);
            return dfsHandle.FileStore.UnlockFile(dfsHandle.Handle, byteOffset, length);
        }

        public NTStatus QueryDirectory(out List<QueryDirectoryFileInformation> result, object handle, string fileName, FileInformationClass informationClass)
        {
            DfsHandle dfsHandle = GetDfsHandle(handle);
            return dfsHandle.FileStore.QueryDirectory(out result, dfsHandle.Handle, fileName, informationClass);
        }

        public NTStatus GetFileInformation(out FileInformation result, object handle, FileInformationClass informationClass)
        {
            DfsHandle dfsHandle = GetDfsHandle(handle);
            return dfsHandle.FileStore.GetFileInformation(out result, dfsHandle.Handle, informationClass);
        }

        public NTStatus SetFileInformation(object handle, FileInformation information)
        {
            DfsHandle dfsHandle = GetDfsHandle(handle);
            return dfsHandle.FileStore.SetFileInformation(dfsHandle.Handle, information);
        }

        public NTStatus GetFileSystemInformation(out FileSystemInformation result, FileSystemInformationClass informationClass)
        {
            return m_dfsFileStore.GetFileSystemInformation(out result, informationClass);
        }

        public NTStatus SetFileSystemInformation(FileSystemInformation information)
        {
            return m_dfsFileStore.SetFileSystemInformation(information);
        }

        public NTStatus GetSecurityInformation(out SecurityDescriptor result, object handle, SecurityInformation securityInformation)
        {
            DfsHandle dfsHandle = GetDfsHandle(handle);
            return dfsHandle.FileStore.GetSecurityInformation(out result, dfsHandle.Handle, securityInformation);
        }

        public NTStatus SetSecurityInformation(object handle, SecurityInformation securityInformation, SecurityDescriptor securityDescriptor)
        {
            DfsHandle dfsHandle = GetDfsHandle(handle);
            return dfsHandle.FileStore.SetSecurityInformation(dfsHandle.Handle, securityInformation, securityDescriptor);
        }

        public NTStatus NotifyChange(out object ioRequest, object handle, NotifyChangeFilter completionFilter, bool watchTree, int outputBufferSize, OnNotifyChangeCompleted onNotifyChangeCompleted, object context)
        {
            DfsHandle dfsHandle = GetDfsHandle(handle);
            object innerIoRequest;
            NTStatus status = dfsHandle.FileStore.NotifyChange(out innerIoRequest, dfsHandle.Handle, completionFilter, watchTree, outputBufferSize, onNotifyChangeCompleted, context);
            ioRequest = (innerIoRequest != null) ? new DfsHandle(dfsHandle.FileStore, innerIoRequest) : null;
            return status;
        }

        public NTStatus Cancel(object ioRequest)
        {
            DfsHandle dfsHandle = GetDfsHandle(ioRequest);
            return dfsHandle.FileStore.Cancel(dfsHandle.Handle);
        }

        public NTStatus DeviceIOControl(object handle, uint ctlCode, byte[] input, out byte[] output, int maxOutputLength)
        {
            DfsHandle dfsHandle = GetDfsHandle(handle);
            return dfsHandle.FileStore.DeviceIOControl(dfsHandle.Handle, ctlCode, input, out output, maxOutputLength);
        }

        public NTStatus Disconnect()
        {
            foreach (ISMBFileStore fileStore in m_targetFileStores.Values)
            {
                try
                {
                    fileStore.Disconnect();
                }
                catch (InvalidOperationException)
                {
                    // The connection to the target has already been lost
                }
            }
            m_targetFileStores.Clear();

            foreach (SMB2Client client in m_targetClients.Values)
            {
                try
                {
                    client.Logoff();
                }
                catch (InvalidOperationException)
                {
                }
                client.Disconnect();
            }
            m_targetClients.Clear();

            return m_dfsFileStore.Disconnect();
        }

        private DfsHandle GetDfsHandle(object handle)
        {
            DfsHandle dfsHandle = handle as DfsHandle;
            if (dfsHandle == null)
            {
                // A handle that did not originate from this instance (e.g. the FileID used for DFS referral requests) is directed to the DFS root file store
                return new DfsHandle(m_dfsFileStore, handle);
            }
            return dfsHandle;
        }

        public uint MaxReadSize
        {
            get
            {
                uint maxReadSize = m_dfsFileStore.MaxReadSize;
                foreach (ISMBFileStore fileStore in m_targetFileStores.Values)
                {
                    maxReadSize = Math.Min(maxReadSize, fileStore.MaxReadSize);
                }
                return maxReadSize;
            }
        }

        public uint MaxWriteSize
        {
            get
            {
                uint maxWriteSize = m_dfsFileStore.MaxWriteSize;
                foreach (ISMBFileStore fileStore in m_targetFileStores.Values)
                {
                    maxWriteSize = Math.Min(maxWriteSize, fileStore.MaxWriteSize);
                }
                return maxWriteSize;
            }
        }

        /// <summary>
        /// Associates a handle (or NotifyChange ioRequest) with the file store that produced it, so that
        /// subsequent operations are routed to the referral target the handle was opened against.
        /// </summary>
        private class DfsHandle
        {
            public readonly ISMBFileStore FileStore;
            public readonly object Handle;

            public DfsHandle(ISMBFileStore fileStore, object handle)
            {
                FileStore = fileStore;
                Handle = handle;
            }
        }
    }
}
