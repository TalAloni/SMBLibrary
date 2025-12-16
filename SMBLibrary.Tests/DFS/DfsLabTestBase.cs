using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client;

namespace SMBLibrary.Tests.DFS
{
    /// <summary>
    /// Base class for DFS lab tests requiring live Hyper-V environment.
    /// </summary>
    public abstract class DfsLabTestBase
    {
        // Lab configuration - matches smb-dfs-lab-status.md
        protected static readonly string LabDomain = "LAB.LOCAL";
        protected static readonly string LabDcServer = "10.0.0.10";
        protected static readonly string LabFs1Server = "10.0.0.20";
        protected static readonly string LabFs2Server = "10.0.0.21";
        protected static readonly string DfsNamespacePath = @"\\LAB.LOCAL\Files";
        protected static readonly string DfsFolderPath = @"\\LAB.LOCAL\Files\Sales";
        protected static readonly string DirectShare1 = @"\\LAB-FS1\Sales";
        protected static readonly string DirectShare2 = @"\\LAB-FS2\Sales";
        protected static readonly string SysvolPath = @"\\LAB.LOCAL\SYSVOL";
        protected static readonly string NetlogonPath = @"\\LAB.LOCAL\NETLOGON";

        protected static string LabUsername { get; private set; }
        protected static string LabPassword { get; private set; }

        protected SMB2Client Client { get; private set; }

        // InheritanceBehavior.BeforeEachDerivedClass ensures this ClassInitialize runs
        // for each derived test class, allowing proper initialization of lab credentials
        // before any test in the derived class executes.
        [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void LabClassInit(TestContext context)
        {
            LabUsername = Environment.GetEnvironmentVariable("LAB_USERNAME") ?? "Administrator";
            LabPassword = Environment.GetEnvironmentVariable("LAB_PASSWORD");
        }

        [TestInitialize]
        public virtual void TestInit()
        {
            Client = new SMB2Client();
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            try
            {
                Client?.Disconnect();
            }
            catch { }
        }

        /// <summary>
        /// Checks if lab environment is available. Call at start of each test.
        /// </summary>
        protected void RequireLabEnvironment()
        {
            if (string.IsNullOrEmpty(LabPassword))
            {
                Assert.Inconclusive("Lab not configured: LAB_PASSWORD environment variable not set");
            }

            // Quick TCP connectivity check to DC
            try
            {
                using (var tcp = new TcpClient())
                {
                    var result = tcp.BeginConnect(LabDcServer, 445, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                    if (!success || !tcp.Connected)
                    {
                        Assert.Inconclusive($"Lab DC not reachable at {LabDcServer}:445");
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"Lab not available: {ex.Message}");
            }
        }

        /// <summary>
        /// Connects and logs in to the domain controller.
        /// </summary>
        protected void ConnectToDc()
        {
            bool connected = Client.Connect(IPAddress.Parse(LabDcServer), SMBTransportType.DirectTCPTransport);
            Assert.IsTrue(connected, $"Failed to connect to DC at {LabDcServer}");

            NTStatus status = Client.Login(LabDomain, LabUsername, LabPassword);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status, $"Login failed: {status}");
        }

        /// <summary>
        /// Connects and logs in to a specific file server by IP.
        /// </summary>
        protected void ConnectToServer(string serverIp)
        {
            bool connected = Client.Connect(IPAddress.Parse(serverIp), SMBTransportType.DirectTCPTransport);
            Assert.IsTrue(connected, $"Failed to connect to {serverIp}");

            NTStatus status = Client.Login(LabDomain, LabUsername, LabPassword);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status, $"Login failed: {status}");
        }
    }
}
