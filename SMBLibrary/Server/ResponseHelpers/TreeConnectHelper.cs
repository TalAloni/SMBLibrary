/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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

namespace SMBLibrary.Server
{
    public class TreeConnectHelper
    {
        internal static SMBCommand GetTreeConnectResponse(SMBHeader header, TreeConnectAndXRequest request, StateObject state, ShareCollection shares)
        {
            bool isExtended = (request.Flags & TreeConnectFlags.ExtendedResponse) > 0;
            string relativePath = ServerPathUtils.GetRelativeServerPath(request.Path);
            if (String.Equals(relativePath, "\\IPC$", StringComparison.InvariantCultureIgnoreCase))
            {
                header.TID = state.AddConnectedTree(relativePath);
                if (isExtended)
                {
                    return CreateTreeConnectResponseExtended(ServiceName.NamedPipe);
                }
                else
                {
                    return CreateTreeConnectResponse(ServiceName.NamedPipe);
                }
            }
            else
            {
                FileSystemShare share = shares.GetShareFromRelativePath(relativePath);
                if (share == null)
                {
                    header.Status = NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
                    return new ErrorResponse(CommandName.SMB_COM_TREE_CONNECT_ANDX);
                }
                else
                {
                    string userName = state.GetConnectedUserName(header.UID);
                    if (!share.HasReadAccess(userName))
                    {
                        header.Status = NTStatus.STATUS_ACCESS_DENIED;
                        return new ErrorResponse(CommandName.SMB_COM_TREE_CONNECT_ANDX);
                    }
                    else
                    {
                        header.TID = state.AddConnectedTree(relativePath);
                        if (isExtended)
                        {
                            return CreateTreeConnectResponseExtended(ServiceName.DiskShare);
                        }
                        else
                        {
                            return CreateTreeConnectResponse(ServiceName.DiskShare);
                        }
                    }
                }
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

        internal static SMBCommand GetTreeDisconnectResponse(SMBHeader header, TreeDisconnectRequest request, StateObject state)
        {
            if (!state.IsTreeConnected(header.TID))
            {
                header.Status = NTStatus.STATUS_SMB_BAD_TID;
                return new ErrorResponse(CommandName.SMB_COM_TREE_DISCONNECT);
            }

            state.RemoveConnectedTree(header.TID);
            return new TreeDisconnectResponse();
        }
    }
}
