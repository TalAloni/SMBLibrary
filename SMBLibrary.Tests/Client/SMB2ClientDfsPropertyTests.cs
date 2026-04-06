using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class SMB2ClientDfsPropertyTests
    {
        [TestMethod]
        public void DfsClientOptions_DefaultsToNull()
        {
            SMB2Client client = new SMB2Client();

            Assert.IsNull(client.DfsClientOptions);
        }

        [TestMethod]
        public void DfsClientOptions_CanBeSetAndRead()
        {
            SMB2Client client = new SMB2Client();
            DfsClientOptions options = new DfsClientOptions { Enabled = true };

            client.DfsClientOptions = options;

            Assert.AreSame(options, client.DfsClientOptions);
            Assert.IsTrue(client.DfsClientOptions.Enabled);
        }

        [TestMethod]
        public void DfsClientOptions_CanBeSetToNull()
        {
            SMB2Client client = new SMB2Client();
            client.DfsClientOptions = new DfsClientOptions { Enabled = true };

            client.DfsClientOptions = null;

            Assert.IsNull(client.DfsClientOptions);
        }

        [TestMethod]
        public void DfsClientOptions_EnabledDefaultsFalse()
        {
            DfsClientOptions options = new DfsClientOptions();

            Assert.IsFalse(options.Enabled);
        }
    }
}
