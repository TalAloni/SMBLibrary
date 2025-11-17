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

            // DFS disabled: do not attempt resolution; return original path as-is.
            if (!options.Enabled)
            {
                DfsResolutionResult result = new DfsResolutionResult();
                result.Status = DfsResolutionStatus.NotApplicable;
                result.ResolvedPath = originalPath;
                result.OriginalPath = originalPath;
                return result;
            }

            if (_transport == null)
            {
                DfsResolutionResult notImplemented = new DfsResolutionResult();
                notImplemented.Status = DfsResolutionStatus.Error;
                notImplemented.ResolvedPath = originalPath;
                notImplemented.OriginalPath = originalPath;
                return notImplemented;
            }

            // Check cache before calling the transport.
            if (!String.IsNullOrEmpty(originalPath))
            {
                CachedReferral cached;
                if (_cache.TryGetValue(originalPath, out cached))
                {
                    if (cached.ExpirationUtc >= DateTime.UtcNow)
                    {
                        DfsResolutionResult cachedResult = new DfsResolutionResult();
                        cachedResult.Status = DfsResolutionStatus.Success;
                        cachedResult.ResolvedPath = cached.ResolvedPath;
                        cachedResult.OriginalPath = originalPath;
                        return cachedResult;
                    }
                    else
                    {
                        _cache.Remove(originalPath);
                    }
                }
            }

            byte[] buffer;
            uint outputCount;
            NTStatus status = _transport.TryGetReferrals(null, originalPath, 0, out buffer, out outputCount);

            DfsResolutionResult resultWithTransport = new DfsResolutionResult();
            resultWithTransport.OriginalPath = originalPath;

            if (status == NTStatus.STATUS_FS_DRIVER_REQUIRED)
            {
                resultWithTransport.Status = DfsResolutionStatus.NotApplicable;
                resultWithTransport.ResolvedPath = originalPath;
                return resultWithTransport;
            }

            if (status == NTStatus.STATUS_SUCCESS)
            {
                if (buffer == null)
                {
                    resultWithTransport.Status = DfsResolutionStatus.Error;
                    resultWithTransport.ResolvedPath = originalPath;
                    return resultWithTransport;
                }

                byte[] effectiveBuffer = buffer;
                if (outputCount > 0 && outputCount <= (uint)buffer.Length)
                {
                    if (outputCount != (uint)buffer.Length)
                    {
                        effectiveBuffer = new byte[outputCount];
                        Array.Copy(buffer, effectiveBuffer, (int)outputCount);
                    }
                }

                ResponseGetDfsReferral response = new ResponseGetDfsReferral(effectiveBuffer);

                if (response.ReferralEntries == null || response.ReferralEntries.Count == 0)
                {
                    resultWithTransport.Status = DfsResolutionStatus.Error;
                    resultWithTransport.ResolvedPath = originalPath;
                    return resultWithTransport;
                }

                string resolvedPath = DfsReferralSelector.SelectResolvedPath(originalPath, response.PathConsumed, response.ReferralEntries.ToArray());
                if (String.IsNullOrEmpty(resolvedPath))
                {
                    resultWithTransport.Status = DfsResolutionStatus.Error;
                    resultWithTransport.ResolvedPath = originalPath;
                    return resultWithTransport;
                }

                resultWithTransport.Status = DfsResolutionStatus.Success;
                resultWithTransport.ResolvedPath = resolvedPath;

                // Cache the result based on v1 TimeToLive when available.
                if (!String.IsNullOrEmpty(originalPath))
                {
                    uint ttlSeconds = 0;
                    DfsReferralEntryV1 firstV1 = response.ReferralEntries[0] as DfsReferralEntryV1;
                    if (firstV1 != null)
                    {
                        ttlSeconds = firstV1.TimeToLive;
                    }

                    if (ttlSeconds > 0)
                    {
                        CachedReferral cachedReferral = new CachedReferral();
                        cachedReferral.ResolvedPath = resolvedPath;
                        cachedReferral.ExpirationUtc = DateTime.UtcNow.AddSeconds(ttlSeconds);
                        _cache[originalPath] = cachedReferral;
                    }
                }

                return resultWithTransport;
            }

            resultWithTransport.Status = DfsResolutionStatus.Error;
            resultWithTransport.ResolvedPath = originalPath;
            return resultWithTransport;
        }
    }
}
