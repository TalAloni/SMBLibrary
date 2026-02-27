using System;
using SMBLibrary;
using SMBLibrary.DFS;

namespace SMBLibrary.Client.DFS
{
    public static class DfsReferralSelector
    {
        public static string SelectResolvedPath(string originalPath, ushort pathConsumed, DfsReferralEntry entry)
        {
            if (originalPath == null)
            {
                throw new ArgumentNullException("originalPath");
            }

            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            // Get the network address based on entry version
            string networkAddress = GetNetworkAddress(entry);
            if (string.IsNullOrEmpty(networkAddress))
            {
                return null;
            }

            int consumedCharacters = pathConsumed / 2;
            if (consumedCharacters < 0 || consumedCharacters > originalPath.Length)
            {
                return null;
            }

            string suffix = originalPath.Substring(consumedCharacters);
            return networkAddress + suffix;
        }

        private static string GetNetworkAddress(DfsReferralEntry entry)
        {
            // V3/V4 have NetworkAddress
            DfsReferralEntryV3 v3 = entry as DfsReferralEntryV3;
            if (v3 != null)
            {
                return v3.NetworkAddress;
            }

            // V2 has NetworkAddress
            DfsReferralEntryV2 v2 = entry as DfsReferralEntryV2;
            if (v2 != null)
            {
                return v2.NetworkAddress;
            }

            // V1 uses ShareName
            DfsReferralEntryV1 v1 = entry as DfsReferralEntryV1;
            if (v1 != null)
            {
                return v1.ShareName;
            }

            return null;
        }

        public static string SelectResolvedPath(string originalPath, ushort pathConsumed, DfsReferralEntry[] entries)
        {
            if (originalPath == null)
            {
                throw new ArgumentNullException("originalPath");
            }

            if (entries == null)
            {
                throw new ArgumentNullException("entries");
            }

            for (int index = 0; index < entries.Length; index++)
            {
                DfsReferralEntry entry = entries[index];
                if (entry == null)
                {
                    continue;
                }

                string candidate = SelectResolvedPath(originalPath, pathConsumed, entry);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
