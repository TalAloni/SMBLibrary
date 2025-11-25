# MS-DFSC Specification Compliance Audit

> **Specification**: [MS-DFSC] Distributed File System (DFS): Referral Protocol  
> **Version**: 38.0 (2024)  
> **Audit Date**: 2024-11-24  
> **Target**: SMBLibrary DFS Implementation

---

## Executive Summary

SMBLibrary has **basic DFS structures** but is **significantly incomplete** compared to the MS-DFSC specification. The implementation provides:
- ✅ Request/Response message parsing (partial)
- ✅ V1, V2, V3 referral entry parsing
- ❌ No resolution algorithm
- ❌ No caching
- ❌ No V4 referrals
- ❌ No extended requests (site-aware)

**Compliance Level**: ~20% (message parsing only)

---

## Section 2: Messages

### 2.2.2 REQ_GET_DFS_REFERRAL

| Field | Spec | SMBLibrary | Status |
|-------|------|------------|--------|
| MaxReferralLevel | UINT16 | `ushort MaxReferralLevel` | ✅ |
| RequestFileName | Unicode string | `string RequestFileName` | ✅ |

**Current implementation** (`RequestGetDfsReferral.cs`):
```csharp
public ushort MaxReferralLevel;      // ✅ Implemented
public string RequestFileName;        // ✅ Implemented
```

**Issues**:
- ⚠️ `MaxReferralLevel` defaults to 0 (let server decide) instead of 4 (request V4)

---

### 2.2.3 REQ_GET_DFS_REFERRAL_EX

| Field | Spec | SMBLibrary | Status |
|-------|------|------------|--------|
| MaxReferralLevel | UINT16 | ❌ | Missing |
| RequestFlags | UINT16 | ❌ | Missing |
| RequestDataLength | UINT32 | ❌ | Missing |
| RequestFileName | Unicode string | ❌ | Missing |
| SiteName | Unicode string (optional) | ❌ | Missing |

**Status**: ❌ **Not implemented**

This extended request enables site-aware referrals for geographically distributed DFS.

---

### 2.2.4 RESP_GET_DFS_REFERRAL

| Field | Spec | SMBLibrary | Status |
|-------|------|------------|--------|
| PathConsumed | UINT16 | `ushort PathConsumed` | ✅ |
| NumberOfReferrals | UINT16 | `ushort NumberOfReferrals` | ✅ |
| ReferralHeaderFlags | UINT32 | `uint ReferralHeaderFlags` | ✅ Parsed, ❌ Not used |
| ReferralEntries | Variable | `List<DfsReferralEntry>` | ✅ |
| StringBuffer | Variable | `List<string>` | ✅ |

**Current implementation** (`ResponseGetDfsReferral.cs`):
```csharp
public ushort PathConsumed;           // ✅ Implemented
public ushort NumberOfReferrals;      // ✅ Implemented  
public uint ReferralHeaderFlags;      // ✅ Parsed, ❌ Not interpreted
public List<DfsReferralEntry> ReferralEntries;  // ✅ Implemented
```

**Issues**:
- ❌ `ReferralHeaderFlags` not interpreted (ReferralServers, StorageServers, TargetFailback)

---

### 2.2.4.1 ReferralHeaderFlags

| Flag | Value | Purpose | SMBLibrary |
|------|-------|---------|------------|
| ReferralServers | 0x00000001 | Targets are referral servers | ❌ Not used |
| StorageServers | 0x00000002 | Targets are storage servers | ❌ Not used |
| TargetFailback | 0x00000004 | Client should fail back | ❌ Not used |

**Status**: ❌ **Flags parsed but never used for logic**

---

### 2.2.5.1 DFS_REFERRAL_V1

| Field | Offset | Size | SMBLibrary | Status |
|-------|--------|------|------------|--------|
| VersionNumber | 0 | 2 | ✅ | ✅ |
| Size | 2 | 2 | ✅ | ✅ |
| ServerType | 4 | 2 | ❌ | Missing |
| ReferralEntryFlags | 6 | 2 | ❌ | Missing |
| ShareName | 8 | Variable | `DfsPath` + `NetworkAddress` | ⚠️ Partial |

**Current implementation** (`DfsReferralEntryV1.cs`):
```csharp
public ushort VersionNumber;    // ✅
public ushort Size;             // ✅
public uint TimeToLive;         // ✅ (offset wrong in V1?)
public string DfsPath;          // ✅
public string NetworkAddress;   // ✅
// Missing: ServerType, ReferralEntryFlags
```

**Issues**:
- ❌ Missing `ServerType` field
- ❌ Missing `ReferralEntryFlags` field
- ⚠️ V1 spec says ShareName is inline null-terminated string, not offset-based

---

### 2.2.5.2 DFS_REFERRAL_V2

