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
            AccessMask desiredAccess;
            ShareAccess shareAccess;
            CreateDisposition createDisposition;
            try
            {
                desiredAccess = ToAccessMask(request.AccessMode.AccessMode);
                shareAccess = ToShareAccess(request.AccessMode.SharingMode);
                createDisposition = ToCreateDisposition(request.OpenMode);
            }
            catch (ArgumentException)
            {
                // Invalid input according to MS-CIFS
                header.Status = NTStatus.STATUS_OS2_INVALID_ACCESS;
                return new ErrorResponse(request.CommandName);
            }
            CreateOptions createOptions = ToCreateOptions(request.AccessMode);

            FileAccess fileAccess = ToFileAccess(request.AccessMode.AccessMode);
            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasAccess(session.UserName, path, fileAccess, state.ClientEndPoint))
                {
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            FileSystemEntry entry = null;
            Stream stream = null;
            FileStatus fileStatus;
            if (share is NamedPipeShare)
            {
                Stream pipeStream = ((NamedPipeShare)share).OpenPipe(path);
                if (pipeStream == null)
                {
                    header.Status = NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
                    return new ErrorResponse(request.CommandName);
                }
                fileStatus = FileStatus.FILE_OPENED;
            }
            else // FileSystemShare
            {
                FileSystemShare fileSystemShare = (FileSystemShare)share;
                IFileSystem fileSystem = fileSystemShare.FileSystem;

                header.Status = NTFileSystemHelper.CreateFile(out entry, out stream, out fileStatus, fileSystem, path, desiredAccess, shareAccess, createDisposition, createOptions, state);
                if (header.Status != NTStatus.STATUS_SUCCESS)
                {
                    return new ErrorResponse(request.CommandName);
                }
            }

            ushort? fileID = session.AddOpenFile(path, stream);
            if (!fileID.HasValue)
            {
                if (stream != null)
                {
                    stream.Close();
                }
                header.Status = NTStatus.STATUS_TOO_MANY_OPENED_FILES;
                return new ErrorResponse(request.CommandName);
            }

            OpenResult openResult = ToOpenResult(fileStatus);
            if (share is NamedPipeShare)
            {
                if (isExtended)
                {
                    return CreateResponseExtendedForNamedPipe(fileID.Value, openResult);
                }
                else
                {
                    return CreateResponseForNamedPipe(fileID.Value, openResult);
                }
            }
            else // FileSystemShare
            {
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

        private static AccessMask ToAccessMask(AccessMode accessMode)
        {
            if (accessMode == AccessMode.Read)
            {
                return FileAccessMask.GENERIC_READ;
            }
            if (accessMode == AccessMode.Write)
            {
                return FileAccessMask.GENERIC_WRITE | FileAccessMask.FILE_READ_ATTRIBUTES;
            }
            else if (accessMode == AccessMode.ReadWrite)
            {
                return FileAccessMask.GENERIC_READ | FileAccessMask.GENERIC_WRITE;
            }
            else if (accessMode == AccessMode.Execute)
            {
                return FileAccessMask.GENERIC_READ | FileAccessMask.GENERIC_EXECUTE;
            }
            else
            {
                throw new ArgumentException("Invalid AccessMode value");
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

        private static ShareAccess ToShareAccess(SharingMode sharingMode)
        {
            if (sharingMode == SharingMode.Compatibility)
            {
                return ShareAccess.FILE_SHARE_READ;
            }
            else if (sharingMode == SharingMode.DenyReadWriteExecute)
            {
                return 0;
            }
            else if (sharingMode == SharingMode.DenyWrite)
            {
                return ShareAccess.FILE_SHARE_READ;
            }
            else if (sharingMode == SharingMode.DenyReadExecute)
            {
                return ShareAccess.FILE_SHARE_WRITE;
            }
            else if (sharingMode == SharingMode.DenyNothing)
            {
                return ShareAccess.FILE_SHARE_READ | ShareAccess.FILE_SHARE_WRITE;
            }
            else if (sharingMode == (SharingMode)0xFF)
            {
                return 0;
            }
            else
            {
                throw new ArgumentException("Invalid SharingMode value");
            }
        }

        private static CreateDisposition ToCreateDisposition(OpenMode openMode)
        {
            if (openMode.CreateFile == CreateFile.ReturnErrorIfNotExist)
            {
                if (openMode.FileExistsOpts == FileExistsOpts.ReturnError)
                {
                    throw new ArgumentException("Invalid OpenMode combination");
                }
                else if (openMode.FileExistsOpts == FileExistsOpts.Append)
                {
                    return CreateDisposition.FILE_OPEN;
                }
                else if (openMode.FileExistsOpts == FileExistsOpts.TruncateToZero)
                {
                    return CreateDisposition.FILE_OVERWRITE;
                }
            }
            else if (openMode.CreateFile == CreateFile.CreateIfNotExist)
            {
                if (openMode.FileExistsOpts == FileExistsOpts.ReturnError)
                {
                    return CreateDisposition.FILE_CREATE;
                }
                else if (openMode.FileExistsOpts == FileExistsOpts.Append)
                {
                    return CreateDisposition.FILE_OPEN_IF;
                }
                else if (openMode.FileExistsOpts == FileExistsOpts.TruncateToZero)
                {
                    return CreateDisposition.FILE_OVERWRITE_IF;
                }
            }

            throw new ArgumentException("Invalid OpenMode combination");
        }

        private static CreateOptions ToCreateOptions(AccessModeOptions accessModeOptions)
        {
            CreateOptions result = CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_COMPLETE_IF_OPLOCKED;
            if (accessModeOptions.ReferenceLocality == ReferenceLocality.Sequential)
            {
                result |= CreateOptions.FILE_SEQUENTIAL_ONLY;
            }
            else if (accessModeOptions.ReferenceLocality == ReferenceLocality.Random)
            {
                result |= CreateOptions.FILE_RANDOM_ACCESS;
            }
            else if (accessModeOptions.ReferenceLocality == ReferenceLocality.RandomWithLocality)
            {
                result |= CreateOptions.FILE_RANDOM_ACCESS;
            }

            if (accessModeOptions.CachedMode == CachedMode.DoNotCacheFile)
            {
                result |= CreateOptions.FILE_NO_INTERMEDIATE_BUFFERING;
            }

            if (accessModeOptions.WriteThroughMode == WriteThroughMode.WriteThrough)
            {
                result |= CreateOptions.FILE_WRITE_THROUGH;
            }
            return result;
        }

        private static OpenResult ToOpenResult(FileStatus fileStatus)
        {
            if (fileStatus == FileStatus.FILE_OVERWRITTEN ||
                fileStatus == FileStatus.FILE_SUPERSEDED)
            {
                return OpenResult.FileExistedAndWasTruncated;
            }
            else if (fileStatus == FileStatus.FILE_CREATED)
            {
                return OpenResult.NotExistedAndWasCreated;
            }
            else
            {
                return OpenResult.FileExistedAndWasOpened;
            }
        }

        private static OpenAndXResponse CreateResponseForNamedPipe(ushort fileID, OpenResult openResult)
        {
            OpenAndXResponse response = new OpenAndXResponse();
            response.FID = fileID;
            response.AccessRights = AccessRights.SMB_DA_ACCESS_READ_WRITE;
            response.ResourceType = ResourceType.FileTypeMessageModePipe;
            response.NMPipeStatus.ICount = 255;
            response.NMPipeStatus.ReadMode = ReadMode.MessageMode;
            response.NMPipeStatus.NamedPipeType = NamedPipeType.MessageNodePipe;
            response.OpenResults.OpenResult = openResult;
            return response;
        }

        private static OpenAndXResponseExtended CreateResponseExtendedForNamedPipe(ushort fileID, OpenResult openResult)
        {
            OpenAndXResponseExtended response = new OpenAndXResponseExtended();
            response.FID = fileID;
            response.AccessRights = AccessRights.SMB_DA_ACCESS_READ_WRITE;
            response.ResourceType = ResourceType.FileTypeMessageModePipe;
            response.NMPipeStatus.ICount = 255;
            response.NMPipeStatus.ReadMode = ReadMode.MessageMode;
            response.NMPipeStatus.NamedPipeType = NamedPipeType.MessageNodePipe;
            response.OpenResults.OpenResult = openResult;
            return response;
        }

        private static OpenAndXResponse CreateResponseFromFileSystemEntry(FileSystemEntry entry, ushort fileID, OpenResult openResult)
        {
            OpenAndXResponse response = new OpenAndXResponse();
            response.FID = fileID;
            if (entry.IsDirectory)
            {
                response.FileAttrs = SMBFileAttributes.Directory;
            }
            else
            {
                response.FileAttrs = SMBFileAttributes.Normal;
            }
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
            response.FID = fileID;
            if (entry.IsDirectory)
            {
                response.FileAttrs = SMBFileAttributes.Directory;
            }
            else
            {
                response.FileAttrs = SMBFileAttributes.Normal;
            }
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
