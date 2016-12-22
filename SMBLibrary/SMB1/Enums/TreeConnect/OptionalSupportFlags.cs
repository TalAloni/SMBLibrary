using System;

namespace SMBLibrary.SMB1
{
    [Flags]
    public enum OptionalSupportFlags : ushort
    {
        /// <summary>
        /// The server supports the use of SMB_FILE_ATTRIBUTES exclusive search attributes in client requests.
        /// </summary>
        SMB_SUPPORT_SEARCH_BITS = 0x01,
    }
}
