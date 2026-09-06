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
    /// [MS-DFSC] Resolves a share to the namespace server that hosts it, before a tree connect is
    /// attempted. A domain-based namespace is addressed as 'domain\namespace', where the first
    /// component names the domain rather than a server, so no host serves a share by that name and
    /// only a root referral can name one that does.
    /// </summary>
    internal class DfsNamespaceResolver
    {
        /// <summary>
        /// Requests a DFS referral for the share and tree connects to the namespace server it
        /// names, connecting to that server first when it is not the one already connected to.
        /// Returns null when the path is not a DFS namespace, or when no target could be reached,
        /// leaving the caller to tree connect to the current server exactly as it would have done
        /// had this never been attempted.
        /// </summary>
        internal static ISMBFileStore TryResolveNamespace(SMB2Client client, string serverName, string shareName, out NTStatus status)
        {
            status = NTStatus.STATUS_BAD_NETWORK_NAME;

            // Resolving IPC$ would mean requesting a referral over a tree connect to the very
            // share being resolved. Windows does not request a referral for it either.
            if (String.Equals(shareName, "IPC$", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            List<DfsPath> targets = GetNamespaceServers(client, serverName, shareName);
            foreach (DfsPath target in targets)
            {
                NTStatus targetStatus;
                if (IsSameServer(serverName, target.ServerName))
                {
                    // A namespace root hosted by the server already connected to - a standalone
                    // namespace, or a domain-based one this server happens to host. Reuse the
                    // connection rather than open a second one to the same host. The root referral
                    // identifies the namespace even if TREE_CONNECT omits the optional DFS_ROOT flag.
                    ISMBFileStore localStore = client.TreeConnect(target.ShareName, false, out targetStatus);
                    if (targetStatus == NTStatus.STATUS_SUCCESS && localStore != null)
                    {
                        status = targetStatus;
                        return localStore as SMB2DfsFileStore ?? new SMB2DfsFileStore(client, serverName, target.ShareName, localStore);
                    }

                    // MS-DFSC 3.1.5.4.3: targets are listed in order of preference, so a target
                    // that cannot be reached just means trying the next one.
                    continue;
                }

                SMB2Client namespaceClient = client.ConnectAndLoginToDfsTarget(target.ServerName);
                if (namespaceClient == null)
                {
                    continue;
                }

                // Do not resolve again from here: a namespace server answering
                // STATUS_BAD_NETWORK_NAME is a dead end, not another referral to follow.
                ISMBFileStore fileStore = namespaceClient.TreeConnect(target.ShareName, false, out targetStatus);
                if (targetStatus == NTStatus.STATUS_SUCCESS && fileStore != null)
                {
                    // [MS-SMB2] 3.3.5.7: DFS_ROOT is optional. A successful connection to a target
                    // from a root referral is sufficient to wrap the store as a namespace root.
                    SMB2DfsFileStore dfsFileStore = fileStore as SMB2DfsFileStore ??
                        new SMB2DfsFileStore(namespaceClient, target.ServerName, target.ShareName, fileStore);
                    // The caller never created this connection, so the store it is handed owns it
                    // and releases it on Disconnect - the same rule that already applies to the
                    // connections opened when following a link referral.
                    dfsFileStore.TakeOwnershipOf(namespaceClient);
                    status = targetStatus;
                    return dfsFileStore;
                }

                namespaceClient.LogoffAndDisconnect();
            }

            return null;
        }

        /// <summary>
        /// Selects the namespace servers named by a root referral, in the order the server listed
        /// them - MS-DFSC 3.1.5.4.3 states they are ordered by preference.
        /// Only V3 and V4 entries carry NetworkAddress, so V1 and V2 entries cannot name a target.
        /// </summary>
        internal static List<DfsPath> GetNamespaceTargets(ResponseGetDfsReferral referralResponse)
        {
            List<DfsPath> targets = new List<DfsPath>();
            if (referralResponse == null)
            {
                return targets;
            }

            foreach (DfsReferralEntry referralEntry in referralResponse.ReferralEntries)
            {
                DfsReferralEntryV3 entry = referralEntry as DfsReferralEntryV3;
                if (entry == null || String.IsNullOrEmpty(entry.NetworkAddress))
                {
                    continue;
                }

                // MS-DFSC 2.2.4.3: a root referral names root targets. An entry reporting anything
                // else is a link or storage target, not a namespace server, and following it would
                // send the tree connect to a server it was never asked to reach.
                if (entry.ServerType != DfsServerType.Root)
                {
                    continue;
                }

                DfsPath target;
                try
                {
                    target = new DfsPath(entry.NetworkAddress);
                }
                catch (ArgumentException)
                {
                    // A network address that contains no path components. Skip the entry rather
                    // than discard the targets named by the entries around it.
                    continue;
                }

                if (!String.IsNullOrEmpty(target.ShareName))
                {
                    targets.Add(target);
                }
            }

            return targets;
        }

        private static List<DfsPath> GetNamespaceServers(SMB2Client client, string serverName, string shareName)
        {
            NTStatus ipcStatus;
            ISMBFileStore ipcStore = client.TreeConnect("IPC$", false, out ipcStatus);
            if (ipcStatus != NTStatus.STATUS_SUCCESS || ipcStore == null)
            {
                return new List<DfsPath>();
            }

            try
            {
                ResponseGetDfsReferral referralResponse;
                string namespacePath = @"\\" + serverName + @"\" + shareName;
                NTStatus status = DfsReferralHelper.GetDfsReferral(ipcStore, namespacePath, out referralResponse);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    return new List<DfsPath>();
                }

                return GetNamespaceTargets(referralResponse);
            }
            catch
            {
                // e.g. a malformed referral response buffer
                return new List<DfsPath>();
            }
            finally
            {
                try
                {
                    ipcStore.Disconnect();
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        /// <summary>
        /// A referral names its target by fully qualified name, while the caller may have connected
        /// by short name, or the other way round.
        /// Deliberately conservative: reporting two distinct hosts as one would skip a target the
        /// referral named, while failing to recognise the same host only costs a second connection
        /// to it.
        /// </summary>
        internal static bool IsSameServer(string serverName, string targetServerName)
        {
            if (String.IsNullOrEmpty(serverName) || String.IsNullOrEmpty(targetServerName))
            {
                return false;
            }

            if (String.Equals(serverName, targetServerName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return IsShortNameOf(serverName, targetServerName) || IsShortNameOf(targetServerName, serverName);
        }

        /// <summary>
        /// True when shortName is a single label and fullName is that same label with a DNS suffix.
        /// A dotted quad is never a single label, so two addresses on one network - 10.0.0.5 and
        /// 10.0.0.9 - are not taken for the same host.
        /// </summary>
        private static bool IsShortNameOf(string shortName, string fullName)
        {
            if (shortName.IndexOf('.') >= 0 || fullName.Length <= shortName.Length)
            {
                return false;
            }

            return fullName[shortName.Length] == '.' &&
                   String.Compare(fullName, 0, shortName, 0, shortName.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
