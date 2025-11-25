# DFS Implementation Tasks

> **Source**: `dfs-implementation-plan.md`  
> **Created**: 2024-11-24  

---

## Milestone 1: Foundation (3-5 days)

| ID | Task | Est. | File |
|----|------|------|------|
| M1-T1 | Fix MaxReferralLevel = 4 | 15m | `Client/DFS/DfsIoctlRequestBuilder.cs` |
| M1-T2 | Add `DfsReferralHeaderFlags` enum | 30m | `DFS/DfsReferralHeaderFlags.cs` (new) |
| M1-T3 | Add `DfsReferralEntryFlags` enum | 30m | `DFS/DfsReferralEntryFlags.cs` (new) |
| M1-T4 | Add `DfsServerType` enum | 20m | `DFS/DfsServerType.cs` (new) |
| M1-T5 | Create `DfsPath` helper class | 2-3h | `Client/DFS/DfsPath.cs` (new) |
| M1-T6 | Write DfsPath unit tests | 2h | `Tests/DFS/DfsPathTests.cs` (new) |
| M1-T7 | Milestone verification & PR | 1h | — |

### M1-T5 Details: DfsPath

```
- [ ] Constructor DfsPath(string uncPath)
- [ ] SplitPath(string) private method
- [ ] PathComponents property (List<string>)
- [ ] HasOnlyOneComponent property
- [ ] IsSysVolOrNetLogon property
- [ ] IsIpc property
- [ ] ReplacePrefix(string, DfsPath) method
- [ ] ToUncPath() method
```

### M1-T6 Details: DfsPath Tests

```
- [ ] ParseUncPath_ValidPath_ReturnsComponents
- [ ] ParseUncPath_LeadingSlashes_HandledCorrectly
- [ ] IsSysVolOrNetLogon_SysvolPath_ReturnsTrue
- [ ] IsSysVolOrNetLogon_NetlogonPath_ReturnsTrue
- [ ] IsIpc_IpcPath_ReturnsTrue
- [ ] ReplacePrefix_ValidPrefix_ReplacesCorrectly
- [ ] ToUncPath_ReturnsValidUncString
```

---

## Milestone 2: Referral Structures (5-7 days)

| ID | Task | Est. | File |
|----|------|------|------|
| M2-T1 | Create test fixtures directory | 30m | `Tests/DFS/TestData/` (new) |
| M2-T2 | Fix V1 structure (add ServerType, Flags) | 1-2h | `DFS/DfsReferralEntryV1.cs` |
| M2-T3 | Fix V2 structure (add Proximity) | 1-2h | `DFS/DfsReferralEntryV2.cs` |
| M2-T4 | Enhance V3 (NameListReferral, ServiceSiteGuid) | 3-4h | `DFS/DfsReferralEntryV3.cs` |
| M2-T5 | Add V4 structure (TargetSetBoundary) | 1h | `DFS/DfsReferralEntryV4.cs` (new) |
| M2-T6 | Update ResponseGetDfsReferral parser | 3-4h | `DFS/ResponseGetDfsReferral.cs` |
| M2-T7 | Add RequestGetDfsReferralEx | 2h | `DFS/RequestGetDfsReferralEx.cs` (new) |
| M2-T8 | Capture test fixtures via Wireshark | 2-3h | `Tests/DFS/TestData/*.bin` |
| M2-T9 | Write referral parsing tests | 3-4h | `Tests/DFS/DfsReferralParsingTests.cs` |
| M2-T10 | Milestone verification & PR | 1h | — |

### M2-T4 Details: V3 Enhancement

```
- [ ] Add ServiceSiteGuid (Guid, 16 bytes)
- [ ] Add SpecialName (string, for NameListReferral)
- [ ] Add ExpandedNames (List<string>, DC list)
- [ ] Add IsNameListReferral property
- [ ] Handle NameListReferral parsing branch
```

### Test Fixtures (M2-T8)

**Capture via Wireshark** from standalone DFS test server:

| File | Description | Capture Method |
|------|-------------|----------------|
| `V1ReferralResponse.bin` | Basic V1 referral | Wireshark SMB2 IOCTL |
| `V2ReferralResponse.bin` | V2 with Proximity | Wireshark SMB2 IOCTL |
| `V3ReferralResponse.bin` | V3 normal referral | Wireshark SMB2 IOCTL |
| `V3NameListReferral.bin` | V3 DC referral | Domain env or hand-craft |
| `V4ReferralResponse.bin` | V4 with TargetSetBoundary | Wireshark SMB2 IOCTL |
| `V4MultiTarget.bin` | V4 with multiple targets | Wireshark SMB2 IOCTL |

