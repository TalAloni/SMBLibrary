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
        public DfsResolutionResult Resolve(DfsClientOptions options, string originalPath)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            OnResolutionStarted(new DfsResolutionStartedEventArgs(originalPath));

            if (!options.Enabled)
            {
                return CreateNotApplicableResult(originalPath);
            }

            DfsPath dfsPath = TryParsePath(originalPath);
            if (dfsPath == null || dfsPath.HasOnlyOneComponent || dfsPath.IsIpc)
            {
                return CreateNotApplicableResult(originalPath);
            }

            DfsResolutionResult cachedResult = TryResolveFromCache(originalPath, dfsPath);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            if (_transport == null)
            {
                return CreateNotApplicableResult(originalPath);
            }

            return ResolveViaTransport(originalPath, dfsPath);
        }

        private DfsPath TryParsePath(string originalPath)
        {
            try
            {
                return new DfsPath(originalPath);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private DfsResolutionResult TryResolveFromCache(string originalPath, DfsPath dfsPath)
        {
            ReferralCacheEntry cachedEntry = _referralCache.Lookup(originalPath);
            if (cachedEntry == null)
            {
                return null;
            }

            string resolvedPath = ResolveFromCacheEntry(originalPath, dfsPath, cachedEntry);
            if (resolvedPath == null)
            {
                return null;
            }

            return CreateSuccessResult(originalPath, resolvedPath);
        }

        private DfsResolutionResult ResolveViaTransport(string originalPath, DfsPath dfsPath)
        {
            string serverName = dfsPath.ServerName;
            OnReferralRequested(new DfsReferralRequestedEventArgs(originalPath, DfsRequestType.RootReferral, serverName));

            byte[] buffer;
            uint outputCount;
            NTStatus status = _transport.TryGetReferrals(serverName, originalPath, 0, out buffer, out outputCount);

            if (status == NTStatus.STATUS_FS_DRIVER_REQUIRED)
            {
                return CreateNotApplicableResult(originalPath, status);
            }

            if (status == NTStatus.STATUS_SUCCESS && buffer != null && buffer.Length > 0)
            {
                DfsResolutionResult result = TryParseReferralResponse(originalPath, buffer, outputCount, status);
                if (result != null)
                {
                    return result;
                }
            }

            return CreateErrorResult(originalPath, status);
        }

        private DfsResolutionResult TryParseReferralResponse(string originalPath, byte[] buffer, uint outputCount, NTStatus status)
        {
            try
            {
                byte[] effectiveBuffer = GetEffectiveBuffer(buffer, outputCount);
                ResponseGetDfsReferral response = new ResponseGetDfsReferral(effectiveBuffer);
                
                int referralCount = response.ReferralEntries != null ? response.ReferralEntries.Count : 0;
                int ttlSeconds = GetTtlFromResponse(response);

                OnReferralReceived(new DfsReferralReceivedEventArgs(originalPath, status, referralCount, ttlSeconds));

                if (referralCount > 0)
                {
                    string resolvedPath = DfsReferralSelector.SelectResolvedPath(originalPath, response.PathConsumed, response.ReferralEntries.ToArray());
                    if (!String.IsNullOrEmpty(resolvedPath))
                    {
                        CacheReferralResult(originalPath, response, ttlSeconds);
                        return CreateSuccessResult(originalPath, resolvedPath);
                    }
                }
            }
            catch (ArgumentException)
            {
                // Malformed referral response
            }

            return null;
        }

        private byte[] GetEffectiveBuffer(byte[] buffer, uint outputCount)
        {
            if (outputCount > 0 && outputCount <= (uint)buffer.Length && outputCount != (uint)buffer.Length)
            {
                byte[] effectiveBuffer = new byte[outputCount];
                Array.Copy(buffer, effectiveBuffer, (int)outputCount);
                return effectiveBuffer;
            }
            return buffer;
        }

        private int GetTtlFromResponse(ResponseGetDfsReferral response)
        {
            if (response.ReferralEntries != null && response.ReferralEntries.Count > 0)
            {
                DfsReferralEntryV1 firstV1 = response.ReferralEntries[0] as DfsReferralEntryV1;
                if (firstV1 != null)
                {
                    return (int)firstV1.TimeToLive;
                }
            }
            return 0;
        }

        private void CacheReferralResult(string originalPath, ResponseGetDfsReferral response, int ttlSeconds)
        {
            if (ttlSeconds <= 0)
            {
                return;
            }

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

        private DfsResolutionResult CreateNotApplicableResult(string originalPath)
        {
            return CreateNotApplicableResult(originalPath, NTStatus.STATUS_SUCCESS);
        }

        private DfsResolutionResult CreateNotApplicableResult(string originalPath, NTStatus status)
        {
            OnReferralReceived(new DfsReferralReceivedEventArgs(originalPath, status, 0, 0));
            DfsResolutionResult result = new DfsResolutionResult();
            result.OriginalPath = originalPath;
            result.Status = DfsResolutionStatus.NotApplicable;
            result.ResolvedPath = originalPath;
            result.IsDfsPath = false;
            OnResolutionCompleted(new DfsResolutionCompletedEventArgs(originalPath, originalPath, status, false));
            return result;
        }

        private DfsResolutionResult CreateSuccessResult(string originalPath, string resolvedPath)
        {
            DfsResolutionResult result = new DfsResolutionResult();
            result.OriginalPath = originalPath;
            result.Status = DfsResolutionStatus.Success;
            result.ResolvedPath = resolvedPath;
            result.IsDfsPath = true;
            OnResolutionCompleted(new DfsResolutionCompletedEventArgs(originalPath, resolvedPath, NTStatus.STATUS_SUCCESS, true));
            return result;
        }

        private DfsResolutionResult CreateErrorResult(string originalPath, NTStatus status)
        {
            OnReferralReceived(new DfsReferralReceivedEventArgs(originalPath, status, 0, 0));
            DfsResolutionResult result = new DfsResolutionResult();
            result.OriginalPath = originalPath;
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
