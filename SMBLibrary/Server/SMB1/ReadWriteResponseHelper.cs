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
using SMBLibrary.RPC;
using SMBLibrary.SMB1;
using SMBLibrary.Services;
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    public class ReadWriteResponseHelper
    {
        internal static SMB1Command GetReadResponse(SMB1Header header, ReadRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            OpenedFileObject openedFile = state.GetOpenedFileObject(request.FID);
            if (openedFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }
            byte[] data;
            header.Status = ReadFile(out data, openedFile, request.ReadOffsetInBytes, request.CountOfBytesToRead, state);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName);
            }

            ReadResponse response = new ReadResponse();
            response.Bytes = data;
            response.CountOfBytesReturned = (ushort)data.Length;
            return response;
        }

        internal static SMB1Command GetReadResponse(SMB1Header header, ReadAndXRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            OpenedFileObject openedFile = state.GetOpenedFileObject(request.FID);
            if (openedFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }
            uint maxCount = request.MaxCount;
            if ((share is FileSystemShare) && state.LargeRead)
            {
                maxCount = request.MaxCountLarge;
            }
            byte[] data;
            header.Status = ReadFile(out data, openedFile, (long)request.Offset, (int)maxCount, state);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName);
            }

            ReadAndXResponse response = new ReadAndXResponse();
            if (share is FileSystemShare)
            {
                // If the client reads from a disk file, this field MUST be set to -1 (0xFFFF)
                response.Available = 0xFFFF;
            }
            response.Data = data;
            return response;
        }

        public static NTStatus ReadFile(out byte[] data, OpenedFileObject openedFile, long offset, int maxCount, ConnectionState state)
        {
            data = null;
            string openedFilePath = openedFile.Path;
            Stream stream = openedFile.Stream;
            if (stream is RPCPipeStream)
            {
                data = new byte[maxCount];
                int bytesRead = stream.Read(data, 0, maxCount);
                if (bytesRead < maxCount)
                {
                    // EOF, we must trim the response data array
                    data = ByteReader.ReadBytes(data, 0, bytesRead);
                }
                return NTStatus.STATUS_SUCCESS;
            }
            else // File
            {
                if (stream == null)
                {
                    state.LogToServer(Severity.Debug, "ReadFile: Cannot read '{0}', Invalid Operation.", openedFilePath);
                    return NTStatus.STATUS_ACCESS_DENIED;
                }

                int bytesRead;
                try
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    data = new byte[maxCount];
                    bytesRead = stream.Read(data, 0, maxCount);
                }
                catch (IOException ex)
                {
                    ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                    if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                    {
                        // Returning STATUS_SHARING_VIOLATION is undocumented but apparently valid
                        state.LogToServer(Severity.Debug, "ReadFile: Cannot read '{0}'. Sharing Violation.", openedFilePath);
                        return NTStatus.STATUS_SHARING_VIOLATION;
                    }
                    else
                    {
                        state.LogToServer(Severity.Debug, "ReadFile: Cannot read '{0}'. Data Error.", openedFilePath);
                        return NTStatus.STATUS_DATA_ERROR;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    state.LogToServer(Severity.Debug, "ReadFile: Cannot read '{0}'. Offset Out Of Range.", openedFilePath);
                    return NTStatus.STATUS_DATA_ERROR;
                }
                catch (UnauthorizedAccessException)
                {
                    state.LogToServer(Severity.Debug, "ReadFile: Cannot read '{0}', Access Denied.", openedFilePath);
                    return NTStatus.STATUS_ACCESS_DENIED;
                }

                if (bytesRead < maxCount)
                {
                    // EOF, we must trim the response data array
                    data = ByteReader.ReadBytes(data, 0, bytesRead);
                }
                return NTStatus.STATUS_SUCCESS;
            }
        }

        internal static SMB1Command GetWriteResponse(SMB1Header header, WriteRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            OpenedFileObject openedFile = state.GetOpenedFileObject(request.FID);
            if (openedFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return new ErrorResponse(request.CommandName);
            }
            int numberOfBytesWritten;
            header.Status = WriteFile(out numberOfBytesWritten, openedFile, request.WriteOffsetInBytes, request.Data, state);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName);
            }
            WriteResponse response = new WriteResponse();
            response.CountOfBytesWritten = (ushort)numberOfBytesWritten;
            return response;
        }

        internal static SMB1Command GetWriteResponse(SMB1Header header, WriteAndXRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            OpenedFileObject openedFile = state.GetOpenedFileObject(request.FID);
            if (openedFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return new ErrorResponse(request.CommandName);
            }
            int numberOfBytesWritten;
            header.Status = WriteFile(out numberOfBytesWritten, openedFile, (long)request.Offset, request.Data, state);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName);
            }
            WriteAndXResponse response = new WriteAndXResponse();
            response.Count = (uint)numberOfBytesWritten;
            if (share is FileSystemShare)
            {
                // If the client wrote to a disk file, this field MUST be set to 0xFFFF.
                response.Available = 0xFFFF;
            }
            return response;
        }

        public static NTStatus WriteFile(out int numberOfBytesWritten, OpenedFileObject openedFile, long offset, byte[] data, ConnectionState state)
        {
            numberOfBytesWritten = 0;
            string openedFilePath = openedFile.Path;
            Stream stream = openedFile.Stream;
            if (stream is RPCPipeStream)
            {
                stream.Write(data, 0, data.Length);
                numberOfBytesWritten = data.Length;
                return NTStatus.STATUS_SUCCESS;
            }
            else // File
            {
                if (stream == null)
                {
                    state.LogToServer(Severity.Debug, "WriteFile: Cannot write '{0}'. Invalid Operation.", openedFilePath);
                    return NTStatus.STATUS_ACCESS_DENIED;
                }

                try
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    stream.Write(data, 0, data.Length);
                    numberOfBytesWritten = data.Length;
                    return NTStatus.STATUS_SUCCESS;
                }
                catch (IOException ex)
                {
                    ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                    if (errorCode == (ushort)Win32Error.ERROR_DISK_FULL)
                    {
                        state.LogToServer(Severity.Debug, "WriteFile: Cannot write '{0}'. Disk Full.", openedFilePath);
                        return NTStatus.STATUS_DISK_FULL;
                    }
                    else if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                    {
                        state.LogToServer(Severity.Debug, "WriteFile: Cannot write '{0}'. Sharing Violation.", openedFilePath);
                        // Returning STATUS_SHARING_VIOLATION is undocumented but apparently valid
                        return NTStatus.STATUS_SHARING_VIOLATION;
                    }
                    else
                    {
                        state.LogToServer(Severity.Debug, "WriteFile: Cannot write '{0}'. Data Error.", openedFilePath);
                        return NTStatus.STATUS_DATA_ERROR;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    state.LogToServer(Severity.Debug, "WriteFile: Cannot write '{0}'. Offset Out Of Range.", openedFilePath);
                    return NTStatus.STATUS_DATA_ERROR;
                }
                catch (UnauthorizedAccessException)
                {
                    state.LogToServer(Severity.Debug, "WriteFile: Cannot write '{0}'. Access Denied.", openedFilePath);
                    // The user may have tried to write to a readonly file
                    return NTStatus.STATUS_ACCESS_DENIED;
                }
            }
        }
    }
}
