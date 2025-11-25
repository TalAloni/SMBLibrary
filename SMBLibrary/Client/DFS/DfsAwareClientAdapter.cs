using System;
using System.Collections.Generic;
using SMBLibrary;
using SMBLibrary.Client;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// DFS-aware ISMBFileStore adapter that composes an ISMBFileStore with an IDfsClientResolver.
    /// Implements reactive DFS resolution by catching STATUS_PATH_NOT_COVERED and retrying
    /// with resolved paths per MS-DFSC section 3.1.5.1.
    /// </summary>
    public class DfsAwareClientAdapter : ISMBFileStore
    {
        private readonly ISMBFileStore _inner;
        private readonly IDfsClientResolver _resolver;
        private readonly DfsClientOptions _options;
        private readonly int _maxRetries;

        /// <summary>
        /// Maximum number of DFS resolution retries (to prevent infinite loops on interlinks).
        /// </summary>
        private const int DefaultMaxRetries = 3;

        public DfsAwareClientAdapter(ISMBFileStore inner, IDfsClientResolver resolver, DfsClientOptions options)
            : this(inner, resolver, options, DefaultMaxRetries)
        {
        }

        public DfsAwareClientAdapter(ISMBFileStore inner, IDfsClientResolver resolver, DfsClientOptions options, int maxRetries)
        {
            if (inner == null)
            {
                throw new ArgumentNullException("inner");
            }

            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            _inner = inner;
            _resolver = resolver;
            _options = options;
            _maxRetries = maxRetries > 0 ? maxRetries : DefaultMaxRetries;
        }

        /// <summary>
        /// Resolves a path using the DFS resolver. Returns the original path if resolution fails.
        /// </summary>
        private string ResolvePath(string path)
        {
            if (path == null)
            {
                return null;
            }

            DfsResolutionResult result = _resolver.Resolve(_options, path);
            if (result == null)
            {
                return path;
            }

            if (result.Status == DfsResolutionStatus.Success && !String.IsNullOrEmpty(result.ResolvedPath))
            {
                return result.ResolvedPath;
            }

            if (!String.IsNullOrEmpty(result.OriginalPath))
            {
                return result.OriginalPath;
            }

            if (!String.IsNullOrEmpty(result.ResolvedPath))
            {
                return result.ResolvedPath;
            }

            return path;
        }

        /// <summary>
        /// Attempts to resolve a path after receiving STATUS_PATH_NOT_COVERED.
        /// Forces a fresh DFS referral lookup, bypassing any cached entries.
        /// </summary>
        private string ResolvePathAfterNotCovered(string originalPath)
        {
            // Force resolution - the path wasn't covered by current target
            DfsResolutionResult result = _resolver.Resolve(_options, originalPath);
            if (result != null && result.Status == DfsResolutionStatus.Success && !String.IsNullOrEmpty(result.ResolvedPath))
            {
                return result.ResolvedPath;
            }

            // If resolution fails, return null to indicate no retry should occur
            return null;
        }

        /// <summary>
        /// Checks if the status indicates a DFS redirect is needed.
        /// </summary>
        private static bool IsDfsRedirectStatus(NTStatus status)
        {
            return status == NTStatus.STATUS_PATH_NOT_COVERED;
        }

        public NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, FileAttributes fileAttributes, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext)
        {
            string effectivePath = ResolvePath(path);
            int retryCount = 0;

            while (true)
            {
                NTStatus status = _inner.CreateFile(out handle, out fileStatus, effectivePath, desiredAccess, fileAttributes, shareAccess, createDisposition, createOptions, securityContext);

                // If successful or not a DFS redirect, return immediately
                if (status == NTStatus.STATUS_SUCCESS || !IsDfsRedirectStatus(status))
                {
                    return status;
                }

                // STATUS_PATH_NOT_COVERED: attempt DFS resolution and retry
                retryCount++;
                if (retryCount > _maxRetries)
                {
                    // Max retries exceeded - return the error
                    return status;
                }

                string resolvedPath = ResolvePathAfterNotCovered(path);
                if (resolvedPath == null || String.Equals(resolvedPath, effectivePath, StringComparison.OrdinalIgnoreCase))
                {
                    // Resolution failed or returned same path - no point retrying
                    return status;
                }

                // Retry with resolved path
                effectivePath = resolvedPath;
            }
        }

        public NTStatus CloseFile(object handle)
        {
            return _inner.CloseFile(handle);
        }

        public NTStatus ReadFile(out byte[] data, object handle, long offset, int maxCount)
        {
            return _inner.ReadFile(out data, handle, offset, maxCount);
        }

        public NTStatus WriteFile(out int numberOfBytesWritten, object handle, long offset, byte[] data)
        {
            return _inner.WriteFile(out numberOfBytesWritten, handle, offset, data);
        }

        public NTStatus FlushFileBuffers(object handle)
        {
            return _inner.FlushFileBuffers(handle);
        }

        public NTStatus LockFile(object handle, long byteOffset, long length, bool exclusiveLock)
        {
            return _inner.LockFile(handle, byteOffset, length, exclusiveLock);
        }

        public NTStatus UnlockFile(object handle, long byteOffset, long length)
        {
            return _inner.UnlockFile(handle, byteOffset, length);
        }

        public NTStatus QueryDirectory(out List<QueryDirectoryFileInformation> result, object handle, string fileName, FileInformationClass informationClass)
        {
            string effectiveFileName = ResolvePath(fileName);
            return _inner.QueryDirectory(out result, handle, effectiveFileName, informationClass);
        }

        public NTStatus GetFileInformation(out FileInformation result, object handle, FileInformationClass informationClass)
        {
            return _inner.GetFileInformation(out result, handle, informationClass);
        }

        public NTStatus SetFileInformation(object handle, FileInformation information)
        {
            return _inner.SetFileInformation(handle, information);
        }

        public NTStatus GetFileSystemInformation(out FileSystemInformation result, FileSystemInformationClass informationClass)
        {
            return _inner.GetFileSystemInformation(out result, informationClass);
        }

        public NTStatus SetFileSystemInformation(FileSystemInformation information)
        {
            return _inner.SetFileSystemInformation(information);
        }

        public NTStatus GetSecurityInformation(out SecurityDescriptor result, object handle, SecurityInformation securityInformation)
        {
            return _inner.GetSecurityInformation(out result, handle, securityInformation);
        }

        public NTStatus SetSecurityInformation(object handle, SecurityInformation securityInformation, SecurityDescriptor securityDescriptor)
        {
            return _inner.SetSecurityInformation(handle, securityInformation, securityDescriptor);
        }

        public NTStatus NotifyChange(out object ioRequest, object handle, NotifyChangeFilter completionFilter, bool watchTree, int outputBufferSize, OnNotifyChangeCompleted onNotifyChangeCompleted, object context)
        {
            return _inner.NotifyChange(out ioRequest, handle, completionFilter, watchTree, outputBufferSize, onNotifyChangeCompleted, context);
        }

        public NTStatus Cancel(object ioRequest)
        {
            return _inner.Cancel(ioRequest);
        }

        public NTStatus DeviceIOControl(object handle, uint ctlCode, byte[] input, out byte[] output, int maxOutputLength)
        {
            return _inner.DeviceIOControl(handle, ctlCode, input, out output, maxOutputLength);
        }

        public NTStatus Disconnect()
        {
            return _inner.Disconnect();
        }

        public uint MaxReadSize
        {
            get { return _inner.MaxReadSize; }
        }

        public uint MaxWriteSize
        {
            get { return _inner.MaxWriteSize; }
        }
    }
}
