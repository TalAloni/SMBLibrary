/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.Authentication;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Server.SMB2
{
    internal class SetInfoHelper
    {
        internal static SMB2Command GetSetInfoResponse(SetInfoRequest request, ISMBShare share, SMB2ConnectionState state)
        {
            SMB2Session session = state.GetSession(request.Header.SessionID);
            if (request.InfoType == InfoType.File)
            {
                OpenFileObject openFile = session.GetOpenFileObject(request.FileId);
                if (openFile == null)
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_FILE_CLOSED);
                }

                if (share is FileSystemShare)
                {
                    if (!((FileSystemShare)share).HasWriteAccess(session.SecurityContext, openFile.Path))
                    {
                        state.LogToServer(Severity.Verbose, "SetFileInformation on '{0}{1}' failed. User '{2}' was denied access.", share.Name, openFile.Path, session.UserName);
                        return new ErrorResponse(request.CommandName, NTStatus.STATUS_ACCESS_DENIED);
                    }
                }

                FileInformation information;
                try
                {
                    information = FileInformation.GetFileInformation(request.Buffer, 0, request.FileInformationClass);
                }
                catch (UnsupportedInformationLevelException)
                {
                    state.LogToServer(Severity.Verbose, "SetFileInformation on '{0}{1}' failed. Information class: {2}, NTStatus: STATUS_INVALID_INFO_CLASS", share.Name, openFile.Path, request.FileInformationClass);
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_INVALID_INFO_CLASS);
                }
                catch (NotImplementedException)
                {
                    state.LogToServer(Severity.Verbose, "SetFileInformation on '{0}{1}' failed. Information class: {2}, NTStatus: STATUS_NOT_SUPPORTED", share.Name, openFile.Path, request.FileInformationClass);
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_NOT_SUPPORTED);
                }
                catch (Exception)
                {
                    state.LogToServer(Severity.Verbose, "SetFileInformation on '{0}{1}' failed. Information class: {2}, NTStatus: STATUS_INVALID_PARAMETER", share.Name, openFile.Path, request.FileInformationClass);
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_INVALID_PARAMETER);
                }

                NTStatus status = share.FileStore.SetFileInformation(openFile.Handle, information);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    state.LogToServer(Severity.Verbose, "SetFileInformation on '{0}{1}' failed. Information class: {2}, NTStatus: {3}", share.Name, openFile.Path, request.FileInformationClass, status);
                    return new ErrorResponse(request.CommandName, status);
                }
                state.LogToServer(Severity.Information, "SetFileInformation on '{0}{1}' succeeded. Information class: {2}", share.Name, openFile.Path, request.FileInformationClass);
                return new SetInfoResponse();
            }
            return new ErrorResponse(request.CommandName, NTStatus.STATUS_NOT_SUPPORTED);
        }
    }
}
