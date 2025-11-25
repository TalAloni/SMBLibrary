using System;
using SMBLibrary;

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

            DfsReferralEntryV1 v1 = entry as DfsReferralEntryV1;
            if (v1 == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(v1.NetworkAddress))
            {
                return null;
            }

            int consumedCharacters = pathConsumed / 2;
            if (consumedCharacters < 0 || consumedCharacters > originalPath.Length)
            {
                return null;
            }

            string suffix = originalPath.Substring(consumedCharacters);
            return v1.NetworkAddress + suffix;
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
