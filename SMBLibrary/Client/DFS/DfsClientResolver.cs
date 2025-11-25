using System;
using System.Collections.Generic;
using SMBLibrary;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Default DFS client resolver implementation for vNext-DFS.
    /// For now, it only handles the DFS-disabled case and returns NotApplicable.
    /// </summary>
    public class DfsClientResolver : IDfsClientResolver
    {
        private readonly IDfsReferralTransport _transport;
        private readonly Dictionary<string, CachedReferral> _cache;

        private class CachedReferral
        {
            public string ResolvedPath;
            public DateTime ExpirationUtc;
        }

        public DfsClientResolver()
        {
            _transport = null;
            _cache = new Dictionary<string, CachedReferral>(StringComparer.OrdinalIgnoreCase);
        }

        public DfsClientResolver(IDfsReferralTransport transport)
        {
            _transport = transport;
            _cache = new Dictionary<string, CachedReferral>(StringComparer.OrdinalIgnoreCase);
        }

        public DfsResolutionResult Resolve(DfsClientOptions options, string originalPath)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (!options.Enabled)
            {
                return CreateResult(originalPath, DfsResolutionStatus.NotApplicable);
            }

            if (_transport == null)
            {
                return CreateResult(originalPath, DfsResolutionStatus.Error);
            }

            DfsResolutionResult cachedResult = TryGetFromCache(originalPath);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            return ResolveViaTransport(originalPath);
        }

        private DfsResolutionResult TryGetFromCache(string originalPath)
        {
            if (String.IsNullOrEmpty(originalPath))
            {
                return null;
            }

            CachedReferral cached;
            if (!_cache.TryGetValue(originalPath, out cached))
            {
                return null;
            }

            if (cached.ExpirationUtc < DateTime.UtcNow)
            {
                _cache.Remove(originalPath);
                return null;
            }

            return CreateResult(originalPath, DfsResolutionStatus.Success, cached.ResolvedPath);
        }

        private DfsResolutionResult ResolveViaTransport(string originalPath)
        {
            byte[] buffer;
            uint outputCount;
            NTStatus status = _transport.TryGetReferrals(null, originalPath, 0, out buffer, out outputCount);

            if (status == NTStatus.STATUS_FS_DRIVER_REQUIRED)
            {
                return CreateResult(originalPath, DfsResolutionStatus.NotApplicable);
            }

            if (status != NTStatus.STATUS_SUCCESS || buffer == null)
            {
                return CreateResult(originalPath, DfsResolutionStatus.Error);
            }

            return ParseAndCacheResponse(originalPath, buffer, outputCount);
        }

        private DfsResolutionResult ParseAndCacheResponse(string originalPath, byte[] buffer, uint outputCount)
        {
            byte[] effectiveBuffer = GetEffectiveBuffer(buffer, outputCount);
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(effectiveBuffer);

            if (response.ReferralEntries == null || response.ReferralEntries.Count == 0)
            {
                return CreateResult(originalPath, DfsResolutionStatus.Error);
            }

            string resolvedPath = DfsReferralSelector.SelectResolvedPath(originalPath, response.PathConsumed, response.ReferralEntries.ToArray());
            if (String.IsNullOrEmpty(resolvedPath))
            {
                return CreateResult(originalPath, DfsResolutionStatus.Error);
            }

            CacheResult(originalPath, resolvedPath, response);
            return CreateResult(originalPath, DfsResolutionStatus.Success, resolvedPath);
        }

        private byte[] GetEffectiveBuffer(byte[] buffer, uint outputCount)
        {
            if (outputCount > 0 && outputCount < (uint)buffer.Length)
            {
                byte[] effectiveBuffer = new byte[outputCount];
                Array.Copy(buffer, effectiveBuffer, (int)outputCount);
                return effectiveBuffer;
            }
            return buffer;
        }

        private void CacheResult(string originalPath, string resolvedPath, ResponseGetDfsReferral response)
        {
            if (String.IsNullOrEmpty(originalPath))
            {
                return;
            }

            uint ttlSeconds = 0;
            DfsReferralEntryV1 firstV1 = response.ReferralEntries[0] as DfsReferralEntryV1;
            if (firstV1 != null)
            {
                ttlSeconds = firstV1.TimeToLive;
            }

            if (ttlSeconds > 0)
            {
                CachedReferral cached = new CachedReferral();
                cached.ResolvedPath = resolvedPath;
                cached.ExpirationUtc = DateTime.UtcNow.AddSeconds(ttlSeconds);
                _cache[originalPath] = cached;
            }
        }

        private DfsResolutionResult CreateResult(string originalPath, DfsResolutionStatus status)
        {
            return CreateResult(originalPath, status, originalPath);
        }

        private DfsResolutionResult CreateResult(string originalPath, DfsResolutionStatus status, string resolvedPath)
        {
            DfsResolutionResult result = new DfsResolutionResult();
            result.OriginalPath = originalPath;
            result.Status = status;
            result.ResolvedPath = resolvedPath;
            return result;
        }
    }
}
