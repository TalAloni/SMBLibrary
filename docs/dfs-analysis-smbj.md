# DFS Implementation Analysis: smbj (Java)

> **Repository**: https://github.com/hierynomus/smbj  
> **Language**: Java  
> **License**: Apache 2.0  
> **Analysis Date**: 2024-11-24

---

## Summary

**smbj** provides a **complete, production-grade DFS client implementation** following [MS-DFSC]. It implements the full 14-step resolution algorithm, maintains both Referral and Domain caches, handles V1-V4 referrals, and supports cross-server session management. This is the most complete open-source DFS implementation reviewed.

---

## DFS Support Level: ✅ Full

| Feature | Status | Notes |
|---------|--------|-------|
| DFS Referral Request | ✅ | `FSCTL_DFS_GET_REFERRALS` |
| DFS Referral Request EX | ✅ | `FSCTL_DFS_GET_REFERRALS_EX` |
| V1 Referrals | ✅ | `DFSReferralV1` |
| V2 Referrals | ✅ | `DFSReferralV2` |
| V3/V4 Referrals | ✅ | `DFSReferralV34` (combined) |
| Referral Cache | ✅ | Tree-based, TTL-aware |
| Domain Cache | ✅ | DC discovery support |
| 14-Step Algorithm | ✅ | Complete implementation |
| Target Failover | ✅ | `nextTargetHint()` |
| Interlink Detection | ✅ | Per MS-DFSC §3.1.5.4.5 |
| SYSVOL/NETLOGON | ✅ | Special handling |
| Cross-Server Sessions | ✅ | Automatic reconnection |
| NameListReferral | ✅ | DC referral parsing |

---

## File Structure

```
src/main/java/com/hierynomus/msdfsc/
├── DFSException.java           # DFS-specific exception
├── DFSPath.java                # Path parsing/manipulation helper
├── DomainCache.java            # Domain name → DC mapping cache
├── ReferralCache.java          # Tree-based referral cache
└── messages/
    ├── DFSReferral.java            # Base referral class
    ├── DFSReferralV1.java          # Version 1 referral
    ├── DFSReferralV2.java          # Version 2 referral
    ├── DFSReferralV34.java         # Version 3/4 referrals (combined)
    ├── SMB2GetDFSReferralRequest.java
    ├── SMB2GetDFSReferralExRequest.java
    └── SMB2GetDFSReferralResponse.java

src/main/java/com/hierynomus/smbj/paths/
└── DFSPathResolver.java        # 14-step resolution algorithm
```

---

## Key Implementation Details

### 1. DFSPathResolver (14-Step Algorithm)

The `DFSPathResolver` class implements the complete [MS-DFSC] §3.1.4.1 algorithm as individual methods:

```java
// Steps map to private methods:
step1()  → Single component check, IPC$ short-circuit
step2()  → ReferralCache lookup
step3()  → Cache hit: replace prefix with target
step4()  → Link referral: check SYSVOL/NETLOGON, interlink
step5()  → Cache miss: DomainCache lookup, DC referral
step6()  → ROOT referral request
step7()  → ROOT referral success: branch to step 3 or 4
step8()  → I/O operation with resolved path
step9()  → Expired LINK: refresh referral
step10() → SYSVOL referral request
step11() → Interlink: replace prefix and recurse (→ step 2)
step12() → Not DFS: return unchanged
step13() → DC error: fail
step14() → DFS error: fail
```

**Key code pattern** (step 3 - target failover):

```java
private <T> T step3(Session session, ResolveState<T> state, 
                    ReferralCacheEntry lookup) {
    ReferralCache.TargetSetEntry target = lookup.getTargetHint();
    SMBApiException lastException = null;
    DFSPath initialPath = state.path;
    
    while (target != null) {
        try {
            state.path = state.path.replacePrefix(
                lookup.getDfsPathPrefix(), 
                lookup.getTargetHint().getTargetPath());
            state.isDFSPath = true;
            return step8(session, state, lookup);
        } catch (SMBApiException e) {
            lastException = e;
            if (e.getStatusCode() != NtStatus.STATUS_PATH_NOT_COVERED.getValue()) {
                target = lookup.nextTargetHint();  // Failover!
                state.path = initialPath;
            }
        }
    }
    // ... error handling
}
```

### 2. ReferralCache (Tree-Based)

Uses a tree structure for efficient longest-prefix matching:

```java
public class ReferralCache {
    private ReferralCacheNode cacheRoot = new ReferralCacheNode("<root>");
    
    static class ReferralCacheNode {
        private final String pathComponent;
        private final Map<String, ReferralCacheNode> childNodes;
        private volatile ReferralCacheEntry entry;
        
        ReferralCacheEntry getReferralEntry(Iterator<String> pathComponents) {
            if (pathComponents.hasNext()) {
                String component = pathComponents.next().toLowerCase();
                ReferralCacheNode child = childNodes.get(component);
                if (child != null) {
                    return child.getReferralEntry(pathComponents);
                }
            }
            return this.entry;  // Return deepest match
        }
    }
}
```

