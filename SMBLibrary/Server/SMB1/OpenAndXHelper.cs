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
    public class OpenAndXHelper
    {
        internal static SMB1Command GetOpenAndXResponse(SMB1Header header, OpenAndXRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            bool isExtended = (request.Flags & OpenFlags.SMB_OPEN_EXTENDED_RESPONSE) > 0;
            string path = request.FileName;
            if (share is NamedPipeShare)
            {
                Stream pipeStream = ((NamedPipeShare)share).OpenPipe(path);
                if (pipeStream != null)
                {
                    ushort? fileID = session.AddOpenFile(path, pipeStream);
                    if (!fileID.HasValue)
                    {
                        header.Status = NTStatus.STATUS_TOO_MANY_OPENED_FILES;
                        return new ErrorResponse(CommandName.SMB_COM_OPEN_ANDX);
                    }
                    if (isExtended)
                    {
                        return CreateResponseExtendedForNamedPipe(fileID.Value);
                    }
                    else
                    {
                        return CreateResponseForNamedPipe(fileID.Value);
                    }
                }

                header.Status = NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
                return new ErrorResponse(CommandName.SMB_COM_OPEN_ANDX);
            }
            else // FileSystemShare
            {
                FileSystemShare fileSystemShare = (FileSystemShare)share;
                FileAccess fileAccess = ToFileAccess(request.AccessMode.AccessMode);
                if (!fileSystemShare.HasAccess(session.UserName, path, fileAccess, state.ClientEndPoint))
                {
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }

                IFileSystem fileSystem = fileSystemShare.FileSystem;

                OpenResult openResult;
                FileSystemEntry entry = fileSystem.GetEntry(path);
                if (entry != null)
                {
                    if (request.OpenMode.FileExistsOpts == FileExistsOpts.ReturnError)
                    {
                        header.Status = NTStatus.STATUS_OBJECT_NAME_COLLISION;
                        return new ErrorResponse(CommandName.SMB_COM_OPEN_ANDX);
                    }
                    else if (request.OpenMode.FileExistsOpts == FileExistsOpts.TruncateToZero)
                    {
                        try
                        {
                            Stream temp = fileSystem.OpenFile(path, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite);
                            temp.Close();
                        }
                        catch (IOException ex)
                        {
                            ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                            if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                            {
                                header.Status = NTStatus.STATUS_SHARING_VIOLATION;
                                return new ErrorResponse(CommandName.SMB_COM_OPEN_ANDX);
                            }
                            else
                            {
                                header.Status = NTStatus.STATUS_DATA_ERROR;
                                return new ErrorResponse(CommandName.SMB_COM_OPEN_ANDX);
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            header.Status = NTStatus.STATUS_ACCESS_DENIED;
                            return new ErrorResponse(CommandName.SMB_COM_OPEN_ANDX);
                        }
                        openResult = OpenResult.FileExistedAndWasTruncated;
                    }
                    else // FileExistsOpts.Append
                    {
                        openResult = OpenResult.FileExistedAndWasOpened;
                    }
                }
                else
                {
                    if (request.OpenMode.CreateFile == CreateFile.ReturnErrorIfNotExist)
                    {
                        header.Status = NTStatus.STATUS_NO_SUCH_FILE;
                        return new ErrorResponse(CommandName.SMB_COM_OPEN_ANDX);
                    }

                    if ((request.FileAttrs & SMBFileAttributes.Directory) > 0)
                    {
                        state.LogToServer(Severity.Information, "OpenAndX: Creating directory '{0}'", path);
                        entry = fileSystem.CreateDirectory(path);
                    }
                    else
                    {
                        state.LogToServer(Severity.Information, "OpenAndX: Creating file '{0}'", path);
                        entry = fileSystem.CreateFile(path);
                    }
                    openResult = OpenResult.NotExistedAndWasCreated;
                }

                FileShare fileShare = ToFileShare(request.AccessMode.SharingMode);
                Stream stream = null;
                if (!entry.IsDirectory)
                {
                    bool buffered = (request.AccessMode.CachedMode == CachedMode.CachingAllowed && request.AccessMode.WriteThroughMode == WriteThroughMode.Disabled);
                    state.LogToServer(Severity.Verbose, "OpenAndX: Opening '{0}', Access={1}, Share={2}, Buffered={3}", path, fileAccess, fileShare, buffered);
                    stream = fileSystem.OpenFile(path, FileMode.Open, fileAccess, fileShare);
                    if (buffered)
                    {
                        stream = new PrefetchedStream(stream);
                    }
                }
                ushort? fileID = session.AddOpenFile(path, stream);
                if (!fileID.HasValue)
                {
                    header.Status = NTStatus.STATUS_TOO_MANY_OPENED_FILES;
                    return new ErrorResponse(CommandName.SMB_COM_OPEN_ANDX);
                }
                if (isExtended)
                {
                    return CreateResponseExtendedFromFileSystemEntry(entry, fileID.Value, openResult);
                }
                else
                {
                    return CreateResponseFromFileSystemEntry(entry, fileID.Value, openResult);
                }
            }
        }

        private static FileAccess ToFileAccess(AccessMode accessMode)
        {
            if (accessMode == AccessMode.Write)
            {
                return FileAccess.Write;
            }
            else if (accessMode == AccessMode.ReadWrite)
            {
                return FileAccess.ReadWrite;
            }
            else
            {
                return FileAccess.Read;
            }
        }

        private static FileShare ToFileShare(SharingMode sharingMode)
        {
            if (sharingMode == SharingMode.DenyReadWriteExecute)
            {
                return FileShare.None;
            }
            else if (sharingMode == SharingMode.DenyWrite)
            {
                return FileShare.Read;
            }
            else if (sharingMode == SharingMode.DenyReadExecute)
            {
                return FileShare.Write;
            }
            else
            {
                return FileShare.ReadWrite;
            }
        }

        private static OpenAndXResponse CreateResponseForNamedPipe(ushort fileID)
        {
            OpenAndXResponse response = new OpenAndXResponse();
            response.FID = fileID;
            response.AccessRights = AccessRights.SMB_DA_ACCESS_READ_WRITE;
            response.ResourceType = ResourceType.FileTypeMessageModePipe;
            response.NMPipeStatus.ICount = 255;
            response.NMPipeStatus.ReadMode = ReadMode.MessageMode;
            response.NMPipeStatus.NamedPipeType = NamedPipeType.MessageNodePipe;
            return response;
        }

        private static OpenAndXResponseExtended CreateResponseExtendedForNamedPipe(ushort fileID)
        {
            OpenAndXResponseExtended response = new OpenAndXResponseExtended();
            response.FID = fileID;
            response.AccessRights = AccessRights.SMB_DA_ACCESS_READ_WRITE;
            response.ResourceType = ResourceType.FileTypeMessageModePipe;
            response.NMPipeStatus.ICount = 255;
            response.NMPipeStatus.ReadMode = ReadMode.MessageMode;
            response.NMPipeStatus.NamedPipeType = NamedPipeType.MessageNodePipe;
            return response;
        }

        private static OpenAndXResponse CreateResponseFromFileSystemEntry(FileSystemEntry entry, ushort fileID, OpenResult openResult)
        {
            OpenAndXResponse response = new OpenAndXResponse();
            if (entry.IsDirectory)
            {
                response.FileAttrs = SMBFileAttributes.Directory;
            }
            else
            {
                response.FileAttrs = SMBFileAttributes.Normal;
            }
            response.FID = fileID;
            response.LastWriteTime = entry.LastWriteTime;
            response.FileDataSize = (uint)Math.Min(UInt32.MaxValue, entry.Size);
            response.AccessRights = AccessRights.SMB_DA_ACCESS_READ;
            response.ResourceType = ResourceType.FileTypeDisk;
            response.OpenResults.OpenResult = openResult;
            return response;
        }

        private static OpenAndXResponseExtended CreateResponseExtendedFromFileSystemEntry(FileSystemEntry entry, ushort fileID, OpenResult openResult)
        {
            OpenAndXResponseExtended response = new OpenAndXResponseExtended();
            if (entry.IsDirectory)
            {
                response.FileAttrs = SMBFileAttributes.Directory;
            }
            else
            {
                response.FileAttrs = SMBFileAttributes.Normal;
            }
            response.FID = fileID;
            response.LastWriteTime = entry.LastWriteTime;
            response.FileDataSize = (uint)Math.Min(UInt32.MaxValue, entry.Size);
            response.AccessRights = AccessRights.SMB_DA_ACCESS_READ;
            response.ResourceType = ResourceType.FileTypeDisk;
            response.OpenResults.OpenResult = openResult;
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
    }
}
