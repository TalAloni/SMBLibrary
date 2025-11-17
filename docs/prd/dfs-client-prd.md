# DFS Client PRD (vNext-DFS)

## 1. Problem Statement

Today, SMBLibrary’s `SMBClient` implementations (SMB1 and SMB2/3):

- Do **not** issue DFS referral requests on their own.
- Rely on callers or the OS to resolve DFS paths into concrete server/share targets.
- Cannot be used to directly exercise DFS referral flows end‑to‑end against Windows DFS Namespaces or Samba `msdfs` without external help.

This limits:

- How well we can test SMBLibrary against real‑world DFS deployments.
- The ability for library consumers to build DFS‑aware applications in a fully managed, cross‑platform way.
- Our ability to use this repo as a self‑contained harness for DFS protocol correctness.

We want `SMBClient` to become **DFS‑aware** in a way that is **opt‑in, isolated, and non‑breaking**.

---

## 2. Goals & Outcomes

### 2.1 Goals

- **G1 – DFS‑aware SMBClient (SMB2/3 and SMB1)**  
  SMBLibrary’s clients can issue DFS referral requests (`FSCTL_DFS_GET_REFERRALS{,_EX}` and `TRANS2_GET_DFS_REFERRAL`) and follow referrals when explicitly enabled.

- **G2 – Safe, opt‑in behavior**  
  DFS client behavior is fully behind configuration/feature flags; existing code using SMBLibrary without DFS remains unaffected by default.

- **G3 – Shared, hardened DFSC codec**  
  A single DFSC codec (request/response/entries) is used by both current and future features (e.g., a minimal DFS server), with strong tests for malformed inputs.

- **G4 – Interop with real DFS deployments**  
  SMBClient DFS behavior is validated against Windows DFS Namespaces and optionally Samba `msdfs`, using the lab patterns in `docs/lab-setup-smb-dfs.md`.

- **G5 – Spec‑then‑code & testability**  
  DFS client behavior is described in PRD/SPEC/ADR, mapped to unit/integration tests, and can be regression‑tested using deterministic vectors and labs.

### 2.2 Non‑goals

- Implementing full server‑side DFS namespace capability (that is covered in `docs/dfs-enablement-plan.md` and is a separate track).
- Integrating with DFS management protocols (DFSNM) or DFS Replication (DFSR).
- Designing complex target selection / load‑balancing / health‑based routing.
- Changing the public surface in a way that forces existing consumers to adopt DFS.

---

## 3. Users & Scenarios

### 3.1 Users

- **Library consumers** building SMB‑based tools or services that need to access DFS paths directly from managed code.
- **Maintainers** of SMBLibrary who want better regression coverage against real DFS deployments.
- **Testers** using SMBLibrary as a harness to probe DFS behavior in Windows/Samba labs.

### 3.2 Scenarios

- **S1 – Connect to a DFS path over SMB2/3**  
  A consumer app uses SMBLibrary’s SMB2 client to open `\\contoso.com\\Public\\Software\\Tools`. DFS is enabled for that connection. SMBClient transparently issues a DFS referral IOCTL, parses the response, connects to a target (e.g., `\\FS1\\Tools`), and performs the requested operation.

- **S2 – Handle DFS failover**  
  A DFS namespace has multiple folder targets. A test disables one target. SMBClient, when trying to access the DFS path, receives a referral list and succeeds by connecting to a remaining target.

- **S3 – Respect DFS disabled default**  
  Existing tests that connect directly to `\\server\\share` without DFS configuration continue to behave exactly as before; no DFS referrals are issued.

- **S4 – Exercise DFS labs**  
  In the lab described by `docs/lab-setup-smb-dfs.md`, tests use SMBClient to access DFS paths and verify referrals against Windows DFSN and (optionally) Samba `msdfs`.

---

## 4. High-Level Approach

We will:

1. **Harden the DFSC codec** (shared) in `SMBLibrary/DFS` with golden tests.
2. **Teach SMBClient to issue DFS referral requests** behind a DFS client feature flag.
3. Introduce a **DFS client resolver** abstraction and adapter so DFS logic is isolated from the core client.
4. Validate behavior against real DFS labs and capture test vectors.

Server‑side DFS remains out of scope for this vNext; that work is tracked separately.

---

## 5. Requirements & Acceptance Criteria

### 5.1 Functional requirements

**FR1 – SMBClient DFS IOCTL (SMB2/3)**  
When DFS client support is enabled for a connection, SMBClient MUST be able to:

- Issue a DFS referral IOCTL (`FSCTL_DFS_GET_REFERRALS` or `FSCTL_DFS_GET_REFERRALS_EX`) via SMB2/3.
- Use the DFSC codec to decode the response buffer into referral entries.
- Select a target and reconnect/operate on the referred server/share.

**FR2 – SMBClient DFS TRANS2 (SMB1)**  
When both SMB1 and DFS are enabled, SMBClient MUST be able to:

- Issue a `TRANS2_GET_DFS_REFERRAL` request.
- Decode the response using the same DFSC codec.
- Select a target and perform operations on the referred server/share.

**FR3 – Feature flags & configuration**  
DFS client behavior MUST be controlled via configuration/flags such that:

- DFS is **disabled by default**.
- When disabled, SMBClient **never** issues DFS referral requests.
- Enabling DFS for a connection is explicit and discoverable (e.g., options object or config API).

**FR4 – Isolation of DFS logic**  
DFS‑specific behavior MUST be contained within dedicated types (e.g., resolver/adapter) so that:

- The core SMBClient code paths remain understandable and maintainable.
- It is possible to disable DFS at build/runtime without large cross‑cutting changes.

**FR5 – Interop with Windows DFS Namespace**  
In a lab using a Windows DFS Namespace configured per `docs/lab-setup-smb-dfs.md`, SMBClient MUST be able to:

- Open a DFS path and complete basic operations (open/list) successfully.
- Correctly handle at least one failover scenario (one target offline, others online).

### 5.2 Non-functional requirements

**NFR1 – Backwards compatibility**  
When DFS is disabled, all existing SMBClient tests MUST pass unchanged.

**NFR2 – Security & robustness**  
DFS referral parsing MUST:

- Use strict bounds checking and guardrails informed by MS11‑042.
- Fail safely on malformed inputs.

**NFR3 – Observability**  
DFS client behavior SHOULD expose enough metrics/logging to:

- Track referral requests, successes, failures, and overflows.
- Support troubleshooting in lab and production‑like environments.

---

## 6. Acceptance Criteria (Given/When/Then)

### AC1 – DFS disabled by default

- **Given** an application using SMBLibrary’s SMB2 client with no DFS options configured,  
  **When** it connects to a non‑DFS path such as `\\server\\share` and performs operations,  
  **Then** no DFS IOCTL or TRANS2 referral requests are sent, and behavior matches the current version.

### AC2 – Basic DFS referral resolution (SMB2/3)

- **Given** DFS client support is enabled for a connection, and a Windows DFS Namespace is configured per `lab-setup-smb-dfs.md`,  
  **When** SMBClient attempts to open a DFS path like `\\contoso.com\\Public\\Software\\Tools`,  
  **Then** SMBClient issues a DFS referral IOCTL, decodes the response, connects to one of the advertised targets, and successfully completes an open operation.

### AC3 – Handling malformed referral responses

- **Given** a captured or synthetic DFSC response that is malformed (truncated or with invalid offsets),  
  **When** the DFSC codec is asked to parse it,  
  **Then** it fails safely (no buffer overrun or crash) and returns an error that the resolver can handle, and a test verifies this behavior.

### AC4 – SMB1 DFS referral support (opt‑in)

- **Given** an environment where SMB1 and DFS are both explicitly enabled for a connection,  
  **When** SMBClient accesses a DFS path that results in a `TRANS2_GET_DFS_REFERRAL` request,  
  **Then** the client issues the request, parses the referral, and completes an operation on the referred target, or returns a clear error if referral parsing fails.

### AC5 – No regression in non‑DFS tests

- **Given** the existing SMBLibrary test suite (non‑DFS),  
  **When** DFS features are compiled in but disabled by default,  
  **Then** all existing tests pass without modification.

### AC6 – Lab interop validation

- **Given** a DFS lab as described in `lab-setup-smb-dfs.md` (Windows DFSN, optional Samba),  
  **When** the DFS client integration tests are run,  
  **Then** they produce a green run for the targeted scenarios (as defined in the SPEC) and capture representative traces for regression purposes.

---

## 7. Success Metrics

- **M1 – Test coverage**  
  Number of new unit/integration tests specifically covering DFS client behavior and DFSC codec; target: coverage for all acceptance criteria.

- **M2 – Interop scenarios**  
  Number of lab scenarios that pass against Windows DFSN and, optionally, Samba `msdfs`.

- **M3 – Regression safety**  
  Zero breaking changes reported by existing SMBLibrary consumers when upgrading to the version that includes DFS client support, assuming DFS remains disabled by default.

- **M4 – Developer ergonomics**  
  Qualitative feedback from maintainers that DFS logic is localized and understandable (e.g., via internal review of resolver/adapter design).

---

## 8. Dependencies & Risks (Summary)

- **Dependencies**
  - Stable SMB1/SMB2/3 client implementation.
  - DFS lab availability (Windows DFSN; optional Samba `msdfs`).
  - Time to capture and curate DFSC golden vectors.

- **Risks**
  - Misinterpreting DFSC structures, leading to interop issues.
  - Over‑coupling DFS client logic with future server‑side DFS work.
  - Under‑estimating the complexity of path normalization and target selection (especially for more complex DFS deployments).

These are further elaborated in the roadmap and design documents and will be refined in the SPEC/ADR steps.
