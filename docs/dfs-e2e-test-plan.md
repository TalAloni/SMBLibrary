# DFS Client E2E Test Plan

> **Lab**: LAB.LOCAL domain (DC1, FS1, FS2)  
> **Namespace**: `\\LAB.LOCAL\Files\Sales` → `\\LAB-FS1\Sales`, `\\LAB-FS2\Sales`  
> **Credentials**: `LAB\Administrator` / `Password123!`

---

## 1. Test Categories

| Category | Purpose | Environment |
|----------|---------|-------------|
| **Smoke** | Quick validation of core flows | Lab VMs running |
| **Functional** | Feature coverage per SPEC | Lab VMs running |
| **Failover** | Target unavailability scenarios | Requires VM stop/start |
| **Interop** | Windows client comparison | Wireshark captures |
| **Negative** | Error handling and edge cases | Lab + malformed inputs |
| **Performance** | Cache effectiveness, connection reuse | Lab with timing |

---

## 2. Test Infrastructure

### 2.1 Test Project Structure

```text
SMBLibrary.Tests/
├── DFS/
│   ├── DfsIntegrationTests.cs       # Existing (fakes)
│   └── DfsLabTests.cs               # NEW: Live lab tests
├── IntegrationTests/
│   └── DfsLiveTests.cs              # NEW: E2E with real network
└── TestData/
    └── DFS/
        ├── Captures/                 # Wireshark pcap files
        ├── ReferralV1.bin            # Binary test vectors
        ├── ReferralV3.bin
        ├── ReferralV4.bin
        └── NameListReferral.bin
```

### 2.2 Test Configuration

Create `SMBLibrary.Tests/appsettings.lab.json`:

```json
{
  "Lab": {
    "Domain": "LAB.LOCAL",
    "Username": "Administrator",
    "Password": "Password123!",
    "DcServer": "10.0.0.10",
    "FileServer1": "10.0.0.20",
    "FileServer2": "10.0.0.21",
    "DfsNamespace": "\\\\LAB.LOCAL\\Files",
    "DfsFolder": "\\\\LAB.LOCAL\\Files\\Sales",
    "DirectShare1": "\\\\LAB-FS1\\Sales",
    "DirectShare2": "\\\\LAB-FS2\\Sales"
  }
}
```

### 2.3 Test Base Class

```csharp
[TestClass]
public abstract class DfsLabTestBase
{
    protected static LabConfig Config { get; private set; }
    protected SMB2Client Client { get; private set; }
    
    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        Config = LoadLabConfig();
        EnsureLabAccessible();
    }
    
    [TestInitialize]
    public void TestInit()
    {
        Client = new SMB2Client();
    }
    
    [TestCleanup]
    public void TestCleanup()
    {
        Client?.Disconnect();
    }
    
    protected DfsCredentials GetCredentials() =>
        new DfsCredentials(Config.Domain, Config.Username, Config.Password);
}
```

---

## 3. Functional Test Cases

### 3.1 Basic DFS Resolution (AC2)

| Test ID | Scenario | Steps | Expected |
|---------|----------|-------|----------|
| **DFS-E2E-001** | Resolve DFS namespace root | Connect to `\\LAB.LOCAL\Files`, enable DFS, list contents | Returns folder list, referral issued |
| **DFS-E2E-002** | Resolve DFS folder | Open `\\LAB.LOCAL\Files\Sales\test.txt` | File opened via resolved target |
| **DFS-E2E-003** | Non-DFS path passthrough | Connect to `\\LAB-FS1\Sales` directly | No referral, direct access |
| **DFS-E2E-004** | DFS disabled behavior | `Enabled=false`, access DFS path | STATUS_PATH_NOT_COVERED or fails |

```csharp
[TestMethod]
[TestCategory("Lab")]
public void DfsResolution_DfsNamespaceRoot_ReturnsReferral()
{
    // Arrange
    Client.Connect(Config.DcServer, SMBTransportType.DirectTCPTransport);
    Client.Login(Config.Domain, Config.Username, Config.Password);
    
    ISMBFileStore store = Client.TreeConnect("Files", out NTStatus status);
    Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
    
    DfsClientOptions options = new DfsClientOptions { Enabled = true };
    ISMBFileStore dfsStore = DfsClientFactory.CreateDfsAwareFileStore(store, null, options);
    
    // Act
    object handle;
    FileStatus fileStatus;
    NTStatus result = dfsStore.CreateFile(
        out handle, out fileStatus,
        @"Sales\test.txt",
        AccessMask.GENERIC_READ,
        FileAttributes.Normal,
        ShareAccess.Read,
        CreateDisposition.FILE_OPEN,
        CreateOptions.FILE_NON_DIRECTORY_FILE,
        null);
    
    // Assert
    Assert.AreEqual(NTStatus.STATUS_SUCCESS, result);
    dfsStore.CloseFile(handle);
}
```