**Fallback**: Hand-craft from MS-DFSC spec if capture not feasible.

---

## Milestone 3: Caching (5-7 days)

| ID | Task | Est. | File |
|----|------|------|------|
| M3-T1 | Create `TargetSetEntry` class | 1h | `Client/DFS/TargetSetEntry.cs` (new) |
| M3-T2 | Create `ReferralCacheEntry` class | 3-4h | `Client/DFS/ReferralCacheEntry.cs` (new) |
| M3-T3 | Create `ReferralCacheNode` (tree node) | 2-3h | `Client/DFS/ReferralCache.cs` (internal) |
| M3-T4 | Create `ReferralCache` class | 2-3h | `Client/DFS/ReferralCache.cs` (new) |
| M3-T5 | Create `DomainCacheEntry` class | 2h | `Client/DFS/DomainCacheEntry.cs` (new) |
| M3-T6 | Create `DomainCache` class | 1-2h | `Client/DFS/DomainCache.cs` (new) |
| M3-T7 | Write ReferralCache tests | 3h | `Tests/DFS/ReferralCacheTests.cs` |
| M3-T8 | Write DomainCache tests | 2h | `Tests/DFS/DomainCacheTests.cs` |
| M3-T9 | Write ReferralCacheEntry tests | 2h | `Tests/DFS/ReferralCacheEntryTests.cs` |
| M3-T10 | Milestone verification & PR | 2h | — |

### M3-T2 Details: ReferralCacheEntry

```
- [ ] Constructor from ResponseGetDfsReferral
- [ ] DfsPathPrefix, RootOrLink, IsInterlink properties
- [ ] TtlSeconds, ExpiresUtc, TargetFailback properties
- [ ] TargetList property (List<TargetSetEntry>)
- [ ] IsExpired, IsRoot, IsLink properties
- [ ] GetTargetHint() method
- [ ] NextTargetHint() with Interlocked.CompareExchange
- [ ] ResetTargetHint() method
```

### M3-T7 Details: ReferralCache Tests

```
- [ ] Lookup_ExactMatch_ReturnsEntry
- [ ] Lookup_PrefixMatch_ReturnsLongestPrefix
- [ ] Lookup_NoMatch_ReturnsNull
- [ ] ClearExpired_RemovesOnlyExpired
- [ ] ConcurrentAccess_NoRaceConditions
```

---

## Milestone 4: Resolution Algorithm (7-10 days)

| ID | Task | Est. | File |
|----|------|------|------|
| M4-T1 | Add `DfsRequestType` enum | 20m | `Client/DFS/DfsRequestType.cs` (new) |
| M4-T2 | Create `DfsResolverState<T>` class | 1h | `Client/DFS/DfsResolverState.cs` (new) |
| M4-T3 | Create `DfsException` class | 30m | `Client/DFS/DfsException.cs` (new) |
| M4-T4 | Create DFS event args classes | 1-2h | `Client/DFS/DfsEvents.cs` (new) |
| M4-T5 | Create DfsPathResolver - structure | 1h | `Client/DFS/DfsPathResolver.cs` (new) |
| M4-T6 | Implement Step 1 (single component) | 1h | ↑ |
| M4-T7 | Implement Step 2 (cache lookup) | 1h | ↑ |
| M4-T8 | Implement Steps 3-4 (cache hit + failover) | 2-3h | ↑ |
| M4-T9 | Implement Steps 5-7 (cache miss) | 3-4h | ↑ |
| M4-T10 | Implement Step 8 (I/O operation) | 1-2h | ↑ |
| M4-T11 | Implement Steps 9-11 (special cases) | 2-3h | ↑ |
| M4-T12 | Implement Steps 12-14 (terminal states) | 1h | ↑ |
| M4-T13 | Implement IsInterlink detection | 1h | ↑ |
| M4-T14 | Implement SendReferralRequest helper | 2h | ↑ |
| M4-T15 | Write DfsPathResolver tests | 4-5h | `Tests/DFS/DfsPathResolverTests.cs` |
| M4-T16 | Milestone verification & PR | 2h | — |

