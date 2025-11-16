---
version: vNext-DFS
type: minor
timeframe: 2025-11 → 2026-02 (tentative)
status: draft
---

# DFS Roadmap (SMBLibrary vNext-DFS)

## Overview & Objectives

This roadmap primarily tracks the work to make SMBLibrary’s **SMBClient** DFS-aware (DFS referrals over SMB1/SMB2/3) while keeping DFS logic **isolated**, **feature-flag controlled**, and **non-breaking** for existing consumers. Server-side DFS capability remains a future concern and is tracked separately (see `docs/dfs-enablement-plan.md`).

**Objectives**
- **DFS referral handling in SMBClient over SMB2/3 IOCTL and SMB1 TRANS2**
  - Enable SMBClient to issue DFS referral requests and consume responses, selecting appropriate targets and reconnecting as needed.
- **Pluggable DFS resolution strategy with simple config/OS-assisted resolution first**
  - Start with an explicit strategy for how SMBClient resolves DFS paths (for example, static mapping or OS-assisted resolution), with room for future resolvers.
- **Isolation and feature flags**
  - Gate all DFS client behavior behind explicit configuration/feature flags, defaulting to **DFS disabled**.
  - Preserve existing SMBClient behavior when DFS is not enabled.
- **Safety and correctness**
  - Harden any DFSC parsing/client-side referral handling against malformed inputs (bounds checks, tests informed by MS11-042).
  - Match [MS-DFSC] and [MS-SMB2] semantics for buffer sizes, alignment, and `STATUS_BUFFER_OVERFLOW` where applicable to client behavior.
- **Interop and validation**
  - Validate SMBClient behavior against Windows DFS Namespaces and optional Samba `msdfs` lab per `docs/lab-setup-smb-dfs.md`.


## Scope & Non-goals

**In scope (vNext-DFS – SMBClient)**
- SMBClient DFS referral handling over SMB2/3:
  - Issuing DFS referral IOCTLs (`FSCTL_DFS_GET_REFERRALS{,_EX}` or equivalent) and consuming responses.
- SMBClient DFS behavior over SMB1:
  - Issuing `TRANS2_GET_DFS_REFERRAL` when needed and following referrals.
- DFSC codec and model (shared building block):
  - Encode/decode `ResponseGetDfsReferral` and related structures and referral entries (v1–v4) with a shared string buffer.
- DFS client resolution abstraction:
  - A DFS resolution abstraction (name TBD) used by SMBClient to choose targets and reconnect, with an initial static/OS-assisted implementation.
- Client feature flags and configuration:
  - Client-level DFS capability flag (disabled by default).
  - Per-connection or per-target configuration for enabling DFS resolution.
- Observability (client-focused):
  - Counters and logs for DFS-related client operations, errors, and referral handling.
- Tests and labs:
  - Unit tests for DFSC codec and SMBClient behavior.
  - Integration tests using the DFS lab from `docs/lab-setup-smb-dfs.md`.

**Out of scope (for vNext-DFS)**
- Full server-side DFS capability (namespace server behavior, share-level DFS flags) as described in `docs/dfs-enablement-plan.md`.
- AD-integrated DFS management (DFSNM) and dynamic namespace discovery.
- DFS Replication (DFSR) configuration or management.
- Advanced target selection policies beyond simple priority/order (no health probes or complex load-balancing logic).
- Large-scale refactors of SMB core; changes should be localized to DFS-specific client paths.


## Themes & Epics

- **Theme 1 – DFSC Codec & Models (shared)**
  - **Epic 1.1: DFSC response/entry structures**
    - Implement `ResponseGetDfsReferral` and referral entry types (v1–v4), aligned to [MS-DFSC].
  - **Epic 1.2: DFSC encoder/decoder tests**
    - Golden vector tests for valid, truncated, and malformed payloads (MS11-042 context).

- **Theme 2 – SMBClient Integration & Feature Flags**
  - **Epic 2.1: SMBClient SMB2/3 IOCTL integration**
    - Teach SMBClient to issue DFS IOCTL requests and consume responses, still behaving exactly as today when DFS is disabled.
  - **Epic 2.2: SMBClient SMB1 TRANS2 integration**
    - Teach SMBClient to issue `TRANS2_GET_DFS_REFERRAL` when appropriate and follow referrals.
  - **Epic 2.3: Client capability flags and options**
    - Introduce a DFS client feature flag and per-connection options so DFS behavior is opt-in.

- **Theme 3 – DFS Client Resolver & Configuration (Isolated DFS Layer)**
  - **Epic 3.1: Resolver abstraction**
    - Define and implement a DFS client resolution abstraction in a DFS-focused namespace to keep concerns isolated.
  - **Epic 3.2: Initial resolver implementation**
    - Implement an initial resolver (for example, static mapping or OS-assisted resolution) wired to configuration without broad changes to existing client code.

- **Theme 4 – Observability, Limits, and Hardening**
  - **Epic 4.1: Metrics and logging**
    - Counters: request count, success/fail, overflow, malformed inputs, TTL distribution.
  - **Epic 4.2: Guardrails**
    - Max referrals per response, max string buffer size, strict offset validation.