| Field | Offset | Size | SMBLibrary | Status |
|-------|--------|------|------------|--------|
| VersionNumber | 0 | 2 | ✅ | ✅ |
| Size | 2 | 2 | ✅ | ✅ |
| ServerType | 4 | 2 | ✅ | ✅ |
| ReferralEntryFlags | 6 | 2 | ✅ | ✅ |
| Proximity | 8 | 4 | ❌ | Missing |
| TimeToLive | 12 | 4 | ✅ | ✅ |
| DFSPathOffset | 16 | 2 | ✅ | ✅ |
| DFSAlternatePathOffset | 18 | 2 | ✅ | ✅ |
| NetworkAddressOffset | 20 | 2 | ✅ | ✅ |

**Current implementation** (`DfsReferralEntryV2.cs`):
```csharp
public ushort VersionNumber;        // ✅
public ushort Size;                 // ✅
public uint TimeToLive;             // ✅
public ushort ServerType;           // ✅
public ushort ReferralEntryFlags;   // ✅
public string DfsPath;              // ✅
public string DfsAlternatePath;     // ✅
public string NetworkAddress;       // ✅
// Missing: Proximity
```

**Issues**:
- ❌ Missing `Proximity` field (4 bytes)

---

### 2.2.5.3 DFS_REFERRAL_V3

| Field | Offset | Size | SMBLibrary | Status |
|-------|--------|------|------------|--------|
| VersionNumber | 0 | 2 | ✅ | ✅ |
| Size | 2 | 2 | ✅ | ✅ |
| ServerType | 4 | 2 | ✅ | ✅ |
| ReferralEntryFlags | 6 | 2 | ✅ | ✅ |
| TimeToLive | 8 | 4 | ✅ | ✅ |
| DFSPathOffset | 12 | 2 | ✅ | ✅ |
| DFSAlternatePathOffset | 14 | 2 | ✅ | ✅ |
| NetworkAddressOffset | 16 | 2 | ✅ | ✅ |
| ServiceSiteGuid | 18 | 16 | ❌ | Missing |

**OR for NameListReferral (when ReferralEntryFlags & 0x02):**

| Field | Offset | Size | SMBLibrary | Status |
|-------|--------|------|------------|--------|
| SpecialNameOffset | 12 | 2 | ❌ | Missing |
| NumberOfExpandedNames | 14 | 2 | ❌ | Missing |
| ExpandedNameOffset | 16 | 2 | ❌ | Missing |
| Padding | 18 | 16 | ❌ | Missing |

**Current implementation** (`DfsReferralEntryV3.cs`):
```csharp
public ushort VersionNumber;        // ✅
public ushort Size;                 // ✅
public uint TimeToLive;             // ✅
public ushort ServerType;           // ✅
public ushort ReferralEntryFlags;   // ✅
public string DfsPath;              // ✅
public string DfsAlternatePath;     // ✅
public string NetworkAddress;       // ✅
// Missing: ServiceSiteGuid
// Missing: NameListReferral parsing
```

**Issues**:
- ❌ Missing `ServiceSiteGuid` field (16 bytes)
- ❌ **No NameListReferral support** - Cannot parse DC referral responses

---

### 2.2.5.4 DFS_REFERRAL_V4

| Field | Status |
|-------|--------|
| Same as V3 | ❌ **Not implemented** |
| TargetSetBoundary flag (0x04) | ❌ **Not implemented** |

**Status**: ❌ **V4 not implemented at all**

V4 adds the `TargetSetBoundary` flag for grouping targets into priority sets.

---

### 2.2.5.3.1/2.2.5.4.1 ReferralEntryFlags

| Flag | Value | Purpose | SMBLibrary |
|------|-------|---------|------------|
| NameListReferral | 0x0002 | DC referral response | ❌ Not handled |
| TargetSetBoundary | 0x0004 | V4 target grouping | ❌ Not handled |

**Status**: ❌ **Flags parsed but never used**

---

## Section 3: Protocol Details

### 3.1.1 Abstract Data Model

| Component | Spec Requirement | SMBLibrary | Status |
|-----------|------------------|------------|--------|
| ReferralCache | Required for caching referrals | `Dictionary<string, CachedReferral>` | ⚠️ Basic |
| DomainCache | Required for domain-joined clients | ❌ | Missing |

**ReferralCache Issues**:
- ⚠️ Simple dictionary, not tree-based (inefficient prefix matching)
- ❌ No `TargetHint` tracking
- ❌ No `TargetList` with failover support
- ❌ No `RootOrLink` classification
- ❌ No `Interlink` detection

**DomainCache Issues**:
- ❌ **Completely missing** - Cannot resolve domain-based DFS paths

---

### 3.1.4.1 Sending a DFS Referral Request (14-Step Algorithm)

| Step | Description | SMBLibrary | Status |
|------|-------------|------------|--------|
| 1 | Single component check → skip DFS | ❌ | Missing |
| 2 | ReferralCache lookup | ⚠️ Basic | Partial |
| 3 | Cache hit (root) → replace prefix | ❌ | Missing |
| 4 | Cache hit (link) → check interlink | ❌ | Missing |
| 5 | Cache miss → DomainCache lookup | ❌ | Missing |
| 6 | Send ROOT referral request | ✅ | Works |
| 7 | ROOT referral success → branch | ❌ | Missing |
| 8 | I/O request with resolved path | ✅ | Works |
| 9 | Expired LINK → refresh referral | ❌ | Missing |
| 10 | SYSVOL referral request | ❌ | Missing |
| 11 | Interlink → replace and recurse | ❌ | Missing |
| 12 | Not DFS → return unchanged | ✅ | Works |
| 13 | DC error → fail with error | ❌ | Missing |
| 14 | DFS error → fail with error | ❌ | Missing |

