/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    public class TreeConnectHelper
    {
        internal static SMB1Command GetTreeConnectResponse(SMB1Header header, TreeConnectAndXRequest request, SMB1ConnectionState state, NamedPipeShare services, ShareCollection shares)
        {
            SMB1Session session = state.GetSession(header.UID);
            bool isExtended = (request.Flags & TreeConnectFlags.ExtendedResponse) > 0;
            string shareName = ServerPathUtils.GetShareName(request.Path);
            ISMBShare share;
            ServiceName serviceName;
            if (String.Equals(shareName, NamedPipeShare.NamedPipeShareName, StringComparison.InvariantCultureIgnoreCase))
            {
                share = services;
                serviceName = ServiceName.NamedPipe;
            }
            else
            {
                share = shares.GetShareFromName(shareName);
                serviceName = ServiceName.DiskShare;
                if (share == null)
                {
                    header.Status = NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
                    return new ErrorResponse(CommandName.SMB_COM_TREE_CONNECT_ANDX);
                }

                if (!((FileSystemShare)share).HasReadAccess(session.UserName, @"\", state.ClientEndPoint))
                {
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(CommandName.SMB_COM_TREE_CONNECT_ANDX);
                }
            }
            ushort? treeID = session.AddConnectedTree(share);
            if (!treeID.HasValue)
            {
                header.Status = NTStatus.STATUS_INSUFF_SERVER_RESOURCES;
                return new ErrorResponse(CommandName.SMB_COM_TREE_CONNECT_ANDX);
            }
            header.TID = treeID.Value;
            if (isExtended)
            {
                return CreateTreeConnectResponseExtended(serviceName);
            }
            else
            {
                return CreateTreeConnectResponse(serviceName);
            }
        }

        private static TreeConnectAndXResponse CreateTreeConnectResponse(ServiceName serviceName)
        {
            TreeConnectAndXResponse response = new TreeConnectAndXResponse();
            response.OptionalSupport = OptionalSupportFlags.SMB_SUPPORT_SEARCH_BITS;
            response.NativeFileSystem = String.Empty;
            response.Service = serviceName;
            return response;
        }

        private static TreeConnectAndXResponseExtended CreateTreeConnectResponseExtended(ServiceName serviceName)
        {
            TreeConnectAndXResponseExtended response = new TreeConnectAndXResponseExtended();
            response.OptionalSupport = OptionalSupportFlags.SMB_SUPPORT_SEARCH_BITS;
            response.MaximalShareAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA | FileAccessMask.FILE_APPEND_DATA |
                                                        FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                        FileAccessMask.FILE_EXECUTE |
                                                        FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                        FileAccessMask.DELETE | FileAccessMask.READ_CONTROL | FileAccessMask.WRITE_DAC | FileAccessMask.WRITE_OWNER | FileAccessMask.SYNCHRONIZE;
            response.GuestMaximalShareAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA |
                                                            FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                            FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                            FileAccessMask.READ_CONTROL | FileAccessMask.SYNCHRONIZE;
            response.NativeFileSystem = String.Empty;
            response.Service = serviceName;
            return response;
        }

        internal static SMB1Command GetTreeDisconnectResponse(SMB1Header header, TreeDisconnectRequest request, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            if (!session.IsTreeConnected(header.TID))
            {
                header.Status = NTStatus.STATUS_SMB_BAD_TID;
                return new ErrorResponse(CommandName.SMB_COM_TREE_DISCONNECT);
            }

            session.RemoveConnectedTree(header.TID);
            return new TreeDisconnectResponse();
        }
    }
}
