
namespace SMBLibrary.DFS
{
    /// <summary>
    /// [MS-DFSC] 2.2.4.x DFS_REFERRAL_Vx - ServerType
    /// </summary>
    public enum DfsServerType : ushort
    {
        /// <summary>
        /// The target is a non-root DFS server (link target or storage server).
        /// </summary>
        NonRoot = 0x0000,

        /// <summary>
        /// The target is a root DFS server (namespace server).
        /// </summary>
        Root = 0x0001,
    }
}
