# DFS Client Development Plan (vNext-DFS)

This document captures the current DFS client development plan for SMBLibrary, aligned with:

- DFS Client SPEC: `docs/specs/dfs-client-spec.md`
- DFS Roadmap: `docs/versions/vNext-DFS/ROADMAP.md`
- DFS Client Design: `docs/versions/vNext-DFS/DFS-Client-DESIGN.md`

The focus is **client-only DFS**, behind feature flags, with existing behavior unchanged when DFS is disabled.

---

## 1. External Protocol References

The plan follows these protocol behaviors and recommendations:

- **[MS-SMB2]**
  - DFS referral information is requested via `FSCTL_DFS_GET_REFERRALS` / `FSCTL_DFS_GET_REFERRALS_EX` over SMB2 IOCTL.
  - DFS-specific IOCTL requirements:
    - `FileId` must be `{ 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF }` for DFS IOCTLs.
    - `Flags` must have `SMB2_0_IOCTL_IS_FSCTL` set.
    - `MaxOutputResponse` is the max buffer the client is prepared to accept.
  - Client behavior for DFS referral IOCTLs:
    - On error: propagate status to caller.
    - On success: use `OutputOffset`/`OutputCount` to locate the referral buffer.
    - `STATUS_BUFFER_OVERFLOW` still returns usable data; client should consume returned bytes.
  - Server behavior:
    - Non-DFS-capable servers return `STATUS_FS_DRIVER_REQUIRED` for DFS IOCTLs.

- **[MS-DFSC]**
  - Referral requests: `REQ_GET_DFS_REFERRAL` / `REQ_GET_DFS_REFERRAL_EX`.
  - Referral responses use a shared string buffer with offsets; robust bounds checking is required (MS11-042 context).
  - Clients may cache referrals. vNext DFS client will implement a simple in-memory, per-connection referral cache keyed by DFS namespace/path and guided by referral TTL (where provided), with no persistent or on-disk caching.

These behaviors are reflected in the phases and tasks below.

---

## 2. Phase A – DFSC Codec & Tests (Client-Focused)

**Goal:** Complete and harden the DFSC codec so that client code can safely parse referral responses for SMB2/3 and SMB1.

### A1. Complete `ResponseGetDfsReferral` and `DfsReferralEntry` models

- **Tests** (under `SMBLibrary.Tests/DFS/`):
  - Add or extend DFSC codec tests to cover:
    - Happy-path parsing of well-formed referral responses for in-scope versions (v1–v4).
    - Optional encoding tests where the client needs to construct DFSC request payloads.
  - Use deterministic vectors (from MS-DFSC examples or lab captures when available) and store them under `docs/labs/dfs/` or similar.

- **Implementation** (under `SMBLibrary/DFS/`):
  - Extend `ResponseGetDfsReferral` beyond header-only behavior to parse:
    - `PathConsumed`, `NumberOfReferrals`, `ReferralHeaderFlags`.
    - The array of referral entries using offsets into a shared string buffer.
  - Implement `DfsReferralEntry` variants for in-scope versions:
    - Core fields: version, size, TTL, flags, path/server/share strings, `PathConsumed`, target set flags, etc.
  - Provide safe accessors on `ResponseGetDfsReferral` to enumerate normalized entries, hiding raw offset arithmetic from callers.

### A2. Hardening DFSC negative paths

- **Tests**:
  - Extend DFSC tests with negative-path / malformed-input vectors covering:
    - Truncated buffers.
    - Invalid offsets pointing outside the buffer.
    - Unsupported version numbers or flags.
    - Oversized referral counts and unexpected sizes.
  - Assert that the codec fails predictably (e.g., explicit exception or status) without out-of-bounds reads.

- **Implementation**:
  - Add strict bounds checks in `ResponseGetDfsReferral` and `DfsReferralEntry` parsing:
    - Validate that all offsets and lengths are within the buffer.
    - Enforce internal max referral count and buffer size limits for safety.
  - Surface failures in a way that DFS client code can act on:
    - Either via specific exception types or explicit status flags on the parsed object.

---

## 3. Phase B – Resolver Core (Pure Referral Logic)