- **Theme 5 – Interop & Test Readiness**
  - **Epic 5.1: Windows DFSN interop**
    - Integration tests validating referrals to Windows DFS Namespaces.
  - **Epic 5.2: Samba msdfs interop (optional)**
    - Validate referrals to Samba `msdfs` targets.
  - **Epic 5.3: Regression harness**
    - Packets and test cases stored under `docs/labs/` or similar for future DFS work.


## Timeline & Milestones (tentative)

- **Phase 0 – Discovery & Design (complete / ongoing)**
  - DFS docs drafted: `docs/dfs-overview.md`, `docs/dfs-enablement-plan.md`, `docs/lab-setup-smb-dfs.md`.

- **Phase 1 – DFSC Codec & Tests (2025-11 → 2025-12)**
  - Implement DFSC response/entry structures.
  - Add encoder/decoder with golden vector tests.

- **Phase 2 – SMBClient Integration (DFS Off by Default) (2025-12 → 2026-01)**
  - Wire SMBClient SMB2/3 DFS IOCTL handling behind a feature flag.
  - Wire SMBClient SMB1 TRANS2 DFS handling behind the same resolver and flags.
  - Ensure DFS behavior is disabled by default and that existing SMBClient scenarios are unaffected when DFS is off.

- **Phase 3 – Resolver & Configuration (2026-01)**
  - Implement the DFS client resolver abstraction and initial resolver.
  - Connect SMBClient integration to the resolver, still gated by configuration.

- **Phase 4 – Interop, Hardening & Pre-release (2026-01 → 2026-02)**
  - Validate SMBClient behavior against Windows DFS Namespace and optional Samba lab.
  - Tune guardrails, finalize logging/metrics.
  - Decide on readiness for a pre-release/preview of DFS client support.

Dates are indicative and should be refined once detailed SPEC and tasks exist.

### Sprint & Milestone Sequence (example; 2-week sprints)

- **Sprint 0 (2025-11, early)** – Discovery & design
  - Finalize DFS client problem framing and goals.
  - Align on resolver abstraction direction and feature-flag strategy.
  - Validate test lab approach per `docs/lab-setup-smb-dfs.md`.

- **Sprint 1 (2025-11, late)** – DFSC models & happy-path codec
  - Implement core DFSC response/entry structures.
  - Add basic encoder/decoder for the main referral versions.
  - Land first unit tests and golden vectors.

- **Sprint 2 (2025-12, early)** – Codec hardening & negative paths
  - Expand tests for malformed/truncated payloads (MS11-042 context).
  - Add bounds checks and guardrails around offsets and string buffers.
  - Ensure no impact to existing SMBClient paths when DFS is disabled.

- **Sprint 3 (2025-12, late)** – SMBClient SMB2/3 IOCTL integration (flagged)
  - Teach SMBClient to issue DFS IOCTLs and consume responses via the DFSC codec/resolver.
  - Keep DFS client feature flag disabled by default; verify behavior matches pre-DFS when disabled.
  - Add targeted SMBClient tests around IOCTL-based referral handling.

- **Sprint 4 (2026-01, early)** – SMBClient SMB1 TRANS2 + flags
  - Implement `TRANS2_GET_DFS_REFERRAL` handling in SMBClient via the same resolver.
  - Introduce/verify DFS client feature flags and per-connection options (opt-in only).
  - Regression tests confirming non-DFS SMBClient scenarios remain unchanged.

- **Sprint 5 (2026-01, mid)** – Resolver & static/OS-assisted configuration
  - Implement the DFS client resolver and configuration wiring.
  - Add configuration examples and minimal docs for DFS-enabled SMBClient usage.
  - Keep DFS disabled by default in sample configs.

- **Sprint 6 (2026-01, late)** – Interop & observability
  - Validate SMBClient against a Windows DFS Namespace in the lab.
  - Optional: smoke test against Samba `msdfs`.
  - Add metrics/logging for DFS client requests, overflows, and rejects.

- **Sprint 7 (2026-02, early)** – Hardening & preview readiness
  - Address interop issues found in the lab.
  - Finalize guardrails, documentation, and examples.
  - Decide on preview/GA scope and update roadmap/PRD/SPEC accordingly.

> Note: Sprint numbers and dates are illustrative; adjust to match your actual cadence while keeping DFS client work behind feature flags and isolated from non-DFS paths.


## Dependencies & Risks

**Dependencies**
- Stable SMB1/SMB2/3 server implementation in SMBLibrary.
- Test lab availability per `docs/lab-setup-smb-dfs.md` (Windows DFS Namespace; optional Samba).
- Existing configuration mechanisms in `SMBServer` and library consumers for feature flags.

**Key Risks & Mitigations**
- **Parsing vulnerabilities (DFSC codec)**
  - Mitigation: strict bounds checking, centralized codec, fuzzing malformed inputs, golden test vectors, and defensive length checks (MS11-042 awareness).
