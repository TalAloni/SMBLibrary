/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Server.SMB2
{
    public class TreeConnectHelper
    {
        internal static SMB2Command GetTreeConnectResponse(TreeConnectRequest request, SMB2ConnectionState state, NamedPipeShare services, ShareCollection shares)
        {
            SMB2Session session = state.GetSession(request.Header.SessionID);
            TreeConnectResponse response = new TreeConnectResponse();
            string shareName = ServerPathUtils.GetShareName(request.Path);
            ISMBShare share;
            ShareType shareType;
            ShareFlags shareFlags = ShareFlags.ManualCaching;
            if (String.Equals(shareName, NamedPipeShare.NamedPipeShareName, StringComparison.InvariantCultureIgnoreCase))
            {
                share = services;
                shareType = ShareType.Pipe;
                shareFlags = ShareFlags.NoCaching;
            }
            else
            {
                share = shares.GetShareFromName(shareName);
                shareType = ShareType.Disk;
                if (share == null)
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_OBJECT_PATH_NOT_FOUND);
                }

                if (!((FileSystemShare)share).HasReadAccess(session.UserName))
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_ACCESS_DENIED);
                }
            }

            uint? treeID = session.AddConnectedTree(share);
            if (!treeID.HasValue)
            {
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_INSUFF_SERVER_RESOURCES);
            }
            response.Header.TreeID = treeID.Value;
            response.ShareType = shareType;
            response.ShareFlags = shareFlags;
            response.MaximalAccess.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA | FileAccessMask.FILE_APPEND_DATA |
                                          FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                          FileAccessMask.FILE_EXECUTE |
                                          FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                          FileAccessMask.DELETE | FileAccessMask.READ_CONTROL | FileAccessMask.WRITE_DAC | FileAccessMask.WRITE_OWNER | FileAccessMask.SYNCHRONIZE;
            return response;
        }
    }
}
