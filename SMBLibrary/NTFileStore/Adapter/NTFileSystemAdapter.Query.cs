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

namespace SMBLibrary
{
    public partial class NTFileSystemAdapter
    {
        public NTStatus GetFileInformation(out FileInformation result, object handle, FileInformationClass informationClass)
        {
            FileHandle fileHandle = (FileHandle)handle;
            string path = fileHandle.Path;
            FileSystemEntry entry;
            try
            {
                entry = m_fileSystem.GetEntry(path);
            }
            catch (Exception ex)
            {
                NTStatus status = ToNTStatus(ex);
                Log(Severity.Verbose, "GetFileInformation on '{0}' failed. {1}", path, status);
                result = null;
                return status;
            }

            if (entry == null)
            {
                result = null;
                return NTStatus.STATUS_NO_SUCH_FILE;
            }

            switch (informationClass)
            {
                case FileInformationClass.FileBasicInformation:
                    {
                        FileBasicInformation information = new FileBasicInformation();
                        information.CreationTime = entry.CreationTime;
                        information.LastAccessTime = entry.LastAccessTime;
                        information.LastWriteTime = entry.LastWriteTime;
                        information.ChangeTime = entry.LastWriteTime;
                        information.FileAttributes = GetFileAttributes(entry);
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FileStandardInformation:
                    {
                        FileStandardInformation information = new FileStandardInformation();
                        information.AllocationSize = (long)GetAllocationSize(entry.Size);
                        information.EndOfFile = (long)entry.Size;
                        information.Directory = entry.IsDirectory;
                        information.DeletePending = fileHandle.DeleteOnClose;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FileInternalInformation:
                    {
                        FileInternalInformation information = new FileInternalInformation();
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FileEaInformation:
                    {
                        FileEaInformation information = new FileEaInformation();
                        information.EaSize = 0;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FileAccessInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileNameInformation:
                    {
                        FileNameInformation information = new FileNameInformation();
                        information.FileName = entry.Name;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FilePositionInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileFullEaInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileModeInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileAlignmentInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileAllInformation:
                    {
                        FileAllInformation information = new FileAllInformation();
                        information.BasicInformation.CreationTime = entry.CreationTime;
                        information.BasicInformation.LastAccessTime = entry.LastAccessTime;
                        information.BasicInformation.LastWriteTime = entry.LastWriteTime;
                        information.BasicInformation.ChangeTime = entry.LastWriteTime;
                        information.BasicInformation.FileAttributes = GetFileAttributes(entry);
                        information.StandardInformation.AllocationSize = (long)GetAllocationSize(entry.Size);
                        information.StandardInformation.EndOfFile = (long)entry.Size;
                        information.StandardInformation.Directory = entry.IsDirectory;
                        information.StandardInformation.DeletePending = fileHandle.DeleteOnClose;
                        information.NameInformation.FileName = entry.Name;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FileAlternateNameInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileStreamInformation:
                    {
                        // This information class is used to enumerate the data streams of a file or a directory.
                        // A buffer of FileStreamInformation data elements is returned by the server.
                        FileStreamInformation information = new FileStreamInformation();
                        FileStreamEntry streamEntry = new FileStreamEntry();
                        streamEntry.StreamSize = (long)entry.Size;
                        streamEntry.StreamAllocationSize = (long)GetAllocationSize(entry.Size);
                        streamEntry.StreamName = "::$DATA";
                        information.Entries.Add(streamEntry);
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FilePipeInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FilePipeLocalInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FilePipeRemoteInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileCompressionInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileNetworkOpenInformation:
                    {
                        FileNetworkOpenInformation information = new FileNetworkOpenInformation();
                        information.CreationTime = entry.CreationTime;
                        information.LastAccessTime = entry.LastAccessTime;
                        information.LastWriteTime = entry.LastWriteTime;
                        information.ChangeTime = entry.LastWriteTime;
                        information.AllocationSize = (long)GetAllocationSize(entry.Size);
                        information.EndOfFile = (long)entry.Size;
                        information.FileAttributes = GetFileAttributes(entry);
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FileAttributeTagInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                default:
                    result = null;
                    return NTStatus.STATUS_INVALID_INFO_CLASS;
            }
        }

        public static FileAttributes GetFileAttributes(FileSystemEntry entry)
        {
            FileAttributes attributes = 0;
            if (entry.IsHidden)
            {
                attributes |= FileAttributes.Hidden;
            }
            if (entry.IsReadonly)
            {
                attributes |= FileAttributes.ReadOnly;
            }
            if (entry.IsArchived)
            {
                attributes |= FileAttributes.Archive;
            }
            if (entry.IsDirectory)
            {
                attributes |= FileAttributes.Directory;
            }

            if (attributes == 0)
            {
                attributes = FileAttributes.Normal;
            }

            return attributes;
        }
    }
}
