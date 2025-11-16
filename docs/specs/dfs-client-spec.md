# DFS Client SPEC (vNext-DFS)

Related:
- PRD: `docs/prd/dfs-client-prd.md`
- Roadmap: `docs/versions/vNext-DFS/ROADMAP.md`
- Design: `docs/versions/vNext-DFS/DFS-Client-DESIGN.md`
- Tasks: `docs/versions/vNext-DFS/TASKS.md`

---

## 1. Scope

This SPEC defines the architecture, interfaces, and behavioral contracts for **DFS client support** in SMBLibrary’s `SMBClient` implementations (SMB1 and SMB2/3), as part of vNext-DFS.

- **In scope**:
  - Client-side DFS referral handling (SMB2/3 IOCTL, SMB1 TRANS2).
  - DFSC codec usage and behavior in the client path.
  - DFS client configuration and feature flags.
  - Resolver and adapter abstractions that keep DFS logic isolated.

- **Out of scope**:
  - Full server-side DFS namespace implementation (see `docs/dfs-enablement-plan.md`).
  - DFS management protocols (DFSNM) and DFS Replication (DFSR).
  - Advanced target selection and health-based routing.

---

## 2. Architecture Overview

### 2.1 High-level components

- **SMB clients (existing)**
  - `SMB2Client` – SMB2/3 protocol implementation.
  - `SMB1Client` – SMB1 protocol implementation.
  - `ISMBClient`, `ISMBFileStore` – public contracts used by consumers.

- **DFS codec (existing)**
  - `SMBLibrary.DFS.RequestGetDfsReferral`.
  - `SMBLibrary.DFS.ResponseGetDfsReferral`.
  - `SMBLibrary.DFS.DfsReferralEntry`.

- **New DFS client types (conceptual)**
  - `DfsClientOptions` (or similar)
  - `IDfsClientResolver`
  - `DfsClientResolver` (initial implementation)
  - `DfsAwareClientAdapter` (integration layer)

### 2.2 Module boundaries

- DFS client logic MUST live in a dedicated namespace (e.g., `SMBLibrary.Client.DFS` or similar) to:
  - Avoid polluting core client namespaces.
  - Make DFS components discoverable and logically grouped.

- Shared DFSC codec stays under `SMBLibrary.DFS` and MUST remain usable by both client and future server code.

---

## 3. Interfaces & Contracts

### 3.1 DfsClientOptions (conceptual)

**Purpose**: Expresses DFS-related configuration for a given client/connection.

**Key properties (conceptual)**:

- `bool Enabled` – default `false`. When `false`, DFS behavior is disabled.
- Optional path/namespace hints (e.g., patterns or explicit flags) to control when DFS applies.

**Constraints**:

- Must be injectable without breaking existing constructors; patterns may include:
  - Overloads that accept an options object.
  - A builder or configuration method.
- Preferred pattern for vNext-DFS:
  - Add constructor overloads and/or factory methods that accept a `DfsClientOptions` instance.
  - Keep existing constructors and default configurations; they should map to `Enabled = false` and preserve current behavior.
  - Avoid global/static configuration for DFS; configuration should be per-client or per-connection.

### 3.2 IDfsClientResolver

**Role**: Abstracts how DFS paths are resolved into concrete targets.