### 3.2 Referral Version Coverage (M2)

| Test ID | Scenario | Validation |
|---------|----------|------------|
| **DFS-E2E-010** | V1 referral parsing | Capture + verify `DfsReferralEntryV1` fields |
| **DFS-E2E-011** | V2 referral parsing | Verify `Proximity` field populated |
| **DFS-E2E-012** | V3 referral parsing | Verify `DfsPath`, `DfsAlternatePath`, `NetworkAddress` |
| **DFS-E2E-013** | V4 referral parsing | Verify `IsTargetSetBoundary` for multi-target |
| **DFS-E2E-014** | NameListReferral (SYSVOL) | Connect to `\\LAB.LOCAL\SYSVOL`, verify `ExpandedNames` |

```csharp
[TestMethod]
[TestCategory("Lab")]
public void DfsResolution_SysvolPath_ReturnsNameListReferral()
{
    // Arrange
    Client.Connect(Config.DcServer, SMBTransportType.DirectTCPTransport);
    Client.Login(Config.Domain, Config.Username, Config.Password);
    
    // Act - Request SYSVOL referral (should return NameListReferral with DC list)
    IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequest(
        @"\\LAB.LOCAL\SYSVOL", 16384);
    
    // Send via IPC$ and capture response
    ISMBFileStore ipcStore = Client.TreeConnect("IPC$", out NTStatus status);
    byte[] output;
    NTStatus ioctlStatus = ipcStore.DeviceIOControl(
        null,
        (uint)IoControlCode.FSCTL_DFS_GET_REFERRALS,
        request.Input,
        out output,
        16384);
    
    // Assert
    Assert.AreEqual(NTStatus.STATUS_SUCCESS, ioctlStatus);
    ResponseGetDfsReferral response = new ResponseGetDfsReferral(output);
    Assert.IsTrue(response.NumberOfReferrals > 0);
    
    DfsReferralEntryV3 entry = response.ReferralEntries[0] as DfsReferralEntryV3;
    Assert.IsNotNull(entry);
    Assert.IsTrue(entry.IsNameListReferral, "SYSVOL should return NameListReferral");
    Assert.IsNotNull(entry.ExpandedNames);
    Assert.IsTrue(entry.ExpandedNames.Count > 0, "Should have at least one DC");
}
```

### 3.3 Caching Behavior (M3)

| Test ID | Scenario | Validation |
|---------|----------|------------|
| **DFS-E2E-020** | Cache hit | Second request uses cache, no IOCTL |
| **DFS-E2E-021** | Cache expiry | Wait TTL, verify re-fetch |
| **DFS-E2E-022** | Longest prefix match | `\\LAB.LOCAL\Files\Sales\sub` matches `Sales` entry |
| **DFS-E2E-023** | Domain cache | Domain referral cached for configured TTL |

```csharp
[TestMethod]
[TestCategory("Lab")]
public void DfsCache_SecondRequest_UsesCachedReferral()
{
    // Arrange
    int ioctlCount = 0;
    // ... setup with IOCTL counting transport wrapper
    
    // Act
    var result1 = resolver.Resolve(options, @"\\LAB.LOCAL\Files\Sales\file1.txt");
    var result2 = resolver.Resolve(options, @"\\LAB.LOCAL\Files\Sales\file2.txt");
    
    // Assert
    Assert.AreEqual(1, ioctlCount, "Second request should use cache");
}
```

### 3.4 Failover Scenarios (AC2, M5)

| Test ID | Scenario | Steps | Expected |
|---------|----------|-------|----------|
| **DFS-E2E-030** | Primary target down | Stop FS1 VM, access DFS path | Resolves to FS2 |
| **DFS-E2E-031** | All targets down | Stop FS1 + FS2, access path | Clean error |
| **DFS-E2E-032** | Target recovery | Start FS1, verify re-routing | Uses FS1 again |
| **DFS-E2E-033** | Target hint rotation | Call `NextTargetHint()`, verify round-robin | Different target selected |