**Goal:** Implement the core path resolution and referral selection logic without network dependencies.

For vNext, the resolver will be implemented as a synchronous API; introducing an asynchronous resolver (for example, `ResolveAsync`) would require a SPEC/ADR update aligned with `dfs-client-spec.md` §3.2.

### B1. Extend `DfsResolutionResult` for downstream needs

- **Tests** (under `SMBLibrary.Tests/Client/`):
  - Verify that `DfsResolutionResult` can carry:
    - `Status` (`NotApplicable`, `Success`, `Error`).
    - `ResolvedPath` (rewritten UNC path).
    - Optional: original path and selected target metadata if needed.

- **Implementation** (under `SMBLibrary.Client.DFS`):
  - Extend `DfsResolutionResult` to hold:
    - The selected resolved UNC path.
    - Status and optional extra information that is useful for logging/metrics.

### B2. Implement pure referral selection and path rewrite logic

- **Tests**:
  - Add resolver tests that construct `ResponseGetDfsReferral` / `DfsReferralEntry` objects in memory and verify:
    - Single-referral case: resolved path is computed correctly using `PathConsumed` semantics.
    - Multi-referral case: highest priority, then first-in-order selection.
    - No usable referral case: `Status = Error` and resolution fails in a predictable way.

- **Implementation**:
  - Introduce helper logic (e.g., internal helper or `DfsReferralSelector`) in `SMBLibrary.Client.DFS` to:
    - Take the original UNC path and parsed referral entries.
    - Apply `PathConsumed` and entry fields to compute the resolved UNC path.
    - Apply simple priority/order semantics as per MS-DFSC.
  - Keep this logic strictly pure (no SMB2/SMB1 dependencies).

### B3. Minimal per-connection referral cache

- **Tests**:
  - Add resolver tests that verify:
    - A second resolution attempt for the same DFS path, while the referral TTL is still valid, is served from an in-memory cache without issuing a new transport call.
    - When the TTL has expired, the resolver consults the transport again and refreshes the cache entry.
    - Error or malformed responses are **not** cached.

- **Implementation**:
  - Introduce a simple in-memory, per-connection referral cache inside the DFS resolver layer:
    - Keyed by a combination of connection identity and DFS namespace/path.
    - Entries store the selected target information and an expiration time derived from referral TTL (where provided) or a conservative default.
  - The cache MUST:
    - Live only in memory (no persistence across process restarts).
    - Be optional and bounded to avoid unbounded growth.
    - Be transparent to callers (disabling DFS still bypasses all DFS behavior, including cache usage).

---

## 4. Phase C – DFS Transport Abstraction (IOCTL/TRANS2 Seam)

**Goal:** Introduce a DFS-specific transport seam that hides SMB2 IOCTL / SMB1 TRANS2 details from the resolver.

### C1. Define a DFS referral transport interface

- **Tests**:
  - Add tests against a fake implementation of the new interface, e.g.:
    - `IDfsReferralTransport` with a method such as:
      - `NTStatus TryGetReferrals(string serverName, string dfsPath, uint maxOutputSize, out byte[] buffer, out uint outputCount)`.
  - Validate that `DfsClientResolver`:
    - Only calls the transport when `DfsClientOptions.Enabled == true`.
    - Surfaces the transport status into `DfsResolutionResult` correctly.

- **Implementation**:
  - Add `IDfsReferralTransport` under `SMBLibrary.Client.DFS` as an SMB-agnostic contract for:
    - "Send REQ_GET_DFS_REFERRAL{,_EX} and receive a raw DFSC response buffer, the corresponding `OutputCount`, and an `NTStatus`".
  - This interface underpins the resolver described in `dfs-client-spec.md` §3.2 and the adapter behavior in §3.3 while keeping SMB1/SMB2 specifics out of higher layers.
  - Update `DfsClientResolver` to depend on this interface (constructor injection or property) instead of directly on SMB clients.

### C2. Connect resolver core to transport + codec