**Status**: ❌ **No algorithm implementation** - Only basic linear request/response

---

### 3.1.5.2 I/O Operation to Target Failure (Failover)

| Requirement | SMBLibrary | Status |
|-------------|------------|--------|
| Try current TargetHint | ❌ | Missing |
| On failure, call NextTargetHint() | ❌ | Missing |
| Retry with new target | ❌ | Missing |
| If all exhausted, fail | ❌ | Missing |

**Status**: ❌ **No failover support**

---

### 3.1.5.4.5 Determining Whether a Referral Response is an Interlink

| Rule | SMBLibrary | Status |
|------|------------|--------|
| ReferralServers=1 AND StorageServers=0 | ❌ | Not checked |
| Single target AND first component in DomainCache | ❌ | Not checked |

**Status**: ❌ **No interlink detection**

---

## FSCTL Codes

| Code | Value | SMBLibrary | Status |
|------|-------|------------|--------|
| FSCTL_DFS_GET_REFERRALS | 0x00060194 | ✅ `IoControlCode.FSCTL_DFS_GET_REFERRALS` | ✅ |
| FSCTL_DFS_GET_REFERRALS_EX | 0x000601B0 | ❌ | Missing |

---

## Gap Summary

### Critical Gaps (High Priority)

| # | Gap | Impact |
|---|-----|--------|
| 1 | **No DomainCache** | Cannot resolve domain-based DFS (e.g., `\\DOMAIN.COM\DFS`) |
| 2 | **No 14-step algorithm** | Resolution logic is incomplete |
| 3 | **No target failover** | Single target failure = total failure |
| 4 | **No NameListReferral parsing** | Cannot discover DCs from referrals |
| 5 | **MaxReferralLevel=0** | Doesn't request V4 referrals |

### Major Gaps (Medium Priority)

| # | Gap | Impact |
|---|-----|--------|
| 6 | **No V4 referral support** | Missing TargetSetBoundary for priority failover |
| 7 | **No interlink detection** | Cross-namespace DFS fails |
| 8 | **No SYSVOL/NETLOGON handling** | AD environments affected |
| 9 | **ReferralHeaderFlags not used** | TargetFailback ignored |
| 10 | **No REQ_GET_DFS_REFERRAL_EX** | No site-aware referrals |

### Minor Gaps (Low Priority)

| # | Gap | Impact |
|---|-----|--------|
| 11 | V1 missing ServerType/ReferralEntryFlags | Edge case |
| 12 | V2 missing Proximity | Edge case |
| 13 | V3 missing ServiceSiteGuid | Edge case |
| 14 | No DfsPath helper class | Code quality |
| 15 | No tree-based cache | Performance |

---

## Compliance Checklist

### Section 2: Messages

- [x] REQ_GET_DFS_REFERRAL structure
- [ ] REQ_GET_DFS_REFERRAL_EX structure
- [x] RESP_GET_DFS_REFERRAL structure
- [ ] ReferralHeaderFlags interpretation
- [x] DFS_REFERRAL_V1 (partial)
- [x] DFS_REFERRAL_V2 (partial)
- [x] DFS_REFERRAL_V3 (partial)
- [ ] DFS_REFERRAL_V4
- [ ] NameListReferral parsing
- [ ] TargetSetBoundary handling

### Section 3: Protocol Details

- [ ] ReferralCache with proper structure
- [ ] DomainCache
- [ ] 14-step resolution algorithm
- [ ] Target failover mechanism
- [ ] Interlink detection
- [ ] SYSVOL/NETLOGON handling
- [ ] Cross-server session management
- [ ] STATUS_PATH_NOT_COVERED handling

---

## Recommendations

### Phase 1: Core Infrastructure

1. **Add DomainCache** class with DC hint tracking
2. **Add V4 referral entry** with TargetSetBoundary
3. **Change MaxReferralLevel to 4** (trivial fix)
4. **Add NameListReferral parsing** to V3/V4

### Phase 2: Algorithm Implementation

5. **Create DfsPath helper** class for path manipulation
6. **Implement 14-step algorithm** in resolver
7. **Add target failover** with NextTargetHint()
8. **Add interlink detection** per spec

### Phase 3: Advanced Features

9. **Add REQ_GET_DFS_REFERRAL_EX** for site awareness
10. **Implement cross-server sessions** for DFS targets
11. **Add tree-based cache** for prefix matching
12. **Handle SYSVOL/NETLOGON** special cases

---

## References

- [MS-DFSC] Distributed File System (DFS): Referral Protocol
- SMBLibrary source: `SMBLibrary/DFS/` and `SMBLibrary/Client/DFS/`
