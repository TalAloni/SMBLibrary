# DFS Implementation Plan

> **Project**: SMBLibrary DFS Client  
> **Target**: Full MS-DFSC Compliance  
> **Created**: 2024-11-24  
> **Status**: Planning

---

## Overview

This plan outlines a phased approach to implementing a complete DFS client in SMBLibrary, targeting full MS-DFSC specification compliance. The implementation is divided into 5 milestones, each building on the previous.

**Reference implementations**:
- Primary: smbj (Java) - Most complete, 14-step algorithm
- Secondary: smbprotocol (Python) - Clean, well-documented

**Estimated effort**: 4-6 weeks for core functionality

**Version impact**: Minor version bump (e.g., 1.x → 1.x+1) — DFS is additive, opt-in, backward compatible

---

## Success Criteria

1. **Standalone DFS resolution**: Resolve paths like `\\server\dfsroot\link` to actual file server targets
2. **Domain-based DFS resolution**: Resolve paths like `\\DOMAIN.COM\DFS\folder` via DC referrals
3. **Target failover**: Automatically retry alternate targets when primary fails
4. **Cache efficiency**: Avoid redundant referral requests via proper caching
5. **Backward compatibility**: All existing SMBLibrary functionality unaffected when DFS disabled

## Non-Goals (Out of Scope)

- **DFS-N server implementation** — This plan covers client-side only
- **DFS-R replication awareness** — No replication topology handling
- **Automatic domain discovery** — Requires domain membership; user must provide domain info
- **Kerberos delegation** — Authentication is handled by existing SMB auth; no new auth flows
- **Site-awareness auto-detection** — Site name must be configured manually if needed

## Feature Flags & Rollback

All DFS features are opt-in via `DfsClientOptions`. Granular flags enable incremental rollout:

```csharp
public class DfsClientOptions
{
    // Master switch - disables all DFS when false (default: false for backward compat)
    public bool Enabled { get; set; }
    
    // M3: Domain cache (requires domain environment)
    public bool EnableDomainCache { get; set; }
    
    // M4: Full 14-step resolution (vs simple single-request)
    public bool EnableFullResolution { get; set; }
    
    // M5: Cross-server session management
    public bool EnableCrossServerSessions { get; set; }
    
    // Timeouts and limits
    public int ReferralCacheTtlSeconds { get; set; }
    public int DomainCacheTtlSeconds { get; set; }
    public int MaxRetries { get; set; }
    public string SiteName { get; set; }
}
```

**Rollback**: Set `Enabled = false` to completely disable DFS. Individual flags allow partial rollback.

## Observability Strategy

Per SMBLibrary architecture, use `LogEntryAdded` events (no external logging frameworks).

**Events to add** (in `SMBLibrary/Client/DFS/DfsEvents.cs`):

| Event | Severity | Data | When |
|-------|----------|------|------|
| `DfsReferralRequested` | Verbose | Path, RequestType | Before sending referral request |
| `DfsReferralReceived` | Verbose | Path, TargetCount, TTL | After successful referral response |
| `DfsReferralFailed` | Warning | Path, NTStatus | Referral request failed |
| `DfsCacheHit` | Verbose | Path, CacheEntry | Cache lookup succeeded |
| `DfsCacheMiss` | Verbose | Path | Cache lookup found nothing |
| `DfsCacheExpired` | Verbose | Path, CacheEntry | Entry expired, will refresh |
| `DfsTargetFailover` | Information | Path, FromTarget, ToTarget | Switching to alternate target |
| `DfsResolutionComplete` | Verbose | OriginalPath, ResolvedPath | Resolution finished |
| `DfsResolutionFailed` | Warning | Path, Reason | Resolution failed after retries |

## Thread Safety Approach

- **Caches**: Use `ConcurrentDictionary<K,V>` for thread-safe lookups
- **Target hint indices**: Use `Interlocked.CompareExchange` for atomic updates
- **Cache entries**: Immutable after construction; replace rather than mutate
- **Session manager**: Lock on hostname key during session creation

## Performance Targets

| Operation | Target | Notes |
|-----------|--------|-------|
| Cache lookup (hit) | < 1ms | Tree traversal is O(path depth) |
| Cache lookup (miss) | < 1ms | Fast negative result |
| Referral request (network) | < 500ms | Depends on server; includes RTT |
| Full resolution (cache hit) | < 5ms | Prefix match + path replacement |
| Full resolution (cache miss) | < 1s | Network request + caching |
| Target failover | < 2s | Per-target timeout before cycling |

## Architecture Decision: Tree-Based Cache

