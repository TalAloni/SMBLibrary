using SMBLibrary;

namespace SMBLibrary.Client.DFS
{
    public interface IDfsReferralTransport
    {
        NTStatus TryGetReferrals(string serverName, string dfsPath, uint maxOutputSize, out byte[] buffer, out uint outputCount);
    }
}
