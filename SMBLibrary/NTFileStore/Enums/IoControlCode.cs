
namespace SMBLibrary
{
    public enum IoControlCode : uint
    {
        FSCTL_DFS_GET_REFERRALS = 0x00060194,            // SMB2-specific processing
        FSCTL_DFS_GET_REFERRALS_EX = 0x000601B0,         // SMB2-specific processing
        FSCTL_SET_REPARSE_POINT = 0x000900A4,            // SMB2-specific processing
        FSCTL_FILE_LEVEL_TRIM = 0x00098208,              // SMB2-specific processing
        FSCTL_PIPE_WAIT = 0x00110018,                    // SMB2-specific processing
        FSCTL_PIPE_PEEK = 0x0011400C,                    // SMB2-specific processing
        FSCTL_PIPE_TRANSCEIVE = 0x0011C017,              // SMB2-specific processing
        FSCTL_SRV_REQUEST_RESUME_KEY = 0x00140078,       // SMB2-specific processing
        FSCTL_VALIDATE_NEGOTIATE_INFO = 0x00140204,      // SMB2-specific processing
        FSCTL_LMR_REQUEST_RESILIENCY = 0x001401D4,       // SMB2-specific processing
        FSCTL_QUERY_NETWORK_INTERFACE_INFO = 0x001401FC, // SMB2-specific processing
        FSCTL_SRV_ENUMERATE_SNAPSHOTS = 0x00144064,      // SMB2-specific processing
        FSCTL_SRV_COPYCHUNK = 0x001440F2,                // SMB2-specific processing
        FSCTL_SRV_READ_HASH = 0x001441BB,                // SMB2-specific processing
        FSCTL_SRV_COPYCHUNK_WRITE = 0x001480F2,          // SMB2-specific processing
    }
}