- **Tests**:
  - Extend `DfsClientResolverTests` to cover:
    - Non-success and non-buffer-overflow statuses → `Status = Error` with `ResolvedPath = originalPath`.
    - `STATUS_FS_DRIVER_REQUIRED` (non-DFS-capable server) → treated as `NotApplicable` so the adapter can fall back.
    - Success/overflow responses → DFSC codec parses buffer, referral selection logic computes a new path, and `Status = Success`.

- **Implementation**:
  - Implement the DFS-enabled path in `DfsClientResolver.Resolve` (beyond the current stub):
    - Check `options.Enabled`; if false, keep current `NotApplicable` behavior.
    - Call `IDfsReferralTransport` with the target `serverName`, `dfsPath`, and `maxOutputSize`.
    - For `STATUS_SUCCESS` / `STATUS_BUFFER_OVERFLOW`:
      - Parse the buffer with `ResponseGetDfsReferral`.
      - Run Phase B selection logic.
      - Populate `DfsResolutionResult` with `Status = Success` and the resolved path.
    - For `STATUS_FS_DRIVER_REQUIRED`:
      - Treat as DFS not available on that server and return `NotApplicable` (or a dedicated status) to allow fallback.
    - For other errors:
      - Return `Status = Error` and keep `ResolvedPath = originalPath` per current stub behavior.

### C3. NTStatus to DfsResolutionStatus mapping

The resolver MUST apply a consistent mapping from transport `NTStatus` values to `DfsResolutionStatus` and caching behavior:

- `STATUS_SUCCESS`:
  - `DfsResolutionStatus.Success`.
  - Use `OutputCount` from `IDfsReferralTransport` together with the buffer when parsing.
  - On successful parsing/selection, update the per-connection referral cache.

- `STATUS_BUFFER_OVERFLOW`:
  - `DfsResolutionStatus.Success` if the partial referral data can be parsed and a target can be selected.
  - Still rely on `OutputCount` to bound parsing; log as an overflow case.
  - Cache the result if resolution succeeds; otherwise, treat as error (see below).

- `STATUS_FS_DRIVER_REQUIRED` (server not DFS-capable):
  - `DfsResolutionStatus.NotApplicable`.
  - `ResolvedPath = originalPath`.
  - Do not cache a referral result (there is no referral data), but the adapter may treat this as a signal to stop attempting DFS on this connection/path.

- Any other non-success status (for example, `STATUS_INVALID_PARAMETER`, timeouts surfaced as status codes):
  - `DfsResolutionStatus.Error`.
  - `ResolvedPath = originalPath`.
  - Do not cache a referral result.

---

## 5. Phase D – Adapter & Client Integration (SMB2/SMB1)

**Goal:** Integrate DFS resolution into client flows via an adapter, keeping DFS logic isolated and fully gated by configuration.

### D1. Design and implement `DfsAwareClientAdapter`

- **Tests** (under `SMBLibrary.Tests/Client/`):
  - Introduce adapter-level tests using test doubles for `ISMBClient` / `ISMBFileStore` and `IDfsClientResolver`:
    - DFS disabled → operations are passed directly to the underlying client; resolver is never invoked.
    - DFS enabled but resolver returns `NotApplicable` → direct pass-through.
    - DFS enabled and resolver returns `Success` → adapter reissues representative operations (e.g. `CreateFile`, `QueryDirectory`) against the resolved target.

- **Implementation**:
  - Add `DfsAwareClientAdapter` (or equivalent) under `SMBLibrary.Client.DFS`:
    - Wraps an `ISMBClient` / `ISMBFileStore`.
    - Owns a `DfsClientOptions` instance and an `IDfsClientResolver` reference.
    - Exposes a minimal set of DFS-aware operations (start with a small surface, e.g. open/list) that:
      - Decide if DFS applies based on options and path.
      - Call resolver when appropriate.
      - Route the operation to the correct underlying client/connection.
  - Ensure the adapter is fully bypassed when DFS is disabled.
  - This adapter is the concrete realization of `DfsAwareClientAdapter` described in `dfs-client-spec.md` §3.3.

### D2. Implement SMB2 DFS referral transport behind seam

