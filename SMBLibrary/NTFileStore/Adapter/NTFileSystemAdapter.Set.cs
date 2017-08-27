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
        public NTStatus SetFileInformation(object handle, FileInformation information)
        {
            FileHandle fileHandle = (FileHandle)handle;
            if (information is FileBasicInformation)
            {
                FileBasicInformation basicInformation = (FileBasicInformation)information;
                bool isHidden = ((basicInformation.FileAttributes & FileAttributes.Hidden) > 0);
                bool isReadonly = (basicInformation.FileAttributes & FileAttributes.ReadOnly) > 0;
                bool isArchived = (basicInformation.FileAttributes & FileAttributes.Archive) > 0;
                try
                {
                    m_fileSystem.SetAttributes(fileHandle.Path, isHidden, isReadonly, isArchived);
                }
                catch (Exception ex)
                {
                    NTStatus status = ToNTStatus(ex);
                    Log(Severity.Verbose, "SetFileInformation: Failed to set file attributes on '{0}'. {1}.", fileHandle.Path, status);
                    return status;
                }

                try
                {
                    m_fileSystem.SetDates(fileHandle.Path, basicInformation.CreationTime, basicInformation.LastWriteTime, basicInformation.LastAccessTime);
                }
                catch (Exception ex)
                {
                    NTStatus status = ToNTStatus(ex);
                    Log(Severity.Verbose, "SetFileInformation: Failed to set file dates on '{0}'. {1}.", fileHandle.Path, status);
                    return status;
                }
                return NTStatus.STATUS_SUCCESS;
            }
            else if (information is FileRenameInformationType2)
            {
                FileRenameInformationType2 renameInformation = (FileRenameInformationType2)information;
                string newFileName = renameInformation.FileName;
                if (!newFileName.StartsWith(@"\"))
                {
                    newFileName = @"\" + newFileName;
                }

                if (fileHandle.Stream != null)
                {
                    fileHandle.Stream.Close();
                }

                // Note: it's possible that we just want to upcase / downcase a filename letter.
                try
                {
                    if (renameInformation.ReplaceIfExists && (m_fileSystem.GetEntry(newFileName) != null))
                    {
                        m_fileSystem.Delete(newFileName);
                    }
                    m_fileSystem.Move(fileHandle.Path, newFileName);
                    Log(Severity.Information, "SetFileInformation: Renamed '{0}' to '{1}'", fileHandle.Path, newFileName);
                }
                catch (Exception ex)
                {
                    NTStatus status = ToNTStatus(ex);
                    Log(Severity.Verbose, "SetFileInformation: Cannot rename '{0}' to '{1}'. {2}.", fileHandle.Path, newFileName, status);
                    return status;
                }
                fileHandle.Path = newFileName;
                return NTStatus.STATUS_SUCCESS;
            }
            else if (information is FileDispositionInformation)
            {
                if (((FileDispositionInformation)information).DeletePending)
                {
                    // We're supposed to delete the file on close, but it's too late to report errors at this late stage
                    if (fileHandle.Stream != null)
                    {
                        fileHandle.Stream.Close();
                    }

                    try
                    {
                        m_fileSystem.Delete(fileHandle.Path);
                        Log(Severity.Information, "SetFileInformation: Deleted '{0}'", fileHandle.Path);
                    }
                    catch (Exception ex)
                    {
                        NTStatus status = ToNTStatus(ex);
                        Log(Severity.Information, "SetFileInformation: Error deleting '{0}'. {1}.", fileHandle.Path, status);
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
                    fileHandle.Stream.SetLength(allocationSize);
                }
                catch (Exception ex)
                {
                    NTStatus status = ToNTStatus(ex);
                    Log(Severity.Verbose, "SetFileInformation: Cannot set allocation for '{0}'. {1}.", fileHandle.Path, status);
                    return status;
                }
                return NTStatus.STATUS_SUCCESS;
            }
            else if (information is FileEndOfFileInformation)
            {
                long endOfFile = ((FileEndOfFileInformation)information).EndOfFile;
                try
                {
                    fileHandle.Stream.SetLength(endOfFile);
                }
                catch (Exception ex)
                {
                    NTStatus status = ToNTStatus(ex);
                    Log(Severity.Verbose, "SetFileInformation: Cannot set end of file for '{0}'. {1}.", fileHandle.Path, status);
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