```csharp
[TestMethod]
[TestCategory("Lab")]
[TestCategory("Failover")]
public void DfsFailover_PrimaryTargetDown_ResolvesToSecondary()
{
    // Arrange - Requires manual VM control or PowerShell remoting
    // Stop-VM LAB-FS1 -Force (run before test)
    
    Client.Connect(Config.DcServer, SMBTransportType.DirectTCPTransport);
    Client.Login(Config.Domain, Config.Username, Config.Password);
    
    ISMBFileStore store = Client.TreeConnect("Files", out NTStatus status);
    DfsClientOptions options = new DfsClientOptions 
    { 
        Enabled = true,
        MaxRetries = 3 
    };
    ISMBFileStore dfsStore = DfsClientFactory.CreateDfsAwareFileStore(store, null, options);
    
    // Act
    object handle;
    FileStatus fileStatus;
    NTStatus result = dfsStore.CreateFile(
        out handle, out fileStatus,
        @"Sales\test.txt",
        AccessMask.GENERIC_READ,
        FileAttributes.Normal,
        ShareAccess.Read,
        CreateDisposition.FILE_OPEN,
        CreateOptions.FILE_NON_DIRECTORY_FILE,
        null);
    
    // Assert - Should succeed via FS2
    Assert.AreEqual(NTStatus.STATUS_SUCCESS, result);
    
    // Cleanup
    // Start-VM LAB-FS1 (run after test)
}
```

---

## 4. Interop Validation

### 4.1 Wireshark Capture Strategy

Capture referral traffic for comparison:

```powershell
# On host machine with Wireshark
# Filter: smb2.cmd == 11 && smb2.ioctl.function == 0x00060194

# Step 1: Clear Windows client cache
dfsutil cache referral flush

# Step 2: Start capture

# Step 3: Access DFS path from Windows
dir \\LAB.LOCAL\Files\Sales

# Step 4: Stop capture, export IOCTL request/response payloads
```

### 4.2 Compare Vectors

| Capture | Purpose | Validation |
|---------|---------|------------|
| `V3ReferralResponse.pcap` | Normal V3 referral | Parse with `ResponseGetDfsReferral`, compare fields |
| `V4MultiTarget.pcap` | V4 with 2 targets | Verify `TargetSetBoundary` on second entry |
| `NameListReferral.pcap` | SYSVOL DC list | Verify `SpecialName`, `ExpandedNames` |
| `SiteAwareReferral.pcap` | With SiteName | Verify site-optimal target returned |

```csharp
[TestMethod]
[TestCategory("Interop")]
public void ResponseParsing_CapturedV3Referral_MatchesWindowsBehavior()
{
    // Arrange - Load captured response from Windows client
    byte[] capturedResponse = File.ReadAllBytes(@"TestData\DFS\Captures\V3ReferralResponse.bin");
    
    // Act
    ResponseGetDfsReferral response = new ResponseGetDfsReferral(capturedResponse);
    
    // Assert - Compare to expected values from Wireshark decode
    Assert.AreEqual(3, ((DfsReferralEntryV3)response.ReferralEntries[0]).VersionNumber);
    Assert.AreEqual(@"\\LAB.LOCAL\Files\Sales", 
        ((DfsReferralEntryV3)response.ReferralEntries[0]).DfsPath);
}
```

---

## 5. Error Handling Tests (AC3)

| Test ID | Scenario | Expected |
|---------|----------|----------|
| **DFS-E2E-050** | Malformed response buffer | `ArgumentException`, no crash |
| **DFS-E2E-051** | Truncated referral entry | `ArgumentException` |
| **DFS-E2E-052** | Invalid string offset | `ArgumentException` |
| **DFS-E2E-053** | STATUS_BUFFER_OVERFLOW | Parse partial, log warning |
| **DFS-E2E-054** | STATUS_NOT_FOUND | Return original path |
| **DFS-E2E-055** | STATUS_FS_DRIVER_REQUIRED | Return NotApplicable, use original |

---

## 6. Site-Aware Referrals (M5-T3)

| Test ID | Scenario | Expected |
|---------|----------|----------|
| **DFS-E2E-060** | Request with SiteName | `FSCTL_DFS_GET_REFERRALS_EX` used |
| **DFS-E2E-061** | Site-optimal target | Server returns site-local target first |

```csharp
[TestMethod]
[TestCategory("Lab")]
public void DfsReferralEx_WithSiteName_ReturnsOptimalTarget()
{
    // Arrange
    DfsClientOptions options = new DfsClientOptions
    {
        Enabled = true,
        SiteName = "Default-First-Site-Name"
    };
    
    // Act - Use extended referral request
    IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequestEx(
        @"\\LAB.LOCAL\Files\Sales",
        options.SiteName,
        16384);
    
    // Assert - Request uses FSCTL_DFS_GET_REFERRALS_EX
    Assert.AreEqual((uint)IoControlCode.FSCTL_DFS_GET_REFERRALS_EX, request.CtlCode);
}
```

---

## 7. Cross-Server Session Management (M5-T1)

