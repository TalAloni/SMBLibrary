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

namespace SMBLibrary.Server
{
    public partial class NTFileSystemHelper
    {
        public static NTStatus SetFileInformation(IFileSystem fileSystem, OpenFileObject openFile, FileInformation information, ConnectionState state)
        {
            if (information is FileBasicInformation)
            {
                FileBasicInformation basicInformation = (FileBasicInformation)information;
                bool isHidden = ((basicInformation.FileAttributes & FileAttributes.Hidden) > 0);
                bool isReadonly = (basicInformation.FileAttributes & FileAttributes.ReadOnly) > 0;
                bool isArchived = (basicInformation.FileAttributes & FileAttributes.Archive) > 0;
                try
                {
                    fileSystem.SetAttributes(openFile.Path, isHidden, isReadonly, isArchived);
                }
                catch (Exception ex)
                {
                    NTStatus status = ToNTStatus(ex);
                    state.LogToServer(Severity.Debug, "SetFileInformation: Failed to set file attributes on '{0}'. {1}.", openFile.Path, status);
                    return status;
                }

                try
                {
                    fileSystem.SetDates(openFile.Path, basicInformation.CreationTime, basicInformation.LastWriteTime, basicInformation.LastAccessTime);
                }
                catch (Exception ex)
                {
                    NTStatus status = ToNTStatus(ex);
                    state.LogToServer(Severity.Debug, "SetFileInformation: Failed to set file dates on '{0}'. {1}.", openFile.Path, status);
                    return status;
                }
                return NTStatus.STATUS_SUCCESS;
            }
            else if (information is FileRenameInformationType2)
            {
                FileRenameInformationType2 renameInformation = (FileRenameInformationType2)information;
                string destination = renameInformation.FileName;
                if (!destination.StartsWith(@"\"))
                {
                    destination = @"\" + destination;
                }
                
                if (openFile.Stream != null)
                {
                    openFile.Stream.Close();
                }

                // Note: it's possible that we just want to upcase / downcase a filename letter.
                try
                {
                    if (renameInformation.ReplaceIfExists && (fileSystem.GetEntry(destination) != null ))
                    {
                        fileSystem.Delete(destination);
                    }
                    fileSystem.Move(openFile.Path, destination);
                }
                catch (Exception ex)
                {
                    NTStatus status = ToNTStatus(ex);
                    state.LogToServer(Severity.Debug, "SetFileInformation: Cannot rename '{0}'. {1}.", openFile.Path, status);
                    return status;
                }
                openFile.Path = destination;
                return NTStatus.STATUS_SUCCESS;
            }
            else if (information is FileDispositionInformation)
            {
                if (((FileDispositionInformation)information).DeletePending)
                {
                    // We're supposed to delete the file on close, but it's too late to report errors at this late stage
                    if (openFile.Stream != null)
                    {
                        openFile.Stream.Close();
                    }

                    try
                    {
                        state.LogToServer(Severity.Information, "SetFileInformation: Deleting file '{0}'", openFile.Path);
                        fileSystem.Delete(openFile.Path);
                    }
                    catch (Exception ex)
                    {
                        NTStatus status = ToNTStatus(ex);
                        state.LogToServer(Severity.Debug, "SetFileInformation: Error deleting '{0}'. {1}.", openFile.Path, status);
                        return status;
                    }
                }
                return NTStatus.STATUS_SUCCESS;
            }
            else if (information is FileAllocationInformation)
            {
                long allocationSize = ((FileAllocationInformation)information).AllocationSize;
                try
                {
                    openFile.Stream.SetLength(allocationSize);
                }
                catch (Exception ex)
                {
                    NTStatus status = ToNTStatus(ex);
                    state.LogToServer(Severity.Debug, "SetFileInformation: Cannot set allocation for '{0}'. {1}.", openFile.Path, status);
                    return status;
                }
                return NTStatus.STATUS_SUCCESS;
            }
            else if (information is FileEndOfFileInformation)
            {
                long endOfFile = ((FileEndOfFileInformation)information).EndOfFile;
                try
                {
                    openFile.Stream.SetLength(endOfFile);
                }
                catch (Exception ex)
                {
                    NTStatus status = ToNTStatus(ex);
                    state.LogToServer(Severity.Debug, "SetFileInformation: Cannot set end of file for '{0}'. {1}.", openFile.Path, status);
                    return status;
                }
                return NTStatus.STATUS_SUCCESS;
            }
            else
            {
                return NTStatus.STATUS_NOT_IMPLEMENTED;
            }
        }
    }
}
