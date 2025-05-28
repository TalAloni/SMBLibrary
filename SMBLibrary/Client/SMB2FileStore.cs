/* Copyright (C) 2017-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Threading;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Client
{
    public class SMB2FileStore : ISMBFileStore
    {
        private const int BytesPerCredit = 65536;

        private SMB2Client m_client;
        private uint m_treeID;
        private bool m_encryptShareData;

        /// <summary>
        /// Delegate for handling directory change notifications from the server.
        /// </summary>
        /// <param name="status">
        /// NTStatus.SUCCESS if changes were detected; an error code (e.g., STATUS_NOTIFY_CLEANUP) if the server terminated the request.
        /// </param>
        /// <param name="buffer">
        /// Raw change notification response data (null if an error occurred).
        /// </param>
        /// <param name="context">
        /// User-defined context passed during NotifyChange registration.
        /// </param>
        public delegate void NotifyChangeEventHandler(NTStatus status, byte[] buffer, object context);
        /// <summary>
        /// Event raised when the server responds to a ChangeNotifyRequest
        /// (e.g., directory changes detected or an error occurred).
        /// Clients should subscribe to this event to receive notifications. Multiple subscribers are supported.
        /// </summary>
        public event NotifyChangeEventHandler NotifyChangeEvent;
        /// <summary>
        /// Platform-specific implementation for stopping the NotifyLoop.
        /// 
        /// .NET 4.0+/NetStandard 2.0: Uses CancellationTokenSource for thread-safe, cooperative cancellation.
        /// This is the preferred method as it integrates with async/await and ensures clean termination.
        /// 
        /// .NET 2.0: Falls back to a volatile boolean flag for cancellation. This is a best-effort approach
        /// due to limited synchronization primitives in .NET 2.0. It may not guarantee immediate termination.
        /// </summary>
#if NET40 || NETSTANDARD2_0
        private CancellationTokenSource _cancelNotify = new CancellationTokenSource();
#else
        private volatile bool _isCancelled = false;
#endif
        /// <summary>
        /// Tracks the currently active ChangeNotifyRequest to enable server-side cancellation.
        /// The Cancel() method uses this field to:
        /// - Retrieve the MessageID of the pending request for the SMB2 CANCEL command.
        /// - Ensure the server correctly identifies and terminates the request.
        /// </summary>
        private ChangeNotifyRequest m_pendingChangeNotifyRequest;


        public SMB2FileStore(SMB2Client client, uint treeID, bool encryptShareData)
        {
            m_client = client;
            m_treeID = treeID;
            m_encryptShareData = encryptShareData;
        }

        public NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, FileAttributes fileAttributes, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext)
        {
            handle = null;
            fileStatus = FileStatus.FILE_DOES_NOT_EXIST;
            CreateRequest request = new CreateRequest();
            request.Name = path;
            request.DesiredAccess = desiredAccess;
            request.FileAttributes = fileAttributes;
            request.ShareAccess = shareAccess;
            request.CreateDisposition = createDisposition;
            request.CreateOptions = createOptions;
            request.ImpersonationLevel = ImpersonationLevel.Impersonation;
            TrySendCommand(request);

            SMB2Command response = m_client.WaitForCommand(request.MessageID, out bool connectionTerminated);
            if (response != null)
            {
                if (response.Header.Status == NTStatus.STATUS_SUCCESS && response is CreateResponse)
                {
                    CreateResponse createResponse = ((CreateResponse)response);
                    handle = createResponse.FileId;
                    fileStatus = ToFileStatus(createResponse.CreateAction);
                }
                return response.Header.Status;
            }

            return connectionTerminated ? NTStatus.STATUS_INVALID_SMB : NTStatus.STATUS_IO_TIMEOUT;
        }

        public NTStatus CloseFile(object handle)
        {
            CloseRequest request = new CloseRequest();
            request.FileId = (FileID)handle;
            TrySendCommand(request);
            SMB2Command response = m_client.WaitForCommand(request.MessageID, out bool connectionTerminated);
            if (response != null)
            {
                return response.Header.Status;
            }

            return connectionTerminated ? NTStatus.STATUS_INVALID_SMB : NTStatus.STATUS_IO_TIMEOUT;
        }

        public NTStatus ReadFile(out byte[] data, object handle, long offset, int maxCount)
        {
            data = null;
            ReadRequest request = new ReadRequest();
            request.Header.CreditCharge = (ushort)Math.Ceiling((double)maxCount / BytesPerCredit);
            request.FileId = (FileID)handle;
            request.Offset = (ulong)offset;
            request.ReadLength = (uint)maxCount;
            
            TrySendCommand(request);
            SMB2Command response = m_client.WaitForCommand(request.MessageID, out bool connectionTerminated);
            if (response != null)
            {
                if (response.Header.Status == NTStatus.STATUS_SUCCESS && response is ReadResponse)
                {
                    data = ((ReadResponse)response).Data;
                }
                return response.Header.Status;
            }

            return connectionTerminated ? NTStatus.STATUS_INVALID_SMB : NTStatus.STATUS_IO_TIMEOUT;
        }

        public NTStatus WriteFile(out int numberOfBytesWritten, object handle, long offset, byte[] data)
        {
            numberOfBytesWritten = 0;
            WriteRequest request = new WriteRequest();
            request.Header.CreditCharge = (ushort)Math.Ceiling((double)data.Length / BytesPerCredit);
            request.FileId = (FileID)handle;
            request.Offset = (ulong)offset;
            request.Data = data;

            TrySendCommand(request);
            SMB2Command response = m_client.WaitForCommand(request.MessageID, out bool connectionTerminated);
            if (response != null)
            {
                if (response.Header.Status == NTStatus.STATUS_SUCCESS && response is WriteResponse)
                {
                    numberOfBytesWritten = (int)((WriteResponse)response).Count;
                }
                return response.Header.Status;
            }

            return connectionTerminated ? NTStatus.STATUS_INVALID_SMB : NTStatus.STATUS_IO_TIMEOUT;
        }

        public NTStatus FlushFileBuffers(object handle)
        {
            FlushRequest request = new FlushRequest();
            request.FileId = (FileID) handle;

            TrySendCommand(request);
            SMB2Command response = m_client.WaitForCommand(request.MessageID, out bool connectionTerminated);
            if (response != null)
            {
                if (response.Header.Status == NTStatus.STATUS_SUCCESS && response is FlushResponse)
                {
                    return response.Header.Status;
                }
            }

            return connectionTerminated ? NTStatus.STATUS_INVALID_SMB : NTStatus.STATUS_IO_TIMEOUT;
        }

        public NTStatus LockFile(object handle, long byteOffset, long length, bool exclusiveLock)
        {
            throw new NotImplementedException();
        }

        public NTStatus UnlockFile(object handle, long byteOffset, long length)
        {
            throw new NotImplementedException();
        }

        public NTStatus QueryDirectory(out List<QueryDirectoryFileInformation> result, object handle, string fileName, FileInformationClass informationClass)
        {
            result = new List<QueryDirectoryFileInformation>();
            QueryDirectoryRequest request = new QueryDirectoryRequest();
            request.Header.CreditCharge = (ushort)Math.Ceiling((double)m_client.MaxTransactSize / BytesPerCredit);
            request.FileInformationClass = informationClass;
            request.Reopen = true;
            request.FileId = (FileID)handle;
            request.OutputBufferLength = m_client.MaxTransactSize;
            request.FileName = fileName;

            TrySendCommand(request);
            SMB2Command response = m_client.WaitForCommand(request.MessageID, out bool connectionTerminated);
            if (response != null)
            {
                while (response.Header.Status == NTStatus.STATUS_SUCCESS && response is QueryDirectoryResponse)
                {
                    List<QueryDirectoryFileInformation> page = ((QueryDirectoryResponse)response).GetFileInformationList(informationClass);
                    result.AddRange(page);
                    request.Reopen = false;
                    TrySendCommand(request);
                    response = m_client.WaitForCommand(request.MessageID, out connectionTerminated);
                    if (response == null)
                    {
                        return connectionTerminated ? NTStatus.STATUS_INVALID_SMB : NTStatus.STATUS_IO_TIMEOUT;
                    }
                }
                return response.Header.Status;
            }

            return connectionTerminated ? NTStatus.STATUS_INVALID_SMB : NTStatus.STATUS_IO_TIMEOUT;
        }

        public NTStatus GetFileInformation(out FileInformation result, object handle, FileInformationClass informationClass)
        {
            result = null;
            QueryInfoRequest request = new QueryInfoRequest();
            request.InfoType = InfoType.File;
            request.FileInformationClass = informationClass;
            request.OutputBufferLength = 4096;
            request.FileId = (FileID)handle;

            TrySendCommand(request);
            SMB2Command response = m_client.WaitForCommand(request.MessageID, out bool connectionTerminated);
            if (response != null)
            {
                if (response.Header.Status == NTStatus.STATUS_SUCCESS && response is QueryInfoResponse)
                {
                    result = ((QueryInfoResponse)response).GetFileInformation(informationClass);
                }
                return response.Header.Status;
            }

            return connectionTerminated ? NTStatus.STATUS_INVALID_SMB : NTStatus.STATUS_IO_TIMEOUT;
        }

        public NTStatus SetFileInformation(object handle, FileInformation information)
        {
            SetInfoRequest request = new SetInfoRequest();
            request.InfoType = InfoType.File;
            request.FileInformationClass = information.FileInformationClass;
            request.FileId = (FileID)handle;
            request.SetFileInformation(information);

            TrySendCommand(request);
            SMB2Command response = m_client.WaitForCommand(request.MessageID, out bool connectionTerminated);
            if (response != null)
            {
                return response.Header.Status;
            }

            return connectionTerminated ? NTStatus.STATUS_INVALID_SMB : NTStatus.STATUS_IO_TIMEOUT;
        }

        public NTStatus GetFileSystemInformation(out FileSystemInformation result, FileSystemInformationClass informationClass)
        {
            result = null;
            object fileHandle;
            FileStatus fileStatus;
            NTStatus status = CreateFile(out fileHandle, out fileStatus, String.Empty, (AccessMask)DirectoryAccessMask.FILE_LIST_DIRECTORY | (AccessMask)DirectoryAccessMask.FILE_READ_ATTRIBUTES | AccessMask.SYNCHRONIZE, 0, ShareAccess.Read | ShareAccess.Write | ShareAccess.Delete, CreateDisposition.FILE_OPEN, CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT | CreateOptions.FILE_DIRECTORY_FILE, null);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                return status;
            }

            status = GetFileSystemInformation(out result, fileHandle, informationClass);
            CloseFile(fileHandle);
            return status;
        }

        public NTStatus GetFileSystemInformation(out FileSystemInformation result, object handle, FileSystemInformationClass informationClass)
        {
            result = null;
            QueryInfoRequest request = new QueryInfoRequest();
            request.InfoType = InfoType.FileSystem;
            request.FileSystemInformationClass = informationClass;
            request.OutputBufferLength = 4096;
            request.FileId = (FileID)handle;

            TrySendCommand(request);
            SMB2Command response = m_client.WaitForCommand(request.MessageID, out bool connectionTerminated);
            if (response != null)
            {
                if (response.Header.Status == NTStatus.STATUS_SUCCESS && response is QueryInfoResponse)
                {
                    result = ((QueryInfoResponse)response).GetFileSystemInformation(informationClass);
                }
                return response.Header.Status;
            }

            return connectionTerminated ? NTStatus.STATUS_INVALID_SMB : NTStatus.STATUS_IO_TIMEOUT;
        }

        public NTStatus SetFileSystemInformation(FileSystemInformation information)
        {
            throw new NotImplementedException();
        }

        public NTStatus GetSecurityInformation(out SecurityDescriptor result, object handle, SecurityInformation securityInformation)
        {
            result = null;
            QueryInfoRequest request = new QueryInfoRequest();
            request.InfoType = InfoType.Security;
            request.SecurityInformation = securityInformation;
            request.OutputBufferLength = 4096;
            request.FileId = (FileID)handle;

            TrySendCommand(request);
            SMB2Command response = m_client.WaitForCommand(request.MessageID, out bool connectionTerminated);
            if (response != null)
            {
                if (response.Header.Status == NTStatus.STATUS_SUCCESS && response is QueryInfoResponse)
                {
                    result = ((QueryInfoResponse)response).GetSecurityInformation();
                }
                return response.Header.Status;
            }

            return connectionTerminated ? NTStatus.STATUS_INVALID_SMB : NTStatus.STATUS_IO_TIMEOUT;
        }

        public NTStatus SetSecurityInformation(object handle, SecurityInformation securityInformation, SecurityDescriptor securityDescriptor)
        {
            return NTStatus.STATUS_NOT_SUPPORTED;
        }



        public NTStatus StartMonitoring(
            out object ioRequest,
            string path,
            NotifyChangeFilter completionFilter,
            bool watchTree,
            int outputBufferSize,
            NotifyChangeEventHandler notifyChangeEventHandler)
        {
            // Open a handle to the directory
            object directoryHandle;
            FileStatus fileStatus;
            NTStatus createStatus = CreateFile(
                out directoryHandle,
                out fileStatus,
                path,
                AccessMask.GENERIC_READ,
                SMBLibrary.FileAttributes.Directory,
                ShareAccess.Read | ShareAccess.Write,
                CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_DIRECTORY_FILE,
                null);

            if (createStatus != NTStatus.STATUS_SUCCESS)
            {
                ioRequest = null;
                return createStatus;
            }

            // Subscribe to NotifyChangeEvent for change notifications
            NotifyChangeEvent += notifyChangeEventHandler;

            // Start monitoring with NotifyChange
            NTStatus notifyStatus = NotifyChange(
                out ioRequest,
                directoryHandle,
                completionFilter,
                watchTree,
                outputBufferSize,
                null,
                new { Client = m_client, FileStore = this, DirectoryHandle = directoryHandle });

            return notifyStatus;
        }

        public NTStatus StopMonitoring(object ioRequest, NotifyChangeEventHandler notifyChangeEventHandler)
        {
            // Unsubscribe from NotifyChangeEvent
            NotifyChangeEvent -= notifyChangeEventHandler;

            // Cancel the monitoring request
            NTStatus cancelStatus = Cancel(ioRequest);

            return cancelStatus;
        }

        /// <summary>
        /// Initiates monitoring of a directory for file system changes using the SMB protocol.
        /// </summary>
        /// <remarks>
        /// To monitor a directory, a <c>ChangeNotifyRequest</c> must be sent with all fields explicitly set.
        /// The <c>Reserved</c> field must be set to 0, as some SMB servers validate this field and return
        /// <c>STATUS_INVALID_PARAMETER</c> (or similar errors) if it is non-zero, interpreting it as a malformed request.
        ///
        /// The request is sent asynchronously in a loop to maintain continuous monitoring. SMB requires
        /// one request per notification (one-shot model), so the loop reissues the request after each response
        /// until the user cancels monitoring via <c>CancelOperation</c> or closes the file handle.
        /// </remarks>
        /// <param name="ioRequest">The SMB request object (output).</param>
        /// <param name="handle">The file handle (FileID) of the directory to monitor.</param>
        /// <param name="completionFilter">
        /// Bitmask specifying the types of changes to monitor (e.g., file creation, modification).
        /// </param>
        /// <param name="watchTree">
        /// True to monitor subdirectories recursively; false for the directory only.
        /// </param>
        /// <param name="outputBufferSize">
        /// Size of the buffer to store change notifications. This should be a power of two
        /// (e.g., 1024, 4096) to ensure compatibility with SMB server implementations.
        /// </param>
        /// <param name="onNotifyChangeCompleted">
        /// Callback invoked when a change is detected or an error occurs.
        /// </param>
        /// <param name="context">
        /// User-defined context passed to the callback.
        /// </param>
        /// <returns>
        /// <c>NTStatus.STATUS_PENDING</c> if monitoring started successfully; other status codes indicate failure.
        /// </returns>
        public NTStatus NotifyChange(out object ioRequest, object handle, NotifyChangeFilter completionFilter, bool watchTree, int outputBufferSize, OnNotifyChangeCompleted onNotifyChangeCompleted, object context)
        {
            if (!(handle is FileID fileId))
            {
                ioRequest = null;
                return NTStatus.STATUS_INVALID_HANDLE;
            }

            var request = new ChangeNotifyRequest
            {
                FileId = fileId, // Handle to the directory being monitored
                CompletionFilter = completionFilter, // Types of changes to report (e.g., FILE_NOTIFY_CHANGE_FILE_NAME)
                Flags = watchTree ? ChangeNotifyFlags.WatchTree : ChangeNotifyFlags.None, // Recursive monitoring flag. Should the server monitor recursively?
                OutputBufferLength = (uint)outputBufferSize, // Buffer size for returned change data
                Reserved = 0 // MUST be zero per SMB protocol specification; non-zero may trigger server errors
            };

            m_pendingChangeNotifyRequest = request;
            ioRequest = request;

#if NET40 || NETSTANDARD2_0
            // Use a dedicated thread to avoid blocking the SMB client
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                NotifyLoop(completionFilter, watchTree, outputBufferSize, onNotifyChangeCompleted, context, fileId, request);
            });
#else
            // Fallback to ThreadPool for .NET 2.0 compatibility
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                NotifyLoop(completionFilter, watchTree, outputBufferSize, onNotifyChangeCompleted, context, fileId, request);
            });
#endif
            // Return STATUS_PENDING to indicate asynchronous monitoring has started
            return NTStatus.STATUS_PENDING;
        }

        /// <summary>
        /// Maintains a continuous monitoring loop for directory changes by reissuing ChangeNotify requests.
        /// </summary>
        /// <remarks>
        /// The loop runs until cancellation is requested (via cancellation token or flag) or a fatal error occurs.
        ///
        /// The SMB protocol requires a "one-shot" model: each ChangeNotify request is fulfilled once, so this loop
        /// reissues a new request after each response to maintain monitoring. The request is sent asynchronously
        /// via <c>TrySendCommand</c>, and the thread blocks in <c>WaitForCommand</c> until a response (or timeout) is received.
        ///
        /// Upon receiving a valid response, the change notification is forwarded to the client via the
        /// <c>NotifyChangeEvent</c> event. If the response indicates an error (e.g., <c>STATUS_NOTIFY_CLEANUP</c> or
        /// <c>STATUS_INVALID_HANDLE</c>), the loop terminates and the error is reported via the event.
        ///
        /// Cancellation is handled via platform-specific mechanisms:
        /// - .NET 4.0+/NetStandard 2.0: Uses a <c>CancellationTokenSource</c> (<c>_cancelNotify</c>).
        /// - .NET 2.0: Uses a manual <c>_isCancelled</c> flag (less robust, but compatible).
        /// </remarks>
        /// <param name="completionFilter">
        /// Bitmask specifying the types of changes to monitor (e.g., <c>FILE_NOTIFY_CHANGE_FILE_NAME</c>).
        /// </param>
        /// <param name="watchTree">
        /// True if monitoring subdirectories recursively.
        /// </param>
        /// <param name="outputBufferSize">
        /// Size of the buffer for change notifications (must match initial request).
        /// </param>
        /// <param name="onNotifyChangeCompleted">
        /// Callback to invoke on change notifications or errors (not used here; legacy parameter?).
        /// </param>
        /// <param name="context">
        /// User-defined context passed to the <c>NotifyChangeEvent</c> callback.
        /// </param>
        /// <param name="fileId">
        /// File handle to the monitored directory.
        /// </param>
        /// <param name="request">
        /// The initial <c>ChangeNotifyRequest</c> object (subsequent requests are recreated in the loop).
        /// </param>
        private void NotifyLoop(NotifyChangeFilter completionFilter, bool watchTree, int outputBufferSize, OnNotifyChangeCompleted onNotifyChangeCompleted, object context, FileID fileId, ChangeNotifyRequest request)
        {
#if NET40 || NETSTANDARD2_0
            while (!_cancelNotify.Token.IsCancellationRequested)
#else
            while (!_isCancelled)
#endif            
            {
                // Sending the request 
                TrySendCommand(request);

                bool connectionTerminated = false;
                // Waiting for the response. This method is blocking but since this method runs in a separate thread, it is ok.
                SMB2Command response = m_client.WaitForCommand(request.MessageID, out connectionTerminated);

#if NET40 || NETSTANDARD2_0
                // Check cancellation again after unblocking to exit early
                if (_cancelNotify.Token.IsCancellationRequested)
                    break;
#endif
                if (response != null)
                {
                    if (response.Header.Status != NTStatus.STATUS_SUCCESS)
                    {
                        // Report protocol-level errors (e.g., STATUS_NOTIFY_CLEANUP, STATUS_INVALID_HANDLE)
                        NotifyChangeEvent?.Invoke(response.Header.Status, null, context);
                        break;
                    }

                    // Forward the raw change notification response data to the client
                    byte[] buffer = response.GetBytes();
                    NotifyChangeEvent?.Invoke(NTStatus.STATUS_SUCCESS, buffer, context);

                    // Reissue a new ChangeNotifyRequest to maintain monitoring
                    request = new ChangeNotifyRequest
                    {
                        FileId = fileId,
                        CompletionFilter = completionFilter,
                        Flags = watchTree ? ChangeNotifyFlags.WatchTree : ChangeNotifyFlags.None,
                        OutputBufferLength = (uint)outputBufferSize,
                        Reserved = 0 // Must be zero per SMB spec
                    };
                    m_pendingChangeNotifyRequest = request;
                }
                else
                {
                    // Handle timeout or connection termination
                    NTStatus error = connectionTerminated
                        ? NTStatus.STATUS_INVALID_SMB
                        : NTStatus.STATUS_IO_TIMEOUT;

                    NotifyChangeEvent?.Invoke(error, null, context);
                    break;
                }
            }
        }

        public NTStatus Cancel(object ioRequest)
        {
            throw new NotImplementedException();
        }

        public NTStatus DeviceIOControl(object handle, uint ctlCode, byte[] input, out byte[] output, int maxOutputLength)
        {
            output = null;
            IOCtlRequest request = new IOCtlRequest();
            request.Header.CreditCharge = (ushort)Math.Ceiling((double)maxOutputLength / BytesPerCredit);
            request.CtlCode = ctlCode;
            request.IsFSCtl = true;
            request.FileId = (FileID)handle;
            request.Input = input;
            request.MaxOutputResponse = (uint)maxOutputLength;
            TrySendCommand(request);
            SMB2Command response = m_client.WaitForCommand(request.MessageID, out bool connectionTerminated);
            if (response != null)
            {
                if ((response.Header.Status == NTStatus.STATUS_SUCCESS || response.Header.Status == NTStatus.STATUS_BUFFER_OVERFLOW) && response is IOCtlResponse)
                {
                    output = ((IOCtlResponse)response).Output;
                }
                return response.Header.Status;
            }

            return connectionTerminated ? NTStatus.STATUS_INVALID_SMB : NTStatus.STATUS_IO_TIMEOUT;
        }

        public NTStatus Disconnect()
        {
            TreeDisconnectRequest request = new TreeDisconnectRequest();
            TrySendCommand(request);
            SMB2Command response = m_client.WaitForCommand(request.MessageID);
            if (response != null)
            {
                return response.Header.Status;
            }

            return NTStatus.STATUS_INVALID_SMB;
        }

        private void TrySendCommand(SMB2Command request)
        {
            request.Header.TreeID = m_treeID;
            if (!m_client.IsConnected)
            {
                throw new InvalidOperationException("The client is no longer connected");
            }
            m_client.TrySendCommand(request, m_encryptShareData);
        }

        public uint MaxReadSize
        {
            get
            {
                return m_client.MaxReadSize;
            }
        }

        public uint MaxWriteSize
        {
            get
            {
                return m_client.MaxWriteSize;
            }
        }

        private static FileStatus ToFileStatus(CreateAction createAction)
        {
            switch (createAction)
            {
                case CreateAction.FILE_SUPERSEDED:
                    return FileStatus.FILE_SUPERSEDED;
                case CreateAction.FILE_OPENED:
                    return FileStatus.FILE_OPENED;
                case CreateAction.FILE_CREATED:
                    return FileStatus.FILE_CREATED;
                case CreateAction.FILE_OVERWRITTEN:
                    return FileStatus.FILE_OVERWRITTEN;
                default:
                    return FileStatus.FILE_OPENED;
            }
        }
    }
}