**Context**: DFS paths require longest-prefix matching (e.g., `\\server\dfs\link\sub` matches `\\server\dfs\link`).

**Options considered**:
1. **Flat dictionary** — Simple, but requires iterating all keys for prefix matching (O(n))
2. **Tree structure** — More complex, but O(path depth) lookups with natural prefix matching
3. **Trie with compression** — Optimal but over-engineered for typical namespace sizes

**Decision**: Tree structure (Option 2)

**Rationale**:
- Matches smbj's proven implementation
- Natural longest-prefix matching via tree traversal
- Acceptable complexity for the performance benefit
- Typical DFS namespaces have <1000 entries; tree handles this well

**Consequences**:
- More complex implementation than flat dictionary
- Need to handle case-insensitive path components
- Tree nodes must be thread-safe (ConcurrentDictionary for children)

---

## Milestone 1: Foundation & Quick Wins

**Goal**: Fix immediate issues and establish infrastructure  
**Duration**: 3-5 days  
**Dependencies**: None

### Acceptance Criteria (M1)

```gherkin
Given a UNC path "\\server\share\folder"
When parsed by DfsPath
Then PathComponents contains ["server", "share", "folder"]

Given a path "\\domain\SYSVOL\policy"
When checked with IsSysVolOrNetLogon
Then returns true

Given a path "\\server\IPC$"
When checked with IsIpc
Then returns true

Given DfsPath "\\server\dfs\link\file.txt" and prefix "\\server\dfs\link"
When ReplacePrefix called with target "\\fileserver\share"
Then result is "\\fileserver\share\file.txt"
```

### 1.1 Fix MaxReferralLevel (Trivial)

**File**: `SMBLibrary/Client/DFS/DfsIoctlRequestBuilder.cs`

```csharp
// Change from:
dfsRequest.MaxReferralLevel = 0;
// To:
dfsRequest.MaxReferralLevel = 4;  // Request V4 referrals
```

**Why**: Enables V4 referral responses with TargetSetBoundary support.

### 1.2 Add ReferralHeaderFlags Constants

**File**: `SMBLibrary/DFS/DfsReferralHeaderFlags.cs` (new)

```csharp
[Flags]
public enum DfsReferralHeaderFlags : uint
{
    None = 0x00000000,
    ReferralServers = 0x00000001,  // Targets are other DFS servers
    StorageServers = 0x00000002,   // Targets are file servers
    TargetFailback = 0x00000004,   // Client should fail back to preferred target
}
```

### 1.3 Add ReferralEntryFlags Constants

**File**: `SMBLibrary/DFS/DfsReferralEntryFlags.cs` (new)

```csharp
[Flags]
public enum DfsReferralEntryFlags : ushort
{
    None = 0x0000,
    NameListReferral = 0x0002,    // DC referral response
    TargetSetBoundary = 0x0004,   // V4: marks target set boundary
}
```

### 1.4 Add ServerType Enum

**File**: `SMBLibrary/DFS/DfsServerType.cs` (new)

```csharp
public enum DfsServerType : ushort
{
    NonRoot = 0x0000,  // LINK target
    Root = 0x0001,     // ROOT target
}
```

### 1.5 Add DfsPath Helper Class

**File**: `SMBLibrary/Client/DFS/DfsPath.cs` (new)

Based on smbj's `DFSPath.java`:

```csharp
// NOTE: net20-compatible - no IReadOnlyList, no expression-bodied members
public class DfsPath
{
    private List<string> m_pathComponents;
    
    public DfsPath(string uncPath)
    {
        m_pathComponents = SplitPath(uncPath);
    }
    
    public DfsPath(List<string> pathComponents)
    {
        m_pathComponents = pathComponents;
    }
    
    public List<string> PathComponents
    {
        get { return m_pathComponents; }
    }
    
    public bool HasOnlyOneComponent
    {
        get { return m_pathComponents.Count == 1; }
    }
    
    public bool IsSysVolOrNetLogon
    {
        get
        {
            if (m_pathComponents.Count > 1)
            {
                string second = m_pathComponents[1];
                return string.Equals(second, "SYSVOL", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(second, "NETLOGON", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
    
    public bool IsIpc
    {
        get
        {
            if (m_pathComponents.Count > 1)
            {
                return string.Equals(m_pathComponents[1], "IPC$", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
    
    public DfsPath ReplacePrefix(string prefixToReplace, DfsPath target);
    public string ToUncPath();
    private static List<string> SplitPath(string uncPath);
}
```

### 1.6 Unit Tests for Foundation

