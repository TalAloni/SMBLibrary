using System;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Implements the 14-step DFS path resolution algorithm per MS-DFSC section 3.1.4.2.
    /// Uses DfsPath for path manipulation, ReferralCache and DomainCache for caching.
    /// </summary>
    public class DfsPathResolver
    {
        private readonly ReferralCache _referralCache;
        private readonly DomainCache _domainCache;
        private readonly IDfsReferralTransport _transport;

        /// <summary>
        /// Raised when DFS resolution starts.
        /// </summary>
        public event EventHandler<DfsResolutionStartedEventArgs> ResolutionStarted;

        /// <summary>
        /// Raised when a DFS referral request is issued.
        /// </summary>
        public event EventHandler<DfsReferralRequestedEventArgs> ReferralRequested;

        /// <summary>
        /// Raised when a DFS referral response is received.
        /// </summary>
        public event EventHandler<DfsReferralReceivedEventArgs> ReferralReceived;

        /// <summary>
        /// Raised when DFS resolution completes.
        /// </summary>
        public event EventHandler<DfsResolutionCompletedEventArgs> ResolutionCompleted;

        /// <summary>
        /// Initializes a new instance of <see cref="DfsPathResolver"/> with no caches or transport.
        /// </summary>
        public DfsPathResolver()
            : this(null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DfsPathResolver"/>.
        /// </summary>
        /// <param name="referralCache">Optional referral cache. If null, a new empty cache is created.</param>
        /// <param name="domainCache">Optional domain cache. If null, a new empty cache is created.</param>
        /// <param name="transport">Optional DFS referral transport. If null, resolution will return
        /// <see cref="DfsResolutionStatus.NotApplicable"/> for paths not found in cache.</param>
        public DfsPathResolver(ReferralCache referralCache, DomainCache domainCache, IDfsReferralTransport transport)
        {
            _referralCache = referralCache ?? new ReferralCache();
            _domainCache = domainCache ?? new DomainCache();
            _transport = transport;
        }

        /// <summary>
        /// Resolves a UNC path using the 14-step DFS algorithm.
        /// </summary>
        /// <param name="options">DFS client options.</param>
        /// <param name="originalPath">The UNC path to resolve.</param>
        /// <returns>The resolution result.</returns>
        public DfsResolutionResult Resolve(DfsClientOptions options, string originalPath)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            // Raise started event
            OnResolutionStarted(new DfsResolutionStartedEventArgs(originalPath));

            DfsResolutionResult result = new DfsResolutionResult();
            result.OriginalPath = originalPath;

            // DFS disabled: return original path as-is
            if (!options.Enabled)
            {
                result.Status = DfsResolutionStatus.NotApplicable;
                result.ResolvedPath = originalPath;
                result.IsDfsPath = false;
                OnResolutionCompleted(new DfsResolutionCompletedEventArgs(originalPath, originalPath, NTStatus.STATUS_SUCCESS, false));
                return result;
            }

            // Parse the path
            DfsPath dfsPath;
            try
            {
                dfsPath = new DfsPath(originalPath);
            }
            catch (ArgumentException)
            {
                // Invalid path format
                result.Status = DfsResolutionStatus.NotApplicable;
                result.ResolvedPath = originalPath;
                result.IsDfsPath = false;
                OnResolutionCompleted(new DfsResolutionCompletedEventArgs(originalPath, originalPath, NTStatus.STATUS_SUCCESS, false));
                return result;
            }

            // Step 1: Single component or IPC$ check
            if (dfsPath.HasOnlyOneComponent || dfsPath.IsIpc)
            {
                result.Status = DfsResolutionStatus.NotApplicable;
                result.ResolvedPath = originalPath;
                result.IsDfsPath = false;
                OnResolutionCompleted(new DfsResolutionCompletedEventArgs(originalPath, originalPath, NTStatus.STATUS_SUCCESS, false));
                return result;
            }

            // Step 2: Check ReferralCache for matching entry
            ReferralCacheEntry cachedEntry = _referralCache.Lookup(originalPath);
            if (cachedEntry != null)
            {
                // Steps 3-4: Cache hit - ROOT or LINK
                string resolvedPath = ResolveFromCacheEntry(originalPath, dfsPath, cachedEntry);
                if (resolvedPath != null)
                {
                    result.Status = DfsResolutionStatus.Success;
                    result.ResolvedPath = resolvedPath;
                    result.IsDfsPath = true;
                    OnResolutionCompleted(new DfsResolutionCompletedEventArgs(originalPath, resolvedPath, NTStatus.STATUS_SUCCESS, true));
                    return result;
                }
            }

            // Steps 5-7: Cache miss - need to issue referral request
            if (_transport == null)
            {
                // No transport available
                result.Status = DfsResolutionStatus.NotApplicable;
                result.ResolvedPath = originalPath;
                result.IsDfsPath = false;
                OnResolutionCompleted(new DfsResolutionCompletedEventArgs(originalPath, originalPath, NTStatus.STATUS_SUCCESS, false));
                return result;
            }

            // Issue referral request
            string serverName = dfsPath.ServerName;
            OnReferralRequested(new DfsReferralRequestedEventArgs(originalPath, DfsRequestType.RootReferral, serverName));

            byte[] buffer;
            uint outputCount;
            NTStatus status = _transport.TryGetReferrals(serverName, originalPath, 0, out buffer, out outputCount);

            // Step 12: Server not DFS-capable
            if (status == NTStatus.STATUS_FS_DRIVER_REQUIRED)
            {
                OnReferralReceived(new DfsReferralReceivedEventArgs(originalPath, status, 0, 0));
                result.Status = DfsResolutionStatus.NotApplicable;
                result.ResolvedPath = originalPath;
                result.IsDfsPath = false;
                OnResolutionCompleted(new DfsResolutionCompletedEventArgs(originalPath, originalPath, status, false));
                return result;
            }

            // Handle successful referral
            if (status == NTStatus.STATUS_SUCCESS && buffer != null && buffer.Length > 0)
            {
                try
                {
                    byte[] effectiveBuffer = buffer;
                    if (outputCount > 0 && outputCount <= (uint)buffer.Length && outputCount != (uint)buffer.Length)
                    {
                        effectiveBuffer = new byte[outputCount];
                        Array.Copy(buffer, effectiveBuffer, (int)outputCount);
                    }

                    ResponseGetDfsReferral response = new ResponseGetDfsReferral(effectiveBuffer);
                    int referralCount = response.ReferralEntries != null ? response.ReferralEntries.Count : 0;
                    int ttlSeconds = 0;
                    if (referralCount > 0)
                    {
                        DfsReferralEntryV1 firstV1 = response.ReferralEntries[0] as DfsReferralEntryV1;
                        if (firstV1 != null)
                        {
                            ttlSeconds = (int)firstV1.TimeToLive;
                        }
                    }

                    OnReferralReceived(new DfsReferralReceivedEventArgs(originalPath, status, referralCount, ttlSeconds));

                    if (referralCount > 0)
                    {
                        string resolvedPath = DfsReferralSelector.SelectResolvedPath(originalPath, response.PathConsumed, response.ReferralEntries.ToArray());
                        if (!String.IsNullOrEmpty(resolvedPath))
                        {
                            // Cache the result
                            if (ttlSeconds > 0)
                            {
                                ReferralCacheEntry newEntry = new ReferralCacheEntry(originalPath);
                                newEntry.TtlSeconds = (uint)ttlSeconds;
                                newEntry.ExpiresUtc = DateTime.UtcNow.AddSeconds(ttlSeconds);
                                DfsReferralEntryV1 firstEntry = response.ReferralEntries[0] as DfsReferralEntryV1;
                                if (firstEntry != null)
                                {
                                    newEntry.TargetList.Add(new TargetSetEntry(firstEntry.NetworkAddress));
                                }
                                _referralCache.Add(newEntry);
                            }

                            result.Status = DfsResolutionStatus.Success;
                            result.ResolvedPath = resolvedPath;
                            result.IsDfsPath = true;
                            OnResolutionCompleted(new DfsResolutionCompletedEventArgs(originalPath, resolvedPath, status, true));
                            return result;
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // Malformed referral response
                }
            }

            // Steps 13-14: Error cases
            OnReferralReceived(new DfsReferralReceivedEventArgs(originalPath, status, 0, 0));
            result.Status = DfsResolutionStatus.Error;
            result.ResolvedPath = originalPath;
            result.IsDfsPath = false;
            OnResolutionCompleted(new DfsResolutionCompletedEventArgs(originalPath, originalPath, status, false));
            return result;
        }

        /// <summary>
        /// Resolves a path using a cached referral entry.
        /// </summary>
        private string ResolveFromCacheEntry(string originalPath, DfsPath dfsPath, ReferralCacheEntry entry)
        {
            TargetSetEntry target = entry.GetTargetHint();
            if (target == null)
            {
                return null;
            }

            // Replace the DFS prefix with the target path
            try
            {
                DfsPath targetDfsPath = new DfsPath(target.TargetPath);
                DfsPath result = dfsPath.ReplacePrefix(entry.DfsPathPrefix, targetDfsPath);
                return result.ToUncPath();
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        protected virtual void OnResolutionStarted(DfsResolutionStartedEventArgs e)
        {
            EventHandler<DfsResolutionStartedEventArgs> handler = ResolutionStarted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnReferralRequested(DfsReferralRequestedEventArgs e)
        {
            EventHandler<DfsReferralRequestedEventArgs> handler = ReferralRequested;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnReferralReceived(DfsReferralReceivedEventArgs e)
        {
            EventHandler<DfsReferralReceivedEventArgs> handler = ReferralReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnResolutionCompleted(DfsResolutionCompletedEventArgs e)
        {
            EventHandler<DfsResolutionCompletedEventArgs> handler = ResolutionCompleted;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
