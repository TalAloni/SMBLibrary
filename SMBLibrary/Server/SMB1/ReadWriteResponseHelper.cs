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
    internal class ReadWriteResponseHelper
    {
        internal static SMB1Command GetReadResponse(SMB1Header header, ReadRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            OpenFileObject openFile = session.GetOpenFileObject(request.FID);
            if (openFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }

            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasReadAccess(session.SecurityContext, openFile.Path))
                {
                    state.LogToServer(Severity.Verbose, "Read from '{0}{1}' failed. User '{2}' was denied access.", share.Name, openFile.Path, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            byte[] data;
            header.Status = share.FileStore.ReadFile(out data, openFile.Handle, request.ReadOffsetInBytes, request.CountOfBytesToRead);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "Read from '{0}{1}' failed. NTStatus: {2}.", share.Name, openFile.Path, header.Status);
                return new ErrorResponse(request.CommandName);
            }

            ReadResponse response = new ReadResponse();
            response.Bytes = data;
            response.CountOfBytesReturned = (ushort)data.Length;
            return response;
        }

        internal static SMB1Command GetReadResponse(SMB1Header header, ReadAndXRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            OpenFileObject openFile = session.GetOpenFileObject(request.FID);
            if (openFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }

            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasReadAccess(session.SecurityContext, openFile.Path))
                {
                    state.LogToServer(Severity.Verbose, "ReadAndX from '{0}{1}' failed. User '{2}' was denied access.", share.Name, openFile.Path, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            uint maxCount = request.MaxCount;
            if ((share is FileSystemShare) && state.LargeRead)
            {
                maxCount = request.MaxCountLarge;
            }
            byte[] data;
            header.Status = share.FileStore.ReadFile(out data, openFile.Handle, (long)request.Offset, (int)maxCount);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "Read from '{0}{1}' failed. NTStatus: {2}.", share.Name, openFile.Path, header.Status);
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

        internal static SMB1Command GetWriteResponse(SMB1Header header, WriteRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            OpenFileObject openFile = session.GetOpenFileObject(request.FID);
            if (openFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return new ErrorResponse(request.CommandName);
            }

            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasWriteAccess(session.SecurityContext, openFile.Path))
                {
                    state.LogToServer(Severity.Verbose, "Write to '{0}{1}' failed. User '{2}' was denied access.", share.Name, openFile.Path, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            int numberOfBytesWritten;
            header.Status = share.FileStore.WriteFile(out numberOfBytesWritten, openFile.Handle, request.WriteOffsetInBytes, request.Data);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "Write to '{0}{1}' failed. NTStatus: {2}.", share.Name, openFile.Path, header.Status);
                return new ErrorResponse(request.CommandName);
            }
            WriteResponse response = new WriteResponse();
            response.CountOfBytesWritten = (ushort)numberOfBytesWritten;
            return response;
        }

        internal static SMB1Command GetWriteResponse(SMB1Header header, WriteAndXRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            OpenFileObject openFile = session.GetOpenFileObject(request.FID);
            if (openFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return new ErrorResponse(request.CommandName);
            }

            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasWriteAccess(session.SecurityContext, openFile.Path))
                {
                    state.LogToServer(Severity.Verbose, "WriteAndX to '{0}{1}' failed. User '{2}' was denied access.", share.Name, openFile.Path, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            int numberOfBytesWritten;
            header.Status = share.FileStore.WriteFile(out numberOfBytesWritten, openFile.Handle, (long)request.Offset, request.Data);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "Write to '{0}{1}' failed. NTStatus: {2}.", share.Name, openFile.Path, header.Status);
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

        internal static SMB1Command GetFlushResponse(SMB1Header header, FlushRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            if (request.FID == 0xFFFF)
            {
                // [MS-CIFS] If the FID is 0xFFFF, the Server.Connection.FileOpenTable MUST be scanned for
                // all files that were opened by the PID listed in the request header.
                // The server MUST attempt to flush each Server.Open so listed.
                return new FlushResponse();
            }

            OpenFileObject openFile = session.GetOpenFileObject(request.FID);
            if (openFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return new ErrorResponse(request.CommandName);
            }
            header.Status = share.FileStore.FlushFileBuffers(openFile.Handle);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "Flush '{0}{1}' failed. NTStatus: {2}.", share.Name, openFile.Path, header.Status);
                return new ErrorResponse(request.CommandName);
            }
            return new FlushResponse();
        }
    }
}
