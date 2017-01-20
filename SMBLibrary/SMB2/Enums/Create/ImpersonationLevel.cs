using System;
using System.Collections.Generic;
using System.Text;

namespace SMBLibrary.SMB2
{
    public enum ImpersonationLevel : uint
    {
        Anonymous = 0x00000000,
        Identification = 0x00000001,
        Impersonation = 0x00000002,
        Delegate = 0x00000003,
    }
}
