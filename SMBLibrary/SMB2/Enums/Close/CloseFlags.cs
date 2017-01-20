using System;

namespace SMBLibrary.SMB2
{
    [Flags]
    public enum CloseFlags : byte
    {
        PostQueryAttribute = 0x0001, // SMB2_CLOSE_FLAG_POSTQUERY_ATTRIB
    }
}
