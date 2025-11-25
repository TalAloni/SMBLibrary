# DFS Client Usage Guide

This guide explains how to use SMBLibrary's DFS (Distributed File System) client support.

## Overview

SMBLibrary provides opt-in DFS client support that enables automatic resolution of DFS paths to their underlying file server targets. DFS is **disabled by default** to maintain backward compatibility.

## Quick Start

### Enabling DFS on a File Store

```csharp
using SMBLibrary;
using SMBLibrary.Client;
using SMBLibrary.Client.DFS;

// 1. Connect to the server normally
SMB2Client client = new SMB2Client();
client.Connect("server.domain.com", SMBTransportType.DirectTCPTransport);
client.Login("DOMAIN", "username", "password");

// 2. Get a file store via TreeConnect
NTStatus status;
ISMBFileStore fileStore = client.TreeConnect("DfsShare", out status);

// 3. Wrap with DFS support using DfsClientFactory
DfsClientOptions options = new DfsClientOptions();
options.Enabled = true;

ISMBFileStore dfsAwareStore = DfsClientFactory.CreateDfsAwareFileStore(
    fileStore,
    null,  // DFS handle (optional)
    options
);

// 4. Use the DFS-aware store for file operations
// DFS paths will be automatically resolved
object handle;
FileStatus fileStatus;
dfsAwareStore.CreateFile(
    out handle,
    out fileStatus,
    @"\DfsLink\subfolder\file.txt",
    AccessMask.GENERIC_READ,
    FileAttributes.Normal,
    ShareAccess.Read,
    CreateDisposition.FILE_OPEN,
    CreateOptions.FILE_NON_DIRECTORY_FILE,
    null
);
```

## Configuration Options

The `DfsClientOptions` class controls DFS behavior:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | bool | `false` | Master switch for DFS. When false, no DFS referral requests are issued. |
| `EnableDomainCache` | bool | `false` | Enable domain cache for domain-based DFS resolution (requires domain environment). |
| `EnableFullResolution` | bool | `false` | Enable full 14-step DFS resolution algorithm per MS-DFSC. |
| `EnableCrossServerSessions` | bool | `false` | Enable cross-server session management for DFS interlink scenarios. |
| `ReferralCacheTtlSeconds` | int | `300` | TTL in seconds for referral cache entries (5 minutes default). |
| `DomainCacheTtlSeconds` | int | `300` | TTL in seconds for domain cache entries (5 minutes default). |
| `MaxRetries` | int | `3` | Maximum retries for DFS target failover. |
| `SiteName` | string | `null` | Optional Active Directory site name for site-aware referrals. |

### Example: Full Configuration

```csharp
DfsClientOptions options = new DfsClientOptions
{
    Enabled = true,
    EnableDomainCache = true,
    EnableFullResolution = true,
    EnableCrossServerSessions = true,
    ReferralCacheTtlSeconds = 600,  // 10 minutes
    MaxRetries = 5,
    SiteName = "Default-First-Site-Name"
};
```

## Feature Flags

### Enabled (Master Switch)

When `Enabled = false` (default), DFS behavior is completely disabled:
- No DFS referral requests are sent
- Paths are used as-is without resolution
- Behavior matches pre-DFS SMBLibrary versions

### EnableDomainCache

Required for domain-based DFS namespaces (`\\domain.com\DfsRoot`). When enabled:
- Caches domain controller referrals
- Enables resolution of domain-based DFS paths

### EnableFullResolution

Enables the complete 14-step DFS resolution algorithm per MS-DFSC:
- Handles nested DFS links (interlinks)
- Supports SYSVOL and NETLOGON paths
- Implements proper cache management

### EnableCrossServerSessions

Required when DFS referrals may redirect to different file servers:
- Uses `DfsSessionManager` to manage multiple server connections
- Reuses existing connections to the same server
- Handles authentication to new servers

## Cross-Server Session Management

When accessing DFS paths that span multiple servers, use `DfsSessionManager`:

```csharp
using SMBLibrary.Client.DFS;

// Create credentials for authentication
DfsCredentials credentials = new DfsCredentials("DOMAIN", "username", "password");

// Create session manager (typically one per application/connection scope)
using (DfsSessionManager sessionManager = new DfsSessionManager())
{
    // Get or create a session to a specific server/share
    NTStatus status;
    ISMBFileStore store = sessionManager.GetOrCreateSession(
        "fileserver1.domain.com",
        "Share1",
        credentials,
        out status
    );

    if (status == NTStatus.STATUS_SUCCESS)
    {
        // Use the store...
    }
}
// Dispose automatically disconnects all managed sessions
```

## Site-Aware Referrals

For optimal target selection in multi-site environments, use `FSCTL_DFS_GET_REFERRALS_EX` with a site name:

```csharp
DfsClientOptions options = new DfsClientOptions
{
    Enabled = true,
    SiteName = "BranchOffice-Site"
};

// When SiteName is set, referral requests include site information
// and the server may return site-optimal targets
```

## Troubleshooting

### DFS Paths Not Resolving

1. **Check `Enabled` flag**: Ensure `DfsClientOptions.Enabled = true`
2. **Verify server supports DFS**: Not all SMB servers support DFS referrals
3. **Check permissions**: User must have access to the DFS namespace

### STATUS_PATH_NOT_COVERED

This status indicates the path is a DFS path but the server can't resolve it:
- Ensure you're using a DFS-aware file store via `DfsClientFactory`
- Verify the DFS namespace is properly configured on the server

### Connection Failures to Referred Targets

When using `EnableCrossServerSessions`:
- Ensure credentials are valid for all target servers
- Check network connectivity to referred servers
- Verify firewall allows SMB traffic (TCP 445)

### Cache Issues

If paths resolve to stale targets:
- Reduce `ReferralCacheTtlSeconds` for more frequent refresh
- Clear the referral cache by creating a new resolver instance

## Known Limitations

1. **SMB1 DFS**: SMB1 DFS support is experimental and may not work in all scenarios
2. **Interlinks**: Nested DFS links require `EnableFullResolution = true`
3. **Kerberos**: No special Kerberos delegation handling; uses existing SMB authentication
4. **Site Auto-Detection**: Site name must be configured manually; no automatic discovery

## Event Logging

DFS operations raise events for diagnostics. Subscribe to resolver events:

```csharp
DfsPathResolver resolver = new DfsPathResolver(referralCache, domainCache, transport);

resolver.ResolutionStarted += (sender, e) =>
{
    Console.WriteLine($"DFS resolution started: {e.OriginalPath}");
};

resolver.ReferralRequested += (sender, e) =>
{
    Console.WriteLine($"Referral requested: {e.Path} (type: {e.RequestType})");
};

resolver.ReferralReceived += (sender, e) =>
{
    Console.WriteLine($"Referral received: {e.ReferralCount} entries, TTL={e.TtlSeconds}s");
};

resolver.ResolutionCompleted += (sender, e) =>
{
    Console.WriteLine($"Resolution complete: {e.OriginalPath} -> {e.ResolvedPath}");
};
```

## See Also

- [MS-DFSC: Distributed File System (DFS) Referral Protocol](https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-dfsc/)
- [SMBLibrary GitHub Repository](https://github.com/TalAloni/SMBLibrary)
