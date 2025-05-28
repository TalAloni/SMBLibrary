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

        /**
         * NotifyChangeEventHandler
         *
         * Delegate for handling directory change notifications from the server.
         * 
         * @param status NTStatus.SUCCESS if changes were detected; an error code (e.g., STATUS_NOTIFY_CLEANUP) if the server terminated the request.
         * @param buffer Raw change notification response data (null if an error occurred).
         * @param context User-defined context passed during NotifyChange registration.
         */
        public delegate void NotifyChangeEventHandler(NTStatus status, byte[] buffer, object context);
        /**
         * NotifyChangeEvent
         *
         * Event raised when the server responds to a ChangeNotifyRequest (e.g., directory changes detected or an error occurred).
         * Clients should subscribe to this event to receive notifications. Multiple subscribers are supported.
         */
        public event NotifyChangeEventHandler NotifyChangeEvent;
        /**
         * Cancellation Mechanism
         *
         * Platform-specific implementation for stopping the NotifyLoop:
         * 
         * - .NET 4.0+/NetStandard 2.0: Uses CancellationTokenSource for thread-safe, cooperative cancellation.
         *   This is the preferred method as it integrates with async/await and ensures clean termination.
         * 
         * - .NET 2.0: Falls back to a volatile boolean flag for cancellation. This is a best-effort approach
         *   due to limited synchronization primitives in .NET 2.0. It may not guarantee immediate termination.
         */
#if NET40 || NETSTANDARD2_0
        private CancellationTokenSource _cancelNotify = new CancellationTokenSource();
#else
        private volatile bool _isCancelled = false;