### 14-Step Algorithm Summary

| Step | Purpose | Branch To |
|------|---------|-----------|
| 1 | Single component / IPC$ check | 12 or 2 |
| 2 | ReferralCache lookup | 3/4 or 5 |
| 3 | ROOT cache hit → replace prefix | 8 |
| 4 | LINK cache hit → check interlink | 8 or 11 |
| 5 | DomainCache lookup | 6 or 10 |
| 6 | Send ROOT referral request | 7 |
| 7 | ROOT referral success → cache | 3 or 4 |
| 8 | Execute I/O operation | Done |
| 9 | Expired LINK → refresh | 6 |
| 10 | SYSVOL referral request | 7 |
| 11 | Interlink → recurse | 2 |
| 12 | Not DFS → return unchanged | Done |
| 13 | DC error → fail | Error |
| 14 | DFS error → fail | Error |

---

## Milestone 5: Integration (5-7 days)

| ID | Task | Est. | File |
|----|------|------|------|
| M5-T1 | Create `DfsSessionManager` | 2-3h | `Client/DFS/DfsSessionManager.cs` (new) |
| M5-T2 | Update `DfsClientOptions` flags | 1h | `Client/DFS/DfsClientOptions.cs` |
| M5-T3 | Add FSCTL_DFS_GET_REFERRALS_EX | 1-2h | `Client/DFS/DfsIoctlRequestBuilder.cs` |
| M5-T4 | Add DFS integration to SMB2Client | 3-4h | `Client/SMB2Client.cs` |
| M5-T5 | Add DFS-aware file operation wrappers | 2-3h | `Client/SMB2Client.cs` |
| M5-T6 | Write DfsSessionManager tests | 2h | `Tests/DFS/DfsSessionManagerTests.cs` |
| M5-T7 | Write integration tests | 3-4h | `Tests/DFS/DfsIntegrationTests.cs` |
| M5-T8 | Write DFS-Usage.md documentation | 2-3h | `docs/DFS-Usage.md` (new) |
| M5-T9 | Milestone verification & PR | 2h | — |

### M5-T4 Details: SMB2Client Integration

```
- [ ] Add m_dfsResolver field (nullable)
- [ ] Add m_dfsSessionManager field (nullable)
- [ ] Add EnableDfs(DfsClientOptions) method
- [ ] Add DisableDfs() method
- [ ] Hook STATUS_PATH_NOT_COVERED handling
```

### M5-T5 Details: DFS-Aware Wrappers

```
- [ ] CreateFileDfs(path, ...) → resolves path, then CreateFile
- [ ] OpenFileDfs(path, ...) → resolves path, then OpenFile
- [ ] ReadFileDfs(...) → uses resolved handle
- [ ] Handle cross-server session via DfsSessionManager
```

### M5-T8 Details: Documentation

```
- [ ] How to enable DFS (code example)
- [ ] Configuration options table
- [ ] Feature flags explanation
- [ ] Troubleshooting common issues
- [ ] Known limitations section
- [ ] Event logging for diagnostics
```

---

## Task Summary

| Milestone | Tasks | Est. Total |
|-----------|-------|------------|
| M1: Foundation | 7 tasks | 3-5 days |
| M2: Structures | 10 tasks | 5-7 days |
| M3: Caching | 10 tasks | 5-7 days |
| M4: Algorithm | 16 tasks | 7-10 days |
| M5: Integration | 9 tasks | 5-7 days |
| **Total** | **52 tasks** | **25-36 days** |

---

## Development Order

Start each milestone after the previous is complete:

```
M1-T1 → M1-T2 → M1-T3 → M1-T4 → M1-T5 → M1-T6 → M1-T7
    ↓
M2-T1 → M2-T2..T5 (parallel) → M2-T6 → M2-T7 → M2-T8 → M2-T9 → M2-T10
    ↓
M3-T1 → M3-T2 → M3-T3 → M3-T4 → M3-T5 → M3-T6 → M3-T7..T9 (parallel) → M3-T10
    ↓
M4-T1..T4 (parallel) → M4-T5 → M4-T6..T14 (sequential) → M4-T15 → M4-T16
    ↓
M5-T1..T3 (parallel) → M5-T4 → M5-T5 → M5-T6..T8 (parallel) → M5-T9
```
