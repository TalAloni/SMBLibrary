
namespace SMBLibrary.SMB1
{
    public enum ImpersonationLevel : uint
    {
        SEC_ANONYMOUS = 0x00,
        SEC_IDENTIFY = 0x01,
        SEC_IMPERSONATE = 0x02,
        SECURITY_DELEGATION = 0x04, // SMB 1.0 addition
    }
}
