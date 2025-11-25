# DFS Implementation Analysis: libsmb2 (C)

> **Repository**: https://github.com/sahlberg/libsmb2  
> **Language**: C  
> **License**: LGPL 2.1  
> **Analysis Date**: 2024-11-24

---

## Summary

**libsmb2** is a lightweight, portable SMB2/3 client library in C. It defines DFS-related **constants and flags** but does **not implement DFS referral handling**. The library is designed for embedded systems and simple file operations where DFS complexity is not required.

---

## DFS Support Level: ❌ Constants Only

| Feature | Status | Notes |
|---------|--------|-------|
| DFS Capability Flag | ✅ | `SMB2_GLOBAL_CAP_DFS` |
| DFS Operations Flag | ✅ | `SMB2_FLAGS_DFS_OPERATIONS` |
| DFS Share Flags | ✅ | `SMB2_SHAREFLAG_DFS`, `SMB2_SHAREFLAG_DFS_ROOT` |
| DFS Share Capability | ✅ | `SMB2_SHARE_CAP_DFS` |
| FSCTL Codes | ✅ | `SMB2_FSCTL_DFS_GET_REFERRALS`, `SMB2_FSCTL_DFS_GET_REFERRALS_EX` |
| DFS Referral Request | ❌ | Not implemented |
| DFS Referral Response | ❌ | Not implemented |
| DFS Resolution | ❌ | Not implemented |
| Referral Cache | ❌ | Not implemented |

---

## File Structure

```text
include/smb2/
├── smb2.h              # Main header with DFS constants
├── smb2-ioctl.h        # IOCTL definitions
└── smb2-errors.h       # Error codes

lib/
├── smb2-cmd-*.c        # Command implementations (no DFS)
└── ...
```

---

## DFS-Related Definitions

### SMB2 Header Flags (`smb2.h`)

```c
#define SMB2_FLAGS_SERVER_TO_REDIR    0x00000001
#define SMB2_FLAGS_ASYNC_COMMAND      0x00000002
#define SMB2_FLAGS_RELATED_OPERATIONS 0x00000004
#define SMB2_FLAGS_SIGNED             0x00000008
#define SMB2_FLAGS_PRIORITY_MASK      0x00000070
#define SMB2_FLAGS_DFS_OPERATIONS     0x10000000  // ← DFS flag
#define SMB2_FLAGS_REPLAY_OPERATION   0x20000000
```

### Negotiate Capabilities

```c
#define SMB2_GLOBAL_CAP_DFS                0x00000001  // ← DFS capability
#define SMB2_GLOBAL_CAP_LEASING            0x00000002
#define SMB2_GLOBAL_CAP_LARGE_MTU          0x00000004
#define SMB2_GLOBAL_CAP_MULTI_CHANNEL      0x00000008
#define SMB2_GLOBAL_CAP_PERSISTENT_HANDLES 0x00000010
#define SMB2_GLOBAL_CAP_DIRECTORY_LEASING  0x00000020
#define SMB2_GLOBAL_CAP_ENCRYPTION         0x00000040
```

### Share Flags

```c
#define SMB2_SHAREFLAG_DFS                         0x00000001  // ← DFS share
#define SMB2_SHAREFLAG_DFS_ROOT                    0x00000002  // ← DFS root
#define SMB2_SHAREFLAG_MANUAL_CACHING              0x00000000
#define SMB2_SHAREFLAG_AUTO_CACHING                0x00000010
// ... more flags
```

### Share Capabilities

```c
#define SMB2_SHARE_CAP_DFS                         0x00000008  // ← DFS capable
#define SMB2_SHARE_CAP_CONTINUOUS_AVAILABILITY     0x00000010
#define SMB2_SHARE_CAP_SCALEOUT                    0x00000020
#define SMB2_SHARE_CAP_CLUSTER                     0x00000040
#define SMB2_SHARE_CAP_ASYMMETRIC                  0x00000080
```

### FSCTL Codes

```c
#define SMB2_FSCTL_DFS_GET_REFERRALS            0x00060194  // ← DFS referral IOCTL
#define SMB2_FSCTL_PIPE_PEEK                    0x0011400C
#define SMB2_FSCTL_PIPE_WAIT                    0x00110018
// ...
#define SMB2_FSCTL_DFS_GET_REFERRALS_EX         0x000601B0  // ← Extended referral
#define SMB2_FSCTL_FILE_LEVEL_TRIM              0x00098208
```

---

## What's Missing

### No Referral Structures

The library does not define:
- `REQ_GET_DFS_REFERRAL` request structure
- `REQ_GET_DFS_REFERRAL_EX` request structure  
- `RESP_GET_DFS_REFERRAL` response structure
- `DFS_REFERRAL_V1/V2/V3/V4` entry structures

### No Resolution Logic

The library does not implement:
- DFS path detection
- Referral request/response handling
- Path resolution algorithm
- Caching mechanisms
- Target failover

### No DFS Flag Usage

While `SMB2_FLAGS_DFS_OPERATIONS` is defined, it's not used in the codebase to set the flag on requests or handle DFS-specific responses.

---

## Design Philosophy

libsmb2 is designed for:

1. **Embedded systems** (Xbox, PlayStation, ESP32, Raspberry Pi Pico)
2. **Simple file operations** (open, read, write, close)
3. **Minimal dependencies** (no complex caching or state machines)
4. **Portability** (works on many platforms with minimal OS requirements)

DFS adds significant complexity that conflicts with these goals:
- Requires caching infrastructure
- Requires cross-server session management
- Requires multi-step resolution algorithm
- Requires additional memory for cache entries

---

## Potential for DFS Addition

If DFS were to be added to libsmb2, it would require:

1. **New structures** for referral request/response
2. **New parsing code** for referral entries
3. **Simple cache** (perhaps just a single entry per connection)
4. **Path prefix replacement** logic
5. **Reconnection logic** for different servers

Given the library's embedded focus, a minimal implementation might:
- Support only standalone DFS (not domain-based)
- Use a single-entry cache (last referral only)
- Skip domain cache entirely
- Not implement the full 14-step algorithm

---

## Relevance to SMBLibrary

**Low relevance** for DFS implementation patterns. libsmb2 is useful as a reference for:
- FSCTL code definitions
- Share flag definitions
- Capability flag definitions

But provides no DFS resolution logic to learn from.

---

## References

- Source: `c:\dev\libsmb2-reference\include\smb2\smb2.h`
- IOCTL header: `c:\dev\libsmb2-reference\include\smb2\smb2-ioctl.h`