**File**: `SMBLibrary.Tests/DFS/DfsPathTests.cs` (new)

- Test path parsing from various UNC formats
- Test prefix replacement logic
- Test SYSVOL/NETLOGON detection
- Test IPC$ detection

### Milestone 1 Deliverables

- [ ] MaxReferralLevel = 4
- [ ] `DfsReferralHeaderFlags` enum
- [ ] `DfsReferralEntryFlags` enum
- [ ] `DfsServerType` enum
- [ ] `DfsPath` helper class
- [ ] Unit tests for DfsPath

### Done Criteria (M1)

- [ ] All unit tests pass
- [ ] Code compiles on `net20`, `net40`, `netstandard2.0` targets
- [ ] No new dependencies added
- [ ] Code review approved

---

## Milestone 2: Complete Referral Structures

**Goal**: Full V1-V4 referral parsing with NameListReferral support  
**Duration**: 5-7 days  
**Dependencies**: Milestone 1

### Acceptance Criteria (M2)

```gherkin
Given a captured V1 referral response byte sequence
When parsed by ResponseGetDfsReferral
Then V1 entry has correct VersionNumber, Size, TimeToLive, ShareName

Given a captured V3 NameListReferral response (DC referral)
When parsed by ResponseGetDfsReferral
Then entry.IsNameListReferral is true
And entry.ExpandedNames contains DC hostnames

Given a captured V4 referral with TargetSetBoundary flag
When parsed by ResponseGetDfsReferral
Then entry.IsTargetSetBoundary is true for boundary entries

Given a RequestGetDfsReferralEx with SiteName
When serialized with GetBytes()
Then output matches MS-DFSC 2.2.3 format
```

### Test Fixtures (M2)

**File**: `SMBLibrary.Tests/DFS/TestData/` (new directory)

Capture or construct byte arrays for:

| File | Description | Source |
|------|-------------|--------|
| `V1ReferralResponse.bin` | Basic V1 referral | Wireshark capture or hand-crafted |
| `V2ReferralResponse.bin` | V2 with Proximity | Wireshark capture |
| `V3ReferralResponse.bin` | V3 normal referral | Wireshark capture |
| `V3NameListReferral.bin` | V3 DC referral | Domain environment capture |
| `V4ReferralResponse.bin` | V4 with TargetSetBoundary | Wireshark capture |
| `V4MultiTarget.bin` | V4 with multiple targets | Test failover |

### 2.1 Fix V1 Referral Structure

**File**: `SMBLibrary/DFS/DfsReferralEntryV1.cs`

Add missing fields per MS-DFSC 2.2.5.1:

```csharp
public class DfsReferralEntryV1 : DfsReferralEntry
{
    public ushort VersionNumber { get; set; }
    public ushort Size { get; set; }
    public DfsServerType ServerType { get; set; }      // NEW
    public DfsReferralEntryFlags ReferralEntryFlags { get; set; }  // NEW
    public uint TimeToLive { get; set; }
    public string ShareName { get; set; }  // Inline string, not offset
}
```

### 2.2 Fix V2 Referral Structure

**File**: `SMBLibrary/DFS/DfsReferralEntryV2.cs`

Add missing `Proximity` field:

```csharp
public class DfsReferralEntryV2 : DfsReferralEntry
{
    public ushort VersionNumber { get; set; }
    public ushort Size { get; set; }
    public DfsServerType ServerType { get; set; }
    public DfsReferralEntryFlags ReferralEntryFlags { get; set; }
    public uint Proximity { get; set; }  // NEW - 4 bytes at offset 8
    public uint TimeToLive { get; set; }
    public string DfsPath { get; set; }
    public string DfsAlternatePath { get; set; }
    public string NetworkAddress { get; set; }
}
```

### 2.3 Enhance V3 Referral Structure

**File**: `SMBLibrary/DFS/DfsReferralEntryV3.cs`

Add NameListReferral support and ServiceSiteGuid:

```csharp
public class DfsReferralEntryV3 : DfsReferralEntry
{
    public ushort VersionNumber { get; set; }
    public ushort Size { get; set; }
    public DfsServerType ServerType { get; set; }
    public DfsReferralEntryFlags ReferralEntryFlags { get; set; }
    public uint TimeToLive { get; set; }
    
    // Normal referral fields
    public string DfsPath { get; set; }
    public string DfsAlternatePath { get; set; }
    public string NetworkAddress { get; set; }
    public Guid ServiceSiteGuid { get; set; }  // NEW - 16 bytes
    
    // NameListReferral fields (when ReferralEntryFlags & NameListReferral)
    public string SpecialName { get; set; }           // NEW
    public List<string> ExpandedNames { get; set; }   // NEW - DC list
    
    // NOTE: net20-compatible - explicit property getter
    public bool IsNameListReferral
    {
        get { return (ReferralEntryFlags & DfsReferralEntryFlags.NameListReferral) != 0; }
    }
}
```