**ReferralCacheEntry** tracks:
- `dfsPathPrefix` - The DFS path this entry covers
- `rootOrLink` - ServerType (ROOT vs LINK)
- `interlink` - Whether target is in another namespace
- `ttl` / `expires` - Time-to-live tracking
- `targetFailback` - V4 failback flag
- `targetHint` - Current target index
- `targetList` - List of `TargetSetEntry`

### 3. DomainCache

Simple map-based cache for domain → DC mappings:

```java
public class DomainCache {
    private Map<String, DomainCacheEntry> cache = new ConcurrentHashMap<>();
    
    public static class DomainCacheEntry {
        String domainName;
        String DCHint;        // Last successful DC
        List<String> DCList;  // All known DCs
    }
}
```

Populated from **NameListReferral** responses (DC referrals).

### 4. DFSPath Helper

Utility class for DFS path manipulation:

```java
public class DFSPath {
    private final List<String> pathComponents;
    
    public DFSPath replacePrefix(String prefixToReplace, DFSPath target);
    public boolean hasOnlyOnePathComponent();
    public boolean isSysVolOrNetLogon();
    public boolean isIpc();
    public String toPath();
}
```

### 5. Interlink Detection

Per MS-DFSC §3.1.5.4.5:

```java
// In ReferralCacheEntry constructor:
boolean interlink = response.getReferralHeaderFlags()
    .contains(ReferralHeaderFlags.ReferralServers)
    && !response.getReferralHeaderFlags()
    .contains(ReferralHeaderFlags.StorageServers);

if (!interlink && referralEntries.size() == 1) {
    List<String> pathEntries = new DFSPath(firstReferral.getPath())
        .getPathComponents();
    interlink = (domainCache.lookup(pathEntries.get(0)) != null);
}
```

### 6. Cross-Server Session Management

Automatically connects to different servers when DFS target differs:

```java
private ReferralResult sendDfsReferralRequest(DfsRequestType type, 
        String hostName, Session session, DFSPath path) {
    Session dfsSession = session;
    
    if (!hostName.equals(session.getConnection().getRemoteHostname())) {
        AuthenticationContext auth = session.getAuthenticationContext();
        Connection connection = oldConnection.getClient().connect(hostName);
        dfsSession = connection.authenticate(auth);
    }
    
    Share dfsShare = dfsSession.connectShare("IPC$");
    return getReferral(type, dfsShare, path);
}
```

### 7. V3/V4 Referral Parsing with NameListReferral

```java
// DFSReferralV34.readReferral()
if (!isSet(referralEntryFlags, ReferralEntryFlags.NameListReferral)) {
    // Normal referral: DFSPath, DFSAlternatePath, NetworkAddress
    dfsPath = readOffsettedString(buffer, referralStartPos, buffer.readUInt16());
    dfsAlternatePath = readOffsettedString(buffer, referralStartPos, buffer.readUInt16());
    path = readOffsettedString(buffer, referralStartPos, buffer.readUInt16());
} else {
    // NameListReferral (DC referral): SpecialName + ExpandedNames list
    specialName = readOffsettedString(buffer, referralStartPos, buffer.readUInt16());
    int nrNames = buffer.readUInt16();
    int firstExpandedNameOffset = buffer.readUInt16();
    expandedNames = new ArrayList<>(nrNames);
    // ... read null-terminated strings
}
```

---

## Request Type Classification

```java
private enum DfsRequestType {
    DOMAIN,  // Domain referral (list trusted domains)
    DC,      // DC referral (list DCs for domain)
    SYSVOL,  // SYSVOL/NETLOGON referral
    ROOT,    // DFS root referral
    LINK     // DFS link referral
}
```

Each type has specific:
- Target server selection
- Response handling
- Cache population behavior

---

## Constants

```java
private static final long FSCTL_DFS_GET_REFERRALS = 0x00060194L;
private static final long FSCTL_DFS_GET_REFERRALS_EX = 0x000601B0L;

// In SMB2GetDFSReferralRequest
public static final int MAX_REFERRAL_LEVEL = 4;  // Request V4 referrals
```

---

## Test Coverage

- `DFSTest.groovy` - Unit tests for referral parsing
- `SMB2GetDFSReferralResponseTest.groovy` - Response parsing tests
- `DFSPathTest.java` - Path manipulation tests
- `DfsIntegrationTest.java` - Integration tests

---

## Patterns to Adopt for SMBLibrary

1. **Tree-based ReferralCache** for efficient prefix matching
2. **14-step algorithm as explicit methods** for clarity and debugging
3. **DFSPath helper class** for path manipulation
4. **DomainCache** with DC hint tracking
5. **NameListReferral parsing** for DC discovery
6. **Interlink detection** per spec
7. **Cross-server session management** with credential reuse
8. **Request type classification** for proper handling
9. **Target failover with `nextTargetHint()`**
10. **MaxReferralLevel = 4** to request V4 referrals

---

## References

- Source: `c:\dev\smbj-reference\src\main\java\com\hierynomus\msdfsc\`
- Main resolver: `c:\dev\smbj-reference\src\main\java\com\hierynomus\smbj\paths\DFSPathResolver.java`