**Conceptual signature** (for illustration; exact C# not committed here):

- `ResolveAsync(context, originalPath) -> DfsResolutionResult`

Where `context` includes:

- SMB dialect (SMB1 or SMB2/3).
- Connection identity (server, share, credentials).
- DFS options.

And `DfsResolutionResult` includes:

- `ResolvedPath` – the UNC path to the chosen target.
- `Targets` – list of referral entries.
- `Status` – success, overflow, malformed, unsupported.

**Responsibilities**:

- Decide when to issue DFS referral requests.
- Build and send `RequestGetDfsReferral` via SMB1/SMB2 as appropriate.
- Parse `ResponseGetDfsReferral` and pick a target.
- Surface failures in a way the adapter can act on (fallback vs error).

> Implementation note (vNext-DFS initial stub): until DFS IOCTL/TRANS2 wiring is implemented, a DFS-enabled resolution attempt MAY return `Status = Error` with `ResolvedPath` equal to the original path. This indicates that DFS resolution is not yet available for that path in the current build, and callers should treat this as a non-fatal lack of DFS support.

### 3.3 DfsAwareClientAdapter

**Role**: Sits between high-level client operations and the underlying SMB1/SMB2 client, coordinating DFS resolution.

**Responsibilities**:

- Consult `DfsClientOptions` and path patterns to determine whether DFS should be invoked.
- Call `IDfsClientResolver` when needed.
- (Re)establish connections to referred targets using existing `SMB1Client`/`SMB2Client` mechanisms.
- Replay the original operation on the resolved connection.

**Constraints**:

- MUST be bypassed entirely when DFS is disabled.
- MUST minimize changes to the public interfaces of `ISMBClient` and `ISMBFileStore`.

---

## 4. Behavior (SMB2/3)

### 4.1 DFS-disabled path

- When `DfsClientOptions.Enabled == false`:
  - No DFS IOCTLs are sent.
  - `SMB2Client` behaves as in the current version.

### 4.2 DFS-enabled path (happy path)

1. Caller issues an operation that includes a path (e.g., open `\\contoso.com\\Public\\Software\\Tools`).
2. Adapter checks options and determines DFS applies.
3. Resolver builds a `RequestGetDfsReferral` and calls into `SMB2Client` to send an IOCTL with `CtlCode = FSCTL_DFS_GET_REFERRALS` (or `_EX`).
4. `SMB2Client` returns the IOCTL response buffer.
5. Resolver parses the buffer with `ResponseGetDfsReferral` and obtains a list of `DfsReferralEntry` objects.
6. Resolver selects a target based on referral priority/order and derives the concrete path.
7. Adapter ensures a connection to the target server/share exists and replays the original operation.

### 4.3 Error handling

- If `SMB2Client` returns `STATUS_FS_DRIVER_REQUIRED` or similar non-DFS status when DFS is enabled:
  - Resolver surfaces a resolvable error.
  - By default, the adapter SHOULD treat this as "server not DFS-capable" and fall back to non-DFS behavior, preserving current semantics for that path.
  - A stricter mode (if added later) MAY choose to surface an error instead of falling back; such a mode must be explicitly configured.

- If DFSC parsing fails (malformed input):
  - Resolver returns a failure status; adapter SHOULD:
    - Avoid retrying with the same broken data, and
    - Surface a clear error to callers.

### 4.4 Buffer and alignment requirements

- DFSC buffers MUST adhere to the layout and alignment requirements from MS-DFSC/MS-SMB2.
- The DFSC codec is responsible for verifying bounds and alignment when decoding.

---

## 5. Behavior (SMB1)

### 5.1 DFS-disabled path

- When DFS is disabled, SMB1 behavior remains unchanged; no DFS TRANS2 referrals are emitted.

### 5.2 DFS-enabled path

- When both SMB1 and DFS client are enabled and DFS applies:
  - Resolver uses `SMB1Client` to issue `TRANS2_GET_DFS_REFERRAL`.
  - The same DFSC codec parses responses.
  - Adapter performs target selection and operation replay as in the SMB2/3 case.

> SMB1 DFS support is considered optional/legacy. SMB2/3 is the primary focus for DFS client behavior and interop; SMB1 DFS tests may be gated or reduced depending on environment support.

---

## 6. Telemetry & Observability

### 6.1 Metrics (conceptual)

- Number of DFS client requests issued (IOCTL / TRANS2).
- Number of successful resolutions vs failures.
- Count of malformed/overflow responses.

### 6.2 Logging (conceptual)

- Log notable DFS client events:
  - When a DFS resolution is attempted.
  - When a target is chosen or when no valid target is found.
  - When parsing fails due to malformed input.
- Follow existing logging rules (no secrets or full payloads in logs).

---

## 7. Risks & Mitigations

- **Risk: parser vulnerabilities**  
  Mitigation: centralized DFSC codec with strict bounds checks and test vectors.

- **Risk: behavior drift vs Windows/Samba**  
  Mitigation: use lab captures to validate behavior and add regression tests.

- **Risk: DFS logic leaking into core client paths**  
  Mitigation: enforce separation via resolver/adapter abstraction and namespaces.

---

## 8. Acceptance Criteria Mapping

This section maps PRD acceptance criteria to SPEC sections and planned tests.

- **AC1 – DFS disabled by default** (PRD)
  - SPEC: Sections 4.1 and 5.1.
  - Tests: Regression tests that assert no DFS calls when options are not set.

- **AC2 – Basic DFS referral resolution (SMB2/3)**
  - SPEC: Sections 4.2 and 3.2/3.3.
  - Tests: Integration tests in lab; unit tests around resolver/adapter.

- **AC3 – Handling malformed referral responses**
  - SPEC: Sections 4.3 and 6.
  - Tests: DFSC negative-path tests.

- **AC4 – SMB1 DFS referral support (opt-in)**
  - SPEC: Section 5.2.
  - Tests: SMB1 DFS tests (optional or gated where SMB1 is used).

- **AC5 – No regression in non-DFS tests**
  - SPEC: Sections 4.1 and 5.1.
  - Tests: Full suite regression run with DFS disabled.

- **AC6 – Lab interop validation**
  - SPEC: Section 6 and cross-reference to lab docs.
  - Tests: Integration tests using `lab-setup-smb-dfs.md` environment.

---

## 9. Open SPEC Questions

- Exact shape of DFS configuration APIs (overloads vs builders vs options objects) within the preferred pattern described in §3.1.
- For vNext, DFS client will not introduce its own persistent caching layer for referrals beyond any caching inherent in the underlying protocol/runtime. Any future client-side DFS caching beyond simple in-memory, per-connection behavior MUST be introduced via a dedicated ADR.
- Whether SMB1 DFS support beyond basic, best-effort behavior is required for all consumers or can remain optional/experimental.

These questions should be resolved before implementation in the relevant ADRs and/or SPEC revisions.
