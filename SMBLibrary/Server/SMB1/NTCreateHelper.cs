/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SMBLibrary.Services;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    public class NTCreateHelper
    {
        internal static SMB1Command GetNTCreateResponse(SMB1Header header, NTCreateAndXRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            bool isExtended = (request.Flags & NTCreateFlags.NT_CREATE_REQUEST_EXTENDED_RESPONSE) > 0;
            string path = request.FileName;
            FileAccess createAccess = NTFileStoreHelper.ToCreateFileAccess(request.DesiredAccess, request.CreateDisposition);
            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasAccess(session.SecurityContext, path, createAccess))
                {
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            object handle;
            FileStatus fileStatus;
            NTStatus createStatus = share.FileStore.CreateFile(out handle, out fileStatus, path, request.DesiredAccess, request.ShareAccess, request.CreateDisposition, request.CreateOptions, session.SecurityContext);
            if (createStatus != NTStatus.STATUS_SUCCESS)
            {
                header.Status = createStatus;
                return new ErrorResponse(request.CommandName);
            }

            ushort? fileID = session.AddOpenFile(header.TID, path, handle);
            if (!fileID.HasValue)
            {
                share.FileStore.CloseFile(handle);
                header.Status = NTStatus.STATUS_TOO_MANY_OPENED_FILES;
                return new ErrorResponse(request.CommandName);
            }

            if (share is NamedPipeShare)
            {
                if (isExtended)
                {
                    return CreateResponseExtendedForNamedPipe(fileID.Value, FileStatus.FILE_OPENED);
                }
                else
                {
                    return CreateResponseForNamedPipe(fileID.Value, FileStatus.FILE_OPENED);
                }
            }
            else // FileSystemShare
            {
                FileNetworkOpenInformation fileInfo = NTFileStoreHelper.GetNetworkOpenInformation(share.FileStore, handle);
                if (isExtended)
                {
                    NTCreateAndXResponseExtended response = CreateResponseExtendedFromFileInformation(fileInfo, fileID.Value, fileStatus);
                    if ((request.Flags & NTCreateFlags.NT_CREATE_REQUEST_OPBATCH) > 0)
                    {
                        response.OpLockLevel = OpLockLevel.BatchOpLockGranted;
                    }
                    return response;
                }
                else
                {
                    NTCreateAndXResponse response = CreateResponseFromFileInformation(fileInfo, fileID.Value, fileStatus);
                    if ((request.Flags & NTCreateFlags.NT_CREATE_REQUEST_OPBATCH) > 0)
                    {
                        response.OpLockLevel = OpLockLevel.BatchOpLockGranted;
                    }
                    return response;
                }
            }
        }

        private static NTCreateAndXResponse CreateResponseForNamedPipe(ushort fileID, FileStatus fileStatus)
        {
            NTCreateAndXResponse response = new NTCreateAndXResponse();
            response.FID = fileID;
            response.CreateDisposition = ToCreateDisposition(fileStatus);
            response.ExtFileAttributes = ExtendedFileAttributes.Normal;
            response.ResourceType = ResourceType.FileTypeMessageModePipe;
            response.NMPipeStatus.ICount = 255;
            response.NMPipeStatus.ReadMode = ReadMode.MessageMode;
            response.NMPipeStatus.NamedPipeType = NamedPipeType.MessageNodePipe;
            return response;
        }

        private static NTCreateAndXResponseExtended CreateResponseExtendedForNamedPipe(ushort fileID, FileStatus fileStatus)
        {
            NTCreateAndXResponseExtended response = new NTCreateAndXResponseExtended();
            response.FID = fileID;
            response.CreateDisposition = ToCreateDisposition(fileStatus);
            response.ExtFileAttributes = ExtendedFileAttributes.Normal;
            response.ResourceType = ResourceType.FileTypeMessageModePipe;
            NamedPipeStatus status = new NamedPipeStatus();
            status.ICount = 255;
            status.ReadMode = ReadMode.MessageMode;
            status.NamedPipeType = NamedPipeType.MessageNodePipe;
            response.NMPipeStatus = status;
            response.MaximalAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA | FileAccessMask.FILE_APPEND_DATA |
                                                FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                FileAccessMask.FILE_EXECUTE |
                                                FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                FileAccessMask.DELETE | FileAccessMask.READ_CONTROL | FileAccessMask.WRITE_DAC | FileAccessMask.WRITE_OWNER | FileAccessMask.SYNCHRONIZE;
            response.GuestMaximalAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA |
                                                    FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                    FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                    FileAccessMask.READ_CONTROL | FileAccessMask.SYNCHRONIZE;
            return response;
        }

        private static NTCreateAndXResponse CreateResponseFromFileInformation(FileNetworkOpenInformation fileInfo, ushort fileID, FileStatus fileStatus)
        {
            NTCreateAndXResponse response = new NTCreateAndXResponse();
            response.FID = fileID;
            response.CreateDisposition = ToCreateDisposition(fileStatus);
            response.CreateTime = fileInfo.CreationTime;
            response.LastAccessTime = fileInfo.LastAccessTime;
            response.LastWriteTime = fileInfo.LastWriteTime;
            response.LastChangeTime = fileInfo.LastWriteTime;
            response.AllocationSize = fileInfo.AllocationSize;
            response.EndOfFile = fileInfo.EndOfFile;
            response.ExtFileAttributes = (ExtendedFileAttributes)fileInfo.FileAttributes;
            response.ResourceType = ResourceType.FileTypeDisk;
            response.Directory = fileInfo.IsDirectory;
            return response;
        }

        private static NTCreateAndXResponseExtended CreateResponseExtendedFromFileInformation(FileNetworkOpenInformation fileInfo, ushort fileID, FileStatus fileStatus)
        {
            NTCreateAndXResponseExtended response = new NTCreateAndXResponseExtended();
            response.FID = fileID;
            response.CreateDisposition = ToCreateDisposition(fileStatus);
            response.CreateTime = fileInfo.CreationTime;
            response.LastAccessTime = fileInfo.LastAccessTime;
            response.LastWriteTime = fileInfo.LastWriteTime;
            response.LastChangeTime = fileInfo.LastWriteTime;
            response.ExtFileAttributes = (ExtendedFileAttributes)fileInfo.FileAttributes;
            response.AllocationSize = fileInfo.AllocationSize;
            response.EndOfFile = fileInfo.EndOfFile;
            response.ResourceType = ResourceType.FileTypeDisk;
            response.FileStatusFlags = FileStatusFlags.NO_EAS | FileStatusFlags.NO_SUBSTREAMS | FileStatusFlags.NO_REPARSETAG;
            response.Directory = fileInfo.IsDirectory;
            response.MaximalAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA | FileAccessMask.FILE_APPEND_DATA |
                                                FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                FileAccessMask.FILE_EXECUTE |
                                                FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                FileAccessMask.DELETE | FileAccessMask.READ_CONTROL | FileAccessMask.WRITE_DAC | FileAccessMask.WRITE_OWNER | FileAccessMask.SYNCHRONIZE;
            response.GuestMaximalAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA |
                                                    FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                    FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                    FileAccessMask.READ_CONTROL | FileAccessMask.SYNCHRONIZE;
            return response;
        }

        private static CreateDisposition ToCreateDisposition(FileStatus fileStatus)
        {
            if (fileStatus == FileStatus.FILE_SUPERSEDED)
            {
                return CreateDisposition.FILE_SUPERSEDE;
            }
            else if (fileStatus == FileStatus.FILE_CREATED)
            {
                return CreateDisposition.FILE_CREATE;
            }
            else if (fileStatus == FileStatus.FILE_OVERWRITTEN)
            {
                return CreateDisposition.FILE_OVERWRITE;
            }
            else
            {
                return CreateDisposition.FILE_OPEN;
            }
        }
    }
}
