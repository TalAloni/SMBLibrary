using System;
using System.Collections.Generic;
using System.IO;

namespace SMBLibrary.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            new NTLMAuthenticationTests().TestAll();
            new NTLMSigningTests().TestAll();
            new RC4Tests().TestAll();
            
            new NetBiosTests().TestAll();
            new RPCTests().TestAll();
            new SMB2SigningTests().TestAll();

            new NTDirectoryFileSystemTests().TestAll();
        }
    }
}
