# DFS Client Design (SMBLibrary vNext-DFS)

## 1. Goals & Non-goals

**Goals**
- Enable SMBLibrary’s `SMBClient` implementations (SMB1 and SMB2/3) to be **DFS-aware**:
  - Issue DFS referral requests (SMB2 IOCTL / SMB1 TRANS2).
  - Parse DFS referral responses using the existing `SMBLibrary.DFS` structures.
  - Select targets and reconnect transparently, when DFS is enabled.
- Keep DFS client behavior **isolated** and **feature-flag controlled** so existing consumers are unaffected by default.
- Design the DFS client logic so it can later interoperate with a minimal DFS server implementation in this repo, without coupling them too tightly.

**Non-goals (vNext-DFS)**
- Implement a full DFS namespace management story (DFSNM): no AD integration, no admin surface.
- Implement sophisticated target selection or health-based routing; we start with simple priority/order semantics from the referral.
- Change existing SMBClient public APIs beyond what is necessary to opt into DFS behavior.
- Implement DFS server features beyond what is captured in `docs/dfs-enablement-plan.md` (that work is tracked separately).

---

## 2. High-level Architecture

### 2.1 Existing components

- **Core clients** (already in `SMBLibrary/Client`):
  - `SMB2Client` / `SMB1Client` – handle protocol negotiation, session setup, tree connects, IOCTL/TRANS2, file operations.
  - `ISMBClient` / `ISMBFileStore` – public contracts for client operations.
- **DFS codec** (already in `SMBLibrary/DFS`):
  - `RequestGetDfsReferral` – request structure for DFS referrals.
  - `ResponseGetDfsReferral` – response structure for DFS referrals.
  - `DfsReferralEntry` – per-target referral entry.

These types provide the wire format parsing we need on the client side; vNext-DFS will **reuse** them instead of creating a second codec.

### 2.2 New conceptual components (client-side)

*(Names are design-time; exact type names/namespaces can be finalized in the SPEC.)*

- **`DfsClientOptions`** (configuration/flags)
  - Holds client-level DFS settings, for example:
    - `Enabled` – global on/off for DFS behavior.
    - Per-connection options such as "treat \`\\domain\\namespace\\*\` as DFS" vs explicit opt-in.
  - Designed to be injectable/configurable without breaking existing callers.

- **`IDfsClientResolver`** (abstraction)
  - Core responsibility: given a UNC path and connection context, decide whether DFS is applicable and, if so, return a resolved target.
  - Example responsibilities:
    - Build and send DFS referral requests (via SMB1/SMB2 operations on the current connection).
    - Use `ResponseGetDfsReferral`/`DfsReferralEntry` to parse results.
    - Select the best target (respecting priority/order from the referral).
    - Return the rewritten UNC and any metadata (TTL, path consumed).

- **`DfsClientResolver`** (initial implementation)
  - Uses `SMB2Client` / `SMB1Client` primitives to send `FSCTL_DFS_GET_REFERRALS{,_EX}` / `TRANS2_GET_DFS_REFERRAL`.
  - Uses `SMBLibrary.DFS` types to parse the referral buffer.
  - Applies simple target-selection rules (first available by priority, then order).

- **`DfsAwareClientAdapter`** (integration layer)
  - A thin layer that coordinates between the **existing** clients and the resolver.
  - Responsibilities:
    - Detect when a path should be passed through the resolver (based on config or error codes such as `STATUS_PATH_NOT_COVERED`).
    - Delegate to `IDfsClientResolver` to obtain the target path.
    - Establish/refresh the underlying connection to the target server/share.
    - Forward the original operation (e.g., open, list directory) on the resolved connection.

---

## 3. Request Flow (SMB2/3)

### 3.1 DFS-disabled path (baseline)

1. Caller uses `SMB2Client`/`ISMBClient` to connect and perform operations.
2. DFS-related options indicate `Enabled = false`.
3. Client never issues DFS IOCTLs; behavior is identical to current implementation.

This path must remain the default after vNext-DFS.

### 3.2 DFS-enabled path (happy path, SMB2/3)

