/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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
    /// <summary>
    /// A file store (a.k.a. object store) interface to allow access to a file system or a named pipe in an NT-like manner dictated by the SMB protocol.
    /// </summary>
    public interface INTFileStore
    {
        NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext);

        NTStatus CloseFile(object handle);

        NTStatus ReadFile(out byte[] data, object handle, long offset, int maxCount);

        NTStatus WriteFile(out int numberOfBytesWritten, object handle, long offset, byte[] data);

        NTStatus FlushFileBuffers(object handle);

        NTStatus QueryDirectory(out List<QueryDirectoryFileInformation> result, object handle, string fileName, FileInformationClass informationClass);

        NTStatus GetFileInformation(out FileInformation result, object handle, FileInformationClass informationClass);

        NTStatus SetFileInformation(object handle, FileInformation information);

        NTStatus GetFileSystemInformation(out FileSystemInformation result, FileSystemInformationClass informationClass);

        NTStatus DeviceIOControl(object handle, uint ctlCode, byte[] input, out byte[] output, int maxOutputLength);
    }
}