### 2.4 Add V4 Referral Structure

**File**: `SMBLibrary/DFS/DfsReferralEntryV4.cs` (new)

V4 is identical to V3 but adds TargetSetBoundary flag interpretation:

```csharp
public class DfsReferralEntryV4 : DfsReferralEntryV3
{
    // NOTE: net20-compatible - explicit property getter
    public bool IsTargetSetBoundary
    {
        get { return (ReferralEntryFlags & DfsReferralEntryFlags.TargetSetBoundary) != 0; }
    }
}
```

### 2.5 Update ResponseGetDfsReferral Parser

**File**: `SMBLibrary/DFS/ResponseGetDfsReferral.cs`

- Add V4 parsing (version == 4)
- Add NameListReferral parsing branch for V3/V4
- Parse and expose `ReferralHeaderFlags` as enum
- Use typed enums for ServerType and ReferralEntryFlags

### 2.6 Add REQ_GET_DFS_REFERRAL_EX

**File**: `SMBLibrary/DFS/RequestGetDfsReferralEx.cs` (new)

```csharp
public class RequestGetDfsReferralEx
{
    public ushort MaxReferralLevel { get; set; } = 4;
    public DfsReferralRequestFlags RequestFlags { get; set; }
    public string RequestFileName { get; set; }
    public string SiteName { get; set; }  // Optional, for site-aware referrals
    
    public byte[] GetBytes();
}

[Flags]
public enum DfsReferralRequestFlags : ushort
{
    None = 0x0000,
    SiteName = 0x0001,
}
```

### 2.7 Unit Tests for Referral Structures

**File**: `SMBLibrary.Tests/DFS/DfsReferralParsingTests.cs` (new)

- Test V1, V2, V3, V4 parsing with known byte sequences
- Test NameListReferral parsing (DC referral)
- Test TargetSetBoundary flag extraction
- Test round-trip serialization

### Milestone 2 Deliverables

- [ ] Fixed V1 structure with ServerType/ReferralEntryFlags
- [ ] Fixed V2 structure with Proximity
- [ ] Enhanced V3 with NameListReferral + ServiceSiteGuid
- [ ] New V4 structure with TargetSetBoundary
- [ ] Updated response parser for all versions
- [ ] `RequestGetDfsReferralEx` structure
- [ ] Test fixtures (byte sequences)
- [ ] Comprehensive parsing unit tests

### Done Criteria (M2)

- [ ] All V1-V4 parsing tests pass with captured byte sequences
- [ ] NameListReferral parsing verified with DC referral fixture
- [ ] Round-trip serialization tests pass
- [ ] Code compiles on all targets
- [ ] Code review approved

---

## Milestone 3: Caching Infrastructure

**Goal**: Proper ReferralCache and DomainCache per MS-DFSC 3.1.1  
**Duration**: 5-7 days  
**Dependencies**: Milestone 2

### Acceptance Criteria (M3)

```gherkin
Given a ReferralCache with entry for "\\server\dfs\link"
When Lookup called with "\\server\dfs\link\subdir\file.txt"
Then returns the cached entry (longest-prefix match)

Given a ReferralCacheEntry with 3 targets
When GetTargetHint called
Then returns first target
When NextTargetHint called twice
Then returns second, then third target
When NextTargetHint called again
Then returns null (exhausted)

Given a ReferralCacheEntry with TTL of 300 seconds
When checked after 301 seconds
Then IsExpired returns true

Given concurrent threads accessing ReferralCache
When Put and Lookup called simultaneously
Then no exceptions or data corruption occurs
```

### 3.1 Create TargetSetEntry

**File**: `SMBLibrary/Client/DFS/TargetSetEntry.cs` (new)

```csharp
public class TargetSetEntry
{
    public DfsPath TargetPath { get; }
    public bool IsTargetSetBoundary { get; }
    
    public TargetSetEntry(string targetPath, bool targetSetBoundary);
}
```

### 3.2 Create ReferralCacheEntry

**File**: `SMBLibrary/Client/DFS/ReferralCacheEntry.cs` (new)

Based on smbj's `ReferralCacheEntry`:

```csharp
public class ReferralCacheEntry
{
    public string DfsPathPrefix { get; }
    public DfsServerType RootOrLink { get; }
    public bool IsInterlink { get; }
    public int TtlSeconds { get; }
    public DateTime ExpiresUtc { get; }
    public bool TargetFailback { get; }
    public IReadOnlyList<TargetSetEntry> TargetList { get; }
    
    private int _targetHintIndex;
    
    public ReferralCacheEntry(ResponseGetDfsReferral response, DomainCache domainCache);
    
    // NOTE: net20-compatible - explicit property getters
    public bool IsExpired { get; }
    public bool IsRoot
    {
        get { return RootOrLink == DfsServerType.Root; }
    }
    public bool IsLink
    {
        get { return RootOrLink == DfsServerType.NonRoot; }
    }
    
    public TargetSetEntry GetTargetHint();
    public TargetSetEntry NextTargetHint();  // Returns null when exhausted
    public void ResetTargetHint();
}
```

### 3.3 Create Tree-Based ReferralCache

**File**: `SMBLibrary/Client/DFS/ReferralCache.cs` (new)

Based on smbj's tree structure for efficient prefix matching:

```csharp
public class ReferralCache
{
    public ReferralCacheEntry Lookup(DfsPath path);
    public void Put(ReferralCacheEntry entry);
    public void Clear(DfsPath path);
    public void ClearAll();
    public void ClearExpired();
}

internal class ReferralCacheNode
{
    private readonly string _pathComponent;
    private readonly ConcurrentDictionary<string, ReferralCacheNode> _children;
    private volatile ReferralCacheEntry _entry;
    
    // Tree traversal for longest-prefix matching
}
```

### 3.4 Create DomainCacheEntry

**File**: `SMBLibrary/Client/DFS/DomainCacheEntry.cs` (new)

```csharp
public class DomainCacheEntry
{
    public string DomainName { get; }
    public IReadOnlyList<string> DcList { get; }
    public DateTime ExpiresUtc { get; }
    
    private int _dcHintIndex;
    
    public DomainCacheEntry(DfsReferralEntryV3 nameListReferral);
    
    public string GetDcHint();
    public string NextDcHint();  // Returns null when exhausted
    public bool IsExpired { get; }
    public bool IsValid { get; }
    
    public void ProcessDcReferral(ResponseGetDfsReferral response);
}
```

### 3.5 Create DomainCache

**File**: `SMBLibrary/Client/DFS/DomainCache.cs` (new)

```csharp
public class DomainCache
{
    private readonly ConcurrentDictionary<string, DomainCacheEntry> _cache;
    
    public DomainCacheEntry Lookup(string domainName);
    public void Put(DomainCacheEntry entry);
    public void Clear();
}
```

### 3.6 Unit Tests for Caches

**File**: `SMBLibrary.Tests/DFS/ReferralCacheTests.cs` (new)

- Test longest-prefix matching
- Test TTL expiration
- Test target hint cycling
- Test concurrent access

**File**: `SMBLibrary.Tests/DFS/DomainCacheTests.cs` (new)

- Test domain lookup
- Test DC hint cycling
- Test expiration

### Milestone 3 Deliverables

- [ ] `TargetSetEntry` class
- [ ] `ReferralCacheEntry` with target failover
- [ ] Tree-based `ReferralCache`
- [ ] `DomainCacheEntry` with DC failover
- [ ] `DomainCache`
- [ ] Unit tests for both caches

### Done Criteria (M3)

- [ ] Longest-prefix matching works correctly
- [ ] Target hint cycling is thread-safe (uses `Interlocked`)
- [ ] TTL expiration tests pass
- [ ] Concurrent access stress test passes (no race conditions)
- [ ] Memory profiling shows no leaks under repeated Put/Clear cycles
- [ ] Code compiles on all targets
- [ ] Code review approved

---

## Milestone 4: Resolution Algorithm

**Goal**: Implement the 14-step MS-DFSC resolution algorithm  
**Duration**: 7-10 days  
**Dependencies**: Milestone 3

### Acceptance Criteria (M4)

```gherkin
Given a standalone DFS path "\\server\dfsroot\link\file.txt"
When resolved with DfsPathResolver
Then returns resolved path "\\fileserver\share\file.txt"
And ReferralCache contains entry for "\\server\dfsroot\link"

Given a cached referral entry with primary target down
When resolution attempted
Then automatically fails over to secondary target
And DfsTargetFailover event is raised

Given a response with ReferralServers=1 and StorageServers=0
When IsInterlink checked
Then returns true

Given an IPC$ path "\\server\IPC$"
When resolution attempted
Then returns path unchanged (short-circuit)

Given STATUS_PATH_NOT_COVERED error from server
When caught by SMB client
Then triggers DFS referral request and retries with resolved path
```