1. **Input**: Caller attempts to access a path that may be DFS-backed (e.g., `\\contoso.com\\Public\\Software\\Tools`).
2. `DfsAwareClientAdapter` checks `DfsClientOptions` and path patterns to determine whether DFS is applicable.
3. If applicable, it asks `IDfsClientResolver` to resolve the path:
   - Resolver builds a `RequestGetDfsReferral` and sends an SMB2 IOCTL with `CtlCode = FSCTL_DFS_GET_REFERRALS` (or `_EX` if needed).
   - `SMB2Client` returns the IOCTL response buffer.
4. Resolver parses the buffer with `ResponseGetDfsReferral` and obtains a list of `DfsReferralEntry` items.
5. Resolver picks a target (e.g., highest priority, then first in order) and computes the resolved UNC:
   - Uses `PathConsumed` and entry strings to rewrite the path.
6. `DfsAwareClientAdapter`:
   - Establishes a connection to the target server/share if not already connected.
   - Issues the original operation (e.g., file open, directory list) on the resolved path.
7. The client caches the referral per protocol rules (details to be finalized in SPEC; basic caching may be sufficient for vNext).

### 3.3 DFS fallback/error handling

- If the IOCTL fails with `STATUS_FS_DRIVER_REQUIRED` or a non-DFS-related error, the client can:
  - Treat the server as non-DFS-capable and either:
    - Fall back to non-DFS behavior, or
    - Surface an explicit error depending on configuration.
- If the response is malformed or the resolver detects inconsistencies:
  - Reject the referral and either retry with another server or return an error.

---

## 4. Request Flow (SMB1)

The SMB1 path mirrors SMB2/3 but uses `TRANS2_GET_DFS_REFERRAL` instead of IOCTLs:

1. `DfsAwareClientAdapter` determines DFS applicability as for SMB2/3.
2. `IDfsClientResolver` constructs and sends a `TRANS2_GET_DFS_REFERRAL` request via `SMB1Client`.
3. Resolver decodes the referral using the same DFSC codec.
4. Target selection, path rewrite, connection/reconnect, and operation replay follow the same pattern as SMB2/3.

SMB1 support is only used when a caller opts into SMB1 and DFS concurrently.

---

## 5. Configuration & Feature Flags

### 5.1 Configuration surface (conceptual)

- DFS options should be exposed in a way that:
  - Works for both SMB1 and SMB2 clients.
  - Allows per-connection configuration without breaking existing constructors or interfaces.
- Potential patterns (to be finalized in SPEC):
  - An options object passed to the client at construction time.
  - An optional configuration method to enable DFS resolution for specific connections.

### 5.2 Behavior guarantees

- **DFS disabled** (default):
  - No DFS IOCTL or TRANS2 referrals are issued.
  - All existing tests and behaviors should remain valid.
- **DFS enabled**:
  - DFS logic is only invoked when paths or configuration indicate it.
  - Non-DFS paths should behave as before.

---

## 6. Testing & Interop Strategy

### 6.1 Unit tests

- Exercise DFSC codec (already planned for shared usage) with:
  - Valid referral blobs for v1–v4.
  - Truncated/malformed/overflow cases.
- Exercise resolver logic:
  - Target selection based on priority/order.
  - Handling of various error codes (e.g., `STATUS_FS_DRIVER_REQUIRED`, buffer overflow).

### 6.2 Integration tests (client-focused)

- Use lab setups from `docs/lab-setup-smb-dfs.md`:
  - Windows DFS Namespace as the primary DFS server.
  - Optional Samba `msdfs` for cross-platform behavior.
- Test cases:
  - Open / list / CRUD via DFS paths with SMB2/3 clients.
  - Verify reconnection and failover when targets are disabled.
  - Confirm correct behavior when DFS is turned off.

### 6.3 Future: testing with SMBLibrary DFS server

- A minimal DFS server implementation (as per `docs/dfs-enablement-plan.md`) could later be used to:
  - Provide deterministic DFSC responses for SMBClient tests.
  - Run fully self-contained integration tests (no Windows/Samba dependency).

---

## 7. Open Design Questions (for SPEC/ADR)

- How should DFS options be exposed on the public client APIs without breaking existing consumers?
- What level of referral caching is required for vNext (none, minimal, or closer to Windows semantics)?
- How aggressively should the client retry/rotate among targets on failures?
- Do we need separate flags for SMB1-DFS vs SMB2/3-DFS, or is a single DFS client feature flag sufficient for vNext?

These questions should be resolved in the upcoming SPEC and ADR documents before implementation begins.
