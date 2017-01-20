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
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    public partial class SMB1FileSystemHelper
    {
        public static NTStatus SetFileInformation(IFileSystem fileSystem, OpenFileObject openFile, SetInformation information, ConnectionState state)
        {
            if (information is SetInfoStandard)
            {
                return NTStatus.STATUS_SUCCESS;
            }
            else if (information is SetExtendedAttributes)
            {
                return NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (information is SetFileBasicInfo)
            {
                SetFileBasicInfo basicInfo = (SetFileBasicInfo)information;
                bool isHidden = (basicInfo.ExtFileAttributes & ExtendedFileAttributes.Hidden) > 0;
                bool isReadonly = (basicInfo.ExtFileAttributes & ExtendedFileAttributes.Readonly) > 0;
                bool isArchived = (basicInfo.ExtFileAttributes & ExtendedFileAttributes.Archive) > 0;
                try
                {
                    fileSystem.SetAttributes(openFile.Path, isHidden, isReadonly, isArchived);
                }
                catch (UnauthorizedAccessException)
                {
                    state.LogToServer(Severity.Debug, "SetFileInformation: Failed to set file attributes on '{0}'. Access Denied.", openFile.Path);
                    return NTStatus.STATUS_ACCESS_DENIED;
                }

                try
                {
                    fileSystem.SetDates(openFile.Path, basicInfo.CreationTime, basicInfo.LastWriteTime, basicInfo.LastAccessTime);
                }
                catch (IOException ex)
                {
                    ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                    if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                    {
                        // Returning STATUS_SHARING_VIOLATION is undocumented but apparently valid
                        state.LogToServer(Severity.Debug, "SetFileInformation: Failed to set file dates on '{0}'. Sharing Violation.", openFile.Path);
                        return NTStatus.STATUS_SHARING_VIOLATION;
                    }
                    else
                    {
                        state.LogToServer(Severity.Debug, "SetFileInformation: Failed to set file dates on '{0}'. Data Error.", openFile.Path);
                        return NTStatus.STATUS_DATA_ERROR;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    state.LogToServer(Severity.Debug, "SetFileInformation: Failed to set file dates on '{0}'. Access Denied.", openFile.Path);
                    return NTStatus.STATUS_ACCESS_DENIED;
                }
                return NTStatus.STATUS_SUCCESS;
            }
            else if (information is SetFileDispositionInfo)
            {
                if (((SetFileDispositionInfo)information).DeletePending)
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
                    catch (IOException ex)
                    {
                        ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                        if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                        {
                            state.LogToServer(Severity.Information, "SetFileInformation: Error deleting '{0}'. Sharing Violation.", openFile.Path);
                            return NTStatus.STATUS_SHARING_VIOLATION;
                        }
                        else
                        {
                            state.LogToServer(Severity.Information, "SetFileInformation: Error deleting '{0}'. Data Error.", openFile.Path);
                            return NTStatus.STATUS_DATA_ERROR;
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        state.LogToServer(Severity.Information, "SetFileInformation: Error deleting '{0}', Access Denied.", openFile.Path);
                        return NTStatus.STATUS_ACCESS_DENIED;
                    }
                }
                return NTStatus.STATUS_SUCCESS;
            }
            else if (information is SetFileAllocationInfo)
            {
                // This information level is used to set the file length in bytes.
                // Note: the input will NOT be a multiple of the cluster size / bytes per sector.
                ulong allocationSize = ((SetFileAllocationInfo)information).AllocationSize;
                try
                {
                    openFile.Stream.SetLength((long)allocationSize);
                }
                catch (IOException ex)
                {
                    ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                    if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                    {
                        state.LogToServer(Severity.Debug, "SetFileInformation: Cannot set allocation for '{0}'. Sharing Violation.", openFile.Path);
                        return NTStatus.STATUS_SHARING_VIOLATION;
                    }
                    else
                    {
                        state.LogToServer(Severity.Debug, "SetFileInformation: Cannot set allocation for '{0}'. Data Error.", openFile.Path);
                        return NTStatus.STATUS_DATA_ERROR;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    state.LogToServer(Severity.Debug, "SetFileInformation: Cannot set allocation for '{0}'. Access Denied.", openFile.Path);
                    return NTStatus.STATUS_ACCESS_DENIED;
                }
                return NTStatus.STATUS_SUCCESS;
            }
            else if (information is SetFileEndOfFileInfo)
            {
                ulong endOfFile = ((SetFileEndOfFileInfo)information).EndOfFile;
                try
                {
                    openFile.Stream.SetLength((long)endOfFile);
                }
                catch (IOException ex)
                {
                    ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                    if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                    {
                        state.LogToServer(Severity.Debug, "SetFileInformation: Cannot set end of file for '{0}'. Sharing Violation.", openFile.Path);
                        return NTStatus.STATUS_SHARING_VIOLATION;
                    }
                    else
                    {
                        state.LogToServer(Severity.Debug, "SetFileInformation: Cannot set end of file for '{0}'. Data Error.", openFile.Path);
                        return NTStatus.STATUS_DATA_ERROR;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    state.LogToServer(Severity.Debug, "SetFileInformation: Cannot set end of file for '{0}'. Access Denied.", openFile.Path);
                    return NTStatus.STATUS_ACCESS_DENIED;
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