- **Interop quirks (alignment, `STATUS_BUFFER_OVERFLOW`)**
  - Mitigation: follow [MS-SMB2] requirements; capture reference traffic from Windows DFS clients; add regression tests.
- **Namespace complexity and misconfiguration**
  - Mitigation: start with static provider; keep the abstraction small; provide clear examples and validation steps.
- **Unexpected impact on existing non-DFS shares**
  - Mitigation: DFS disabled by default; minimal changes to existing code paths; DFS behavior only enabled when flags are explicitly set.


## Compatibility & Breaking Changes

**Compatibility policy**
- This is a **minor version** feature; no breaking changes are planned.

**When DFS client is disabled (default)**
- SMBClient does not issue DFS referral IOCTL or TRANS2 requests.
- All existing SMBClient behaviors and tests remain valid.
- No change to how callers interact with non-DFS paths or direct `\\server\\share` connections.

**When DFS client is enabled**
- DFS behavior is opt-in via configuration:
  - Client-level DFS options/flags (for example, `DfsClientOptions.Enabled`) control whether DFS referral requests are issued for a given connection or path.
- Connections where DFS is disabled behave exactly as before.

**Potential subtle changes (to be tested)**
- Additional round-trips for DFS referral requests may affect latency for some paths.
- Error-handling behavior when DFS referrals fail (timeouts, malformed responses) must be well-documented and should not surprise callers that opt in.


## Migration & Rollout (Feature Flags & Isolation)

**Feature Flags (proposed)**
- Client-level:
  - DFS client options/flags (for example, `DfsClientOptions.Enabled`) control whether DFS referral requests are issued for a given connection or path. Default: `false`.
  - Per-connection or per-path configuration determines which calls participate in DFS resolution.
- Internal toggles (optional):
  - Separate switches for SMB1 vs SMB2/3 DFS client behavior if needed for staged rollout and testing.

**Rollout Strategy**
- **Stage 0 – Dark shipping**
  - Ship code with DFS client options available but disabled; existing behavior unchanged.
- **Stage 1 – Lab-only enablement**
  - Enable DFS client options only in controlled labs using `docs/lab-setup-smb-dfs.md`.
- **Stage 2 – Preview**
  - Allow early adopters to opt in by enabling DFS options for specific connections and using the documented resolver configuration.
- **Stage 3 – General availability**
  - Confirm telemetry and interop; mark DFS client support as supported but still opt-in.

**Rollback Plan**
- Immediate rollback by disabling DFS client options (for example, setting `DfsClientOptions.Enabled` to `false` or removing DFS-related configuration) for affected connections.
- Regression tests must confirm that disabling DFS client support returns behavior to pre-DFS semantics.


## Telemetry & Quality Gates

**Telemetry (internal metrics/logging)**
- DFS IOCTL/TRANS2 request counts, successes, failures, and statuses.
- `STATUS_BUFFER_OVERFLOW` occurrences and returned buffer sizes.
- Malformed/invalid referral requests rejected by the codec.
- TTL distribution and referral cache behavior (where observable).

**Quality Gates for promoting DFS**
- **Codec quality**
  - All DFSC unit tests passing, including malformed/truncation cases.
- **Interop quality**
  - Verified interoperability with at least one Windows DFS Namespace configuration.
  - Optional: validated against Samba `msdfs`.
- **Isolation & safety**
  - Regression tests confirm unchanged behavior when DFS is disabled.
  - No known crashes or high-severity issues in DFS-enabled scenarios.
- **Documentation readiness**
  - DFS behavior and configuration documented in `docs/` and/or `ClientExamples.md` as appropriate.


## References

- **Existing docs**
  - `docs/dfs-overview.md`
  - `docs/dfs-enablement-plan.md`
  - `docs/lab-setup-smb-dfs.md`

- **Future server track (out of scope for vNext-DFS client)**
  - `docs/prd/dfs-server-prd.md` (DFS Server PRD) – TODO
  - `docs/specs/dfs-server-spec.md` (DFS Server SPEC) – TODO
  - `docs/adr/dfs-provider-abstraction.md` (server provider interface/placement) – TODO

- **Protocol specifications**
  - [MS-DFSC] Distributed File System (DFS): Referral Protocol.
  - [MS-SMB2] DFS referral IOCTL handling and CREATE with DFS normalization.
  - [MS-FSCC] DFS-related FSCTL codes.


---

## Open Questions & Next Actions

**Open questions**
- Final version tag for this release (e.g., `v3.1` or similar) and whether DFS ships in a single minor or multiple staged minors.
- Exact naming and placement of DFS feature flags and provider interfaces within SMBLibrary.
- Preferred configuration format/location for `StaticDfsProvider` (new file vs existing server config).
- Whether to support Samba `msdfs` interop as part of the initial release or as a follow-up.

**Next actions**
- Author DFS PRD and SPEC documents and link them here.
- Define the `IDfsProvider` abstraction and configuration story in an ADR.
- Break epics into concrete tasks/tests (see `SMBLibrary.Tests` and new DFS-focused test suites).
- Implement Phase 1 (DFSC codec) following TDD per project rules.