### 4.1 Define Request Types

**File**: `SMBLibrary/Client/DFS/DfsRequestType.cs` (new)

```csharp
public enum DfsRequestType
{
    Domain,   // Domain referral (list trusted domains)
    Dc,       // DC referral (list DCs for a domain)
    SysVol,   // SYSVOL/NETLOGON referral
    Root,     // DFS root referral
    Link,     // DFS link referral
}
```

### 4.2 Create DfsResolverState

**File**: `SMBLibrary/Client/DFS/DfsResolverState.cs` (new)

Internal state tracking for resolution:

```csharp
internal class DfsResolverState<T>
{
    public DfsPath Path { get; set; }
    public bool IsDfsPath { get; set; }
    public Func<DfsPath, T> Operation { get; }
    public T Result { get; set; }
}
```

### 4.3 Implement DfsPathResolver (Core Algorithm)

**File**: `SMBLibrary/Client/DFS/DfsPathResolver.cs` (new)

Based on smbj's `DFSPathResolver.java`:

```csharp
public class DfsPathResolver
{
    private readonly ReferralCache _referralCache;
    private readonly DomainCache _domainCache;
    private readonly IDfsReferralTransport _transport;
    private readonly DfsClientOptions _options;
    
    public DfsPathResolver(
        IDfsReferralTransport transport,
        DfsClientOptions options);
    
    // Main entry point
    public T Resolve<T>(DfsPath path, Func<DfsPath, T> operation);
    
    // 14-step algorithm (private methods)
    private T Step1<T>(DfsResolverState<T> state);   // Single component check
    private T Step2<T>(DfsResolverState<T> state);   // ReferralCache lookup
    private T Step3<T>(DfsResolverState<T> state, ReferralCacheEntry entry);  // Root hit
    private T Step4<T>(DfsResolverState<T> state, ReferralCacheEntry entry);  // Link hit
    private T Step5<T>(DfsResolverState<T> state);   // DomainCache lookup
    private T Step6<T>(DfsResolverState<T> state);   // ROOT referral request
    private T Step7<T>(DfsResolverState<T> state, ReferralCacheEntry entry);  // ROOT success
    private T Step8<T>(DfsResolverState<T> state, ReferralCacheEntry entry);  // I/O operation
    private T Step9<T>(DfsResolverState<T> state, ReferralCacheEntry entry);  // Expired LINK
    private T Step10<T>(DfsResolverState<T> state);  // SYSVOL referral
    private T Step11<T>(DfsResolverState<T> state, ReferralCacheEntry entry); // Interlink
    private T Step12<T>(DfsResolverState<T> state);  // Not DFS
    private T Step13<T>(DfsResolverState<T> state);  // DC error
    private T Step14<T>(DfsResolverState<T> state);  // DFS error
    
    // Helpers
    private ReferralCacheEntry SendReferralRequest(DfsRequestType type, string host, DfsPath path);
    private bool IsInterlink(ResponseGetDfsReferral response);
}
```

### 4.4 Implement Interlink Detection

Per MS-DFSC 3.1.5.4.5:

```csharp
private bool IsInterlink(ResponseGetDfsReferral response)
{
    var flags = (DfsReferralHeaderFlags)response.ReferralHeaderFlags;
    
    // Rule 1: ReferralServers=1 AND StorageServers=0
    if (flags.HasFlag(DfsReferralHeaderFlags.ReferralServers) &&
        !flags.HasFlag(DfsReferralHeaderFlags.StorageServers))
    {
        return true;
    }
    
    // Rule 2: Single target AND first component in DomainCache
    if (response.ReferralEntries.Count == 1)
    {
        var path = new DfsPath(response.ReferralEntries[0].NetworkAddress);
        if (_domainCache.Lookup(path.PathComponents[0]) != null)
        {
            return true;
        }
    }
    
    return false;
}
```

### 4.5 Implement Target Failover

In Step3/Step8:

```csharp
private T Step3WithFailover<T>(DfsResolverState<T> state, ReferralCacheEntry entry)
{
    var initialPath = state.Path;
    var target = entry.GetTargetHint();
    Exception lastException = null;
    
    while (target != null)
    {
        try
        {
            state.Path = state.Path.ReplacePrefix(
                entry.DfsPathPrefix,
                target.TargetPath);
            state.IsDfsPath = true;
            return Step8(state, entry);
        }
        catch (NTStatusException ex) when (ex.Status == NTStatus.STATUS_PATH_NOT_COVERED)
        {
            // DFS redirect needed - not a failover scenario
            throw;
        }
        catch (Exception ex)
        {
            lastException = ex;
            target = entry.NextTargetHint();
            state.Path = initialPath;
        }
    }
    
    throw new DfsException("All targets exhausted", lastException);
}
```