#endif
        /**
         * m_pendingChangeNotifyRequest
         *
         * Tracks the currently active ChangeNotifyRequest to enable server-side cancellation.
         * 
         * The Cancel() method uses this field to:
         * - Retrieve the MessageID of the pending request for the SMB2 CANCEL command.
         * - Ensure the server correctly identifies and terminates the request.
         */
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

        /**
         * NotifyChange
         *
         * Initiates monitoring of a directory for file system changes using the SMB protocol.
         * 
         * To monitor a directory, a ChangeNotifyRequest must be sent with all fields explicitly set.
         * The `Reserved` field must be set to 0, as some SMB servers validate this field and return
         * STATUS_INVALID_PARAMETER (or similar errors) if it is non-zero, interpreting it as a malformed request.
         * 
         * The request is sent asynchronously in a loop to maintain continuous monitoring. SMB requires
         * one request per notification (one-shot model), so the loop reissues the request after each response
         * until the user cancels monitoring via CancelOperation or closes the file handle.
         * 
         * @param ioRequest (out) The SMB request object
         * @param handle The file handle (FileID) of the directory to monitor.
         * @param completionFilter Bitmask specifying the types of changes to monitor (e.g., file creation, modification).
         * @param watchTree True to monitor subdirectories recursively; false for the directory only.
         * @param outputBufferSize Size of the buffer to store change notifications. This should be a power of two
         *                         (e.g., 1024, 4096) to ensure compatibility with SMB server implementations.
         * @param onNotifyChangeCompleted Callback invoked when a change is detected or an error occurs.
         * @param context User-defined context passed to the callback.
         * 
         * @return NTStatus.STATUS_PENDING if monitoring started successfully; other status codes indicate failure.
         */
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

            // TODO: lanmanserver is not supporting async. What about Shieldor? 
            // --> STATUS_NETWORK_NAME_DELETED as response, if not supported 
            //request.Header.Flags |= SMB2PacketHeaderFlags.AsyncCommand;
            //request.Header.AsyncID = m_client.GetNextAsyncId();

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

        /**
         * NotifyLoop
         *
         * Maintains a continuous monitoring loop for directory changes by reissuing ChangeNotify requests.
         * The loop runs until cancellation is requested (via cancellation token or flag) or a fatal error occurs.
         * 
         * The SMB protocol requires a "one-shot" model: each ChangeNotify request is fulfilled once, so this loop
         * reissues a new request after each response to maintain monitoring. The request is sent asynchronously
         * via TrySendCommand, and the thread blocks in WaitForCommand until a response (or timeout) is received.
         * 
         * Upon receiving a valid response, the change notification is forwarded to the client via the
         * NotifyChangeEvent event. If the response indicates an error (e.g., STATUS_NOTIFY_CLEANUP or
         * STATUS_INVALID_HANDLE), the loop terminates and the error is reported via the event.
         * 
         * Cancellation is handled via platform-specific mechanisms:
         * - .NET 4.0+/NetStandard 2.0: Uses a CancellationTokenSource (_cancelNotify).
         * - .NET 2.0: Uses a manual _isCancelled flag (less robust, but compatible).
         * 
         * @param completionFilter Bitmask specifying the types of changes to monitor (e.g., FILE_NOTIFY_CHANGE_FILE_NAME).
         * @param watchTree True if monitoring subdirectories recursively.
         * @param outputBufferSize Size of the buffer for change notifications (must match initial request).
         * @param onNotifyChangeCompleted Callback to invoke on change notifications or errors (not used here; legacy parameter?).
         * @param context User-defined context passed to the NotifyChangeEvent callback.
         * @param fileId File handle to the monitored directory.
         * @param request The initial ChangeNotifyRequest object (subsequent requests are recreated in the loop).
         */
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
                    // TODO see NotifyChange()
                    //request.Header.Flags |= SMB2PacketHeaderFlags.AsyncCommand;
                    //request.Header.AsyncID = m_client.GetNextAsyncId();
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

        /**
         * Cancel
         *
         * Stops the directory monitoring loop initiated by NotifyChange and sends an SMB2 CANCEL request to the server
         * to terminate the pending ChangeNotify operation. This method ensures both local loop termination and
         * server-side cancellation.
         * 
         * Critical Requirements:
         * - The `MessageID` in the CANCEL request **must match** the `MessageID` of the original ChangeNotifyRequest.
         *   This ensures the server correctly identifies and cancels the specific request.
         * - The `TreeID` and `SessionID` must also match the original request to ensure proper routing.
         * 
         * Platform-Specific Behavior:
         * - .NET 4.0+/NetStandard 2.0: Uses a CancellationToken (`_cancelNotify`) to signal loop termination.
         * - .NET 2.0: Uses a manual `_isCancelled` flag (less robust but compatible).
         * 
         * Server Compatibility Notes:
         * - Some servers may not support CANCEL requests (e.g., older SMB versions).
         * - Servers may ignore the CANCEL request if the ChangeNotify operation has already completed.
         * - Even if cancellation fails, the local loop will terminate to prevent resource leaks.
         * 
         * @param ioRequest The ChangeNotifyRequest object returned by NotifyChange. This ensures the correct MessageID is used.
         * @return NTStatus.STATUS_SUCCESS if cancellation was attempted; STATUS_INVALID_HANDLE if ioRequest is invalid.
         */
        public NTStatus Cancel(object ioRequest)
        {
            /* Check, if the user sends correct request. 
               Note that, this request will not be used in CancelRequest, since this the request created by ChangeNotify
               call and not by NotifyLoop. So, this check could also be removed. */
            if (!(ioRequest is ChangeNotifyRequest request))
            {
                return NTStatus.STATUS_INVALID_HANDLE;
            }

#if NET40 || NETSTANDARD2_0
            _cancelNotify.Cancel(); // Signal cancellation to exit NotifyLoop
#else
            _isCancelled = true; // Manual flag for .NET 2.0
#endif  

            // Construct and send SMB2 CANCEL request to server
            CancelRequest cancelCommand = new CancelRequest
            {
                Header = new SMB2Header(SMB2CommandName.Cancel)
                {
                    // Match the original ChangeNotifyRequest's MessageID, TreeID, and SessionID
                    MessageID = m_pendingChangeNotifyRequest.Header.MessageID,
                    TreeID = m_pendingChangeNotifyRequest.Header.TreeID,
                    SessionID = m_pendingChangeNotifyRequest.Header.SessionID
                },
                Reserved = 0 // Must be zero per SMB2 protocol
            };
            TrySendCommand(cancelCommand);

            return NTStatus.STATUS_SUCCESS;
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
