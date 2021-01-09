using System;
using System.Collections.Generic;
using System.Text;

namespace SMBLibrary.Services
{
    public interface IRPCRequest
    {
        byte[] GetBytes();
    }
}