### 4.6 Handle STATUS_PATH_NOT_COVERED

Integration point with SMB client:

```csharp
// In SMB2Client or file operation wrapper
try
{
    return PerformOperation(path);
}
catch (NTStatusException ex) when (ex.Status == NTStatus.STATUS_PATH_NOT_COVERED)
{
    // Path is in DFS namespace but needs referral
    var resolver = GetDfsResolver();
    return resolver.Resolve(new DfsPath(path), p => PerformOperation(p.ToUncPath()));
}
```

### 4.7 Unit Tests for Algorithm

**File**: `SMBLibrary.Tests/DFS/DfsPathResolverTests.cs` (new)

- Test each step in isolation with mocked transport
- Test full resolution flow
- Test failover scenarios
- Test interlink detection
- Test SYSVOL/NETLOGON handling
- Test cache hit/miss scenarios

### Milestone 4 Deliverables

- [ ] `DfsRequestType` enum
- [ ] `DfsResolverState` class
- [ ] `DfsPathResolver` with 14-step algorithm
- [ ] Interlink detection
- [ ] Target failover implementation
- [ ] STATUS_PATH_NOT_COVERED handling
- [ ] Comprehensive algorithm tests

### Done Criteria (M4)

- [ ] All 14 steps implemented and unit tested
- [ ] Failover test passes with mocked transport
- [ ] Interlink detection passes with both rules
- [ ] IPC$ short-circuit verified
- [ ] STATUS_PATH_NOT_COVERED integration tested
- [ ] All observability events raise correctly
- [ ] Code compiles on all targets
- [ ] Code review approved

---

## Milestone 5: Integration & Polish

**Goal**: Integrate DFS into SMB client, add advanced features  
**Duration**: 5-7 days  
**Dependencies**: Milestone 4

### Acceptance Criteria (M5)

```gherkin
Given SMB2Client with DFS enabled
When CreateFileDfs called with DFS path
Then file created on resolved target server
And cross-server session established if needed

Given DfsSessionManager with cached session to server A
When GetOrCreateSession called for server A
Then returns cached session (no new connection)

Given DfsClientOptions.Enabled = false
When DFS path accessed
Then no DFS resolution attempted (backward compatible)

Given DfsClientOptions with SiteName configured
When referral requested
Then uses FSCTL_DFS_GET_REFERRALS_EX with site name
```

### 5.1 Cross-Server Session Management

**File**: `SMBLibrary/Client/DFS/DfsSessionManager.cs` (new)

```csharp
public class DfsSessionManager
{
    private readonly ConcurrentDictionary<string, SMB2Client> _sessions;
    
    public SMB2Client GetOrCreateSession(
        string hostname,
        SMB2Client existingClient,  // For credential reuse
        Action<SMB2Client> authenticate);
    
    public void CloseAll();
}
```

### 5.2 Integrate into SMB2Client

**File**: `SMBLibrary/Client/SMB2Client.cs` (modify)

Add DFS-aware wrapper methods:

```csharp
public partial class SMB2Client
{
    private DfsPathResolver _dfsResolver;
    private DfsSessionManager _dfsSessionManager;
    
    public void EnableDfs(DfsClientOptions options);
    
    // DFS-aware file operations
    public NTStatus CreateFileDfs(string path, ...);
    public NTStatus ReadFileDfs(string path, ...);
    // etc.
}
```

### 5.3 Add FSCTL_DFS_GET_REFERRALS_EX Support

**File**: `SMBLibrary/Client/DFS/DfsIoctlRequestBuilder.cs` (modify)

```csharp
internal static IOCtlRequest CreateDfsReferralExRequest(
    string dfsPath,
    string siteName,
    uint maxOutputResponse)
{
    var request = new RequestGetDfsReferralEx
    {
        MaxReferralLevel = 4,
        RequestFlags = DfsReferralRequestFlags.SiteName,
        RequestFileName = dfsPath,
        SiteName = siteName,
    };
    // ... build IOCTL
}
```

### 5.4 Add IPC$ Short-Circuit

Per MS-DFSC, IPC$ should not trigger DFS resolution:

```csharp
// In DfsPathResolver.Step1
if (path.IsIpc)
{
    // IPC$ is never DFS - skip resolution
    return Step12(state);
}
```

### 5.5 Add Configuration Options