| Test ID | Scenario | Expected |
|---------|----------|----------|
| **DFS-E2E-070** | Interlink to different server | New session created |
| **DFS-E2E-071** | Same server, different share | Session reused |
| **DFS-E2E-072** | Dispose cleanup | All sessions disconnected |

---

## 8. Regression Tests (AC5)

Ensure existing non-DFS tests still pass:

```powershell
# Run full test suite with DFS disabled by default
dotnet test --filter "Category!=Lab"

# Run DFS-specific tests
dotnet test --filter "Category=Lab"
```

---

## 9. Test Execution Plan

### 9.1 Local Development

```powershell
# 1. Start lab VMs
Start-VM LAB-DC1, LAB-FS1, LAB-FS2
Start-Sleep -Seconds 120  # Wait for services

# 2. Configure host DNS
Set-DnsClientServerAddress -InterfaceAlias 'vEthernet (LAB_Internal)' -ServerAddresses '10.0.0.10'

# 3. Run smoke tests
dotnet test --filter "TestCategory=Lab&TestCategory=Smoke"

# 4. Run full lab tests
dotnet test --filter "TestCategory=Lab"

# 5. Run failover tests (manual VM control)
dotnet test --filter "TestCategory=Failover"
```

### 9.2 CI Integration

```yaml
# .github/workflows/dfs-lab-tests.yml (example)
name: DFS Lab Tests

on:
  workflow_dispatch:  # Manual trigger only (requires lab)
  
jobs:
  lab-tests:
    runs-on: self-hosted  # Requires runner with lab access
    steps:
      - uses: actions/checkout@v4
      - name: Start Lab VMs
        run: |
          Start-VM LAB-DC1, LAB-FS1, LAB-FS2
          Start-Sleep -Seconds 120
      - name: Run Lab Tests
        run: dotnet test --filter "TestCategory=Lab" --logger trx
      - name: Stop Lab VMs
        if: always()
        run: Stop-VM LAB-DC1, LAB-FS1, LAB-FS2
```

---

## 10. Test Matrix Summary

| Feature | Unit Tests | Integration (Fakes) | Lab E2E | Interop Capture |
|---------|------------|---------------------|---------|-----------------|
| V1-V4 Parsing | ✅ | ✅ | 🔲 | 🔲 |
| NameListReferral | ✅ | 🔲 | 🔲 | 🔲 |
| DfsPath Helper | ✅ | ✅ | N/A | N/A |
| Referral Cache | ✅ | ✅ | 🔲 | N/A |
| Domain Cache | ✅ | ✅ | 🔲 | N/A |
| DfsPathResolver | ✅ | ✅ | 🔲 | 🔲 |
| DfsClientFactory | ✅ | ✅ | 🔲 | N/A |
| DfsSessionManager | ✅ | ✅ | 🔲 | N/A |
| Site-Aware Referrals | ✅ | 🔲 | 🔲 | 🔲 |
| Failover | 🔲 | ✅ | 🔲 | N/A |
| SYSVOL/NETLOGON | ✅ | 🔲 | 🔲 | 🔲 |

**Legend**: ✅ = Covered | 🔲 = To Implement | N/A = Not Applicable

---

## 11. Recommended Test Order

1. **Phase 1 - Captures** (1-2 hours)
   - Capture V3/V4 referral responses from Windows client
   - Capture SYSVOL NameListReferral
   - Export binary payloads to `TestData/DFS/Captures/`

2. **Phase 2 - Interop Validation** (2-3 hours)
   - Write tests that parse captured payloads
   - Compare field-by-field with Wireshark decode
   - Fix any parsing discrepancies

3. **Phase 3 - Live Lab Tests** (4-6 hours)
   - Implement `DfsLabTestBase` infrastructure
   - Add DFS-E2E-001 through DFS-E2E-014
   - Validate basic resolution works end-to-end

4. **Phase 4 - Failover Tests** (2-3 hours)
   - Add VM control helpers (PowerShell remoting)
   - Implement failover scenarios
   - Test target hint rotation

5. **Phase 5 - Documentation** (1-2 hours)
   - Update `DFS-Usage.md` with any findings
   - Document known limitations
   - Create troubleshooting guide updates

---

## 12. Success Criteria

- [ ] All 234 existing unit tests pass
- [ ] Captured referral payloads parse identically to Windows
- [ ] DFS namespace resolution works end-to-end in lab
- [ ] SYSVOL/NETLOGON NameListReferral works
- [ ] Failover to secondary target succeeds when primary down
- [ ] No regression in non-DFS SMB client behavior
- [ ] Event logging provides sufficient diagnostics