- **Tests**:
  - Add SMB2-focused tests (unit or small integration) that:
    - Verify the SMB2 DFS transport implementation issues IOCTLs with:
      - `CtlCode = FSCTL_DFS_GET_REFERRALS` (or `_EX`).
      - `FileId = { 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF }`.
      - `Flags` indicating `IsFSCtl` / filesystem control.
      - `MaxOutputResponse` bounded by the client’s `MaxTransactSize`.
    - Confirm no DFS IOCTLs are issued when DFS is disabled.

- **Implementation**:
  - Implement `IDfsReferralTransport` for SMB2 using:
    - `DfsIoctlRequestBuilder` to build an `IOCtlRequest` with the correct DFSC payload and control code.
    - `SMB2FileStore.DeviceIOControl` or equivalent to send the IOCTL and receive the raw output buffer.
  - Keep all SMB2-specific logic inside the transport implementation; `DfsClientResolver` only sees `NTStatus`, the buffer, and `OutputCount`.
  - The transport implementation SHOULD issue DFS IOCTLs over an existing `Session`/`TreeConnect` (typically IPC$) consistent with [MS-SMB2] §3.2.4.20.3.

### D3. SMB1 DFS referral transport (optional / later)

- **Tests**:
  - When SMB1 DFS is in scope, add tests mirroring SMB2 for `TRANS2_GET_DFS_REFERRAL` behavior.

- **Implementation**:
  - Implement an SMB1-specific `IDfsReferralTransport` using `TRANS2_GET_DFS_REFERRAL`, following the same contract and error-handling semantics.

---

## 6. Phase E – Feature Flags, Observability, and Docs

**Goal:** Wire DFS options into client construction non-breakingly, add light observability, and update docs to reflect implemented behavior.

### E1. Feature flag wiring and configuration surface

- **Tests**:
  - Add tests confirming that:
    - Default client construction paths map to `DfsClientOptions.Enabled == false`.
    - Enabling DFS via new configuration APIs (constructor overloads, factory, or configuration method) results in DFS behavior being attempted.
    - Disabling DFS again restores pre-DFS behavior.

- **Implementation**:
  - Introduce non-breaking ways to pass `DfsClientOptions` into clients/adapters:
    - Constructor overloads or factories that accept `DfsClientOptions`.
    - Existing constructors remain and implicitly configure DFS as disabled.
  - Keep DFS configuration per-client or per-connection; avoid global static configuration.

### E2. Observability (logging/metrics)

- **Tests**:
  - Where practical, add tests around logging hooks (e.g., verifying that key DFS events result in log entries via existing mechanisms).

- **Implementation**:
  - In DFS-resolver/adapter code only:
    - Log DFS resolution attempts, selected targets, DFS-not-available situations, and malformed responses.
    - Do not log raw DFSC payloads or sensitive data.
  - Reserve metric counters (conceptual) for:
    - DFS client requests (IOCTL/TRANS2).
    - Successful vs failed resolutions.
    - Malformed/overflow responses.

### E3. Documentation and examples

- **Tasks**:
  - Update `docs/specs/dfs-client-spec.md` to reflect:
    - Implemented resolver behavior (`Status` mapping, fallback semantics).
    - Adapter responsibilities and path rewrite rules.
  - Update `docs/versions/vNext-DFS/ROADMAP.md` and `TASKS.md` to:
    - Align epics and tasks with the phases and slices defined here.
  - Add a short DFS-enabled client usage example to `ClientExamples.md` or a new DFS example document:
    - Creating a client with DFS enabled.
    - Accessing a DFS namespace path.
    - Observed behavior when DFS is disabled vs enabled.

---

## 7. Recommended Near-Term Slice

For the next development cycle (TDD, small PR):

1. **Finish DFSC codec for referral entries + negative paths** (Phase A1–A2):
   - Complete parsing and negative-path tests for `ResponseGetDfsReferral` / `DfsReferralEntry`.
   - Ensure strict bounds and overflow protections.
2. **Implement pure resolver selection logic** (Phase B2):
   - Build path rewrite and target selection on top of the completed codec.

This work stays entirely in `SMBLibrary.DFS` and `SMBLibrary.Client.DFS`, with DFS behavior still disabled by default and no changes to existing SMB client behavior.
