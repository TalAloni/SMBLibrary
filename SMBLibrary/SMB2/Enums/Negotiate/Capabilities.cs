using System;

namespace SMBLibrary.SMB2
{
    [Flags]
    public enum Capabilities : uint
    {
        DFS = 0x00000001,               // SMB2_GLOBAL_CAP_DFS
        Leasing = 0x00000002,           // SMB2_GLOBAL_CAP_LEASING (SMB 2.1+)
        LargeMTU = 0x0000004,           // SMB2_GLOBAL_CAP_LARGE_MTU (SMB 2.1+)
        MultiChannel = 0x0000008,       // SMB2_GLOBAL_CAP_MULTI_CHANNEL (SMB 3.0+)
        PersistentHandles = 0x00000010, // SMB2_GLOBAL_CAP_PERSISTENT_HANDLES (SMB 3.0+)
        DirectoryLeasing = 0x00000020,  // SMB2_GLOBAL_CAP_DIRECTORY_LEASING (SMB 3.0+)
        Encryption = 0x00000040,        // SMB2_GLOBAL_CAP_ENCRYPTION (SMB 3.0 / SMB 3.0.2)
        Notifications = 0x00000080,     // SMB2_GLOBAL_CAP_NOTIFICATIONS (SMB 3.1.1)
    }
}
