
namespace SMBLibrary
{
    // All Win32 error codes MUST be in the range 0x0000 to 0xFFFF
    public enum Win32Error : ushort
    {
        ERROR_SUCCESS = 0x0000,
        ERROR_ACCESS_DENIED = 0x0005,
        ERROR_SHARING_VIOLATION = 0x0020,
        ERROR_DISK_FULL = 0x0070,
        ERROR_DIR_NOT_EMPTY = 0x0091,
        ERROR_ALREADY_EXISTS = 0x00B7,
        ERROR_LOGON_FAILURE = 0x052E,
        ERROR_ACCOUNT_RESTRICTION = 0x052F,
        ERROR_ACCOUNT_DISABLED = 0x0533,
        ERROR_LOGON_TYPE_NOT_GRANTED = 0x0569,
        NERR_NetNameNotFound = 0x0906,
    }
}