**File**: `SMBLibrary/Client/DFS/DfsClientOptions.cs` (enhance)

```csharp
public class DfsClientOptions
{
    public bool Enabled { get; set; } = true;
    public int ReferralCacheTtlSeconds { get; set; } = 300;
    public int DomainCacheTtlSeconds { get; set; } = 600;
    public int MaxRetries { get; set; } = 3;
    public string SiteName { get; set; }  // For site-aware referrals
    public bool UseExtendedReferrals { get; set; } = true;
}
```

### 5.6 Integration Tests

**File**: `SMBLibrary.Tests/DFS/DfsIntegrationTests.cs` (new)

Requires test DFS namespace:

- Test standalone DFS resolution
- Test domain-based DFS resolution
- Test target failover with simulated failures
- Test cross-server file operations
- Test SYSVOL/NETLOGON access

### 5.7 Documentation

**File**: `docs/DFS-Usage.md` (new)

- How to enable DFS
- Configuration options
- Troubleshooting
- Known limitations

### Milestone 5 Deliverables

- [ ] `DfsSessionManager` for cross-server connections
- [ ] SMB2Client DFS integration
- [ ] FSCTL_DFS_GET_REFERRALS_EX support
- [ ] IPC$ short-circuit
- [ ] Enhanced configuration options
- [ ] Integration tests
- [ ] User documentation

### Done Criteria (M5)

- [ ] Integration tests pass with standalone DFS namespace
- [ ] Cross-server session reuse verified
- [ ] Backward compatibility verified (DFS disabled = no behavior change)
- [ ] Documentation reviewed and accurate
- [ ] All observability events documented in usage guide
- [ ] Code compiles on all targets
- [ ] Code review approved
- [ ] End-to-end test with real Windows DFS namespace passes

### Integration Test Environment

**All integration tests are manual** — No automated CI integration tests due to infrastructure requirements.

**Option A: Standalone DFS (primary manual test)**

- Single Windows Server with DFS Namespace role
- Create standalone namespace `\\testserver\dfs`
- Configure folder targets pointing to local shares
- No domain required
- **Use for**: M4/M5 end-to-end validation

**Option B: Mocked transport (automated in CI)**

- Use `IDfsReferralTransport` mock for unit tests
- Pre-recorded referral responses captured from Option A
- Verify algorithm logic without network
- **Use for**: All milestone unit tests

**Option C: Domain DFS (manual testing only)**

- Requires AD domain environment
- Test domain-based paths `\\DOMAIN.COM\DFS`
- Test SYSVOL/NETLOGON access
- **Use for**: Optional validation if domain available

### Test Fixture Capture Strategy

Fixtures will be **captured from Wireshark** during manual testing with Option A:

1. Set up standalone DFS namespace on test server
2. Use Wireshark to capture SMB2 IOCTL responses
3. Extract RESP_GET_DFS_REFERRAL payloads
4. Save as `.bin` files in `Tests/DFS/TestData/`
5. Document each fixture's structure in README

**Fallback**: Hand-craft fixtures from MS-DFSC spec if capture not feasible.

---

## Post-Implementation

### Performance Optimization

- Profile cache operations
- Consider LRU eviction for large caches
- Optimize tree traversal

### Edge Cases

- Handle very deep DFS namespaces
- Handle circular referrals (detection/limit)
- Handle server unavailability during resolution

### Future Enhancements

- DFS-N (namespace) server support
- DFS-R (replication) awareness
- Async resolution API

---

## Summary

| Milestone | Duration | Key Deliverables |
|-----------|----------|------------------|
| **M1: Foundation** | 3-5 days | Enums, DfsPath helper, MaxReferralLevel fix |
| **M2: Structures** | 5-7 days | V1-V4 parsing, NameListReferral, REQ_EX |
| **M3: Caching** | 5-7 days | ReferralCache, DomainCache, target failover |
| **M4: Algorithm** | 7-10 days | 14-step resolver, interlink, failover |
| **M5: Integration** | 5-7 days | SMB2Client integration, cross-server, docs |

**Total estimated duration**: 25-36 days (4-6 weeks)

---

## References

- [MS-DFSC] Distributed File System (DFS): Referral Protocol
- smbj: `c:\dev\smbj-reference\src\main\java\com\hierynomus\`
- smbprotocol: `c:\dev\smbprotocol-reference\src\smbprotocol\dfs.py`
- Analysis docs: `c:\dev\SMBLibrary\docs\dfs-analysis-*.md`
- Spec audit: `c:\dev\SMBLibrary\docs\dfs-spec-audit.md`
