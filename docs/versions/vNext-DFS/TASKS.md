# vNext-DFS Tasks (SMBClient DFS – Spec-Then-Code)

This document breaks down the **vNext-DFS** client-only roadmap into phases, sprints, and concrete tasks following the **Spec-Then-Code** methodology.

Related docs:
- Roadmap: `docs/versions/vNext-DFS/ROADMAP.md`
- Design: `docs/versions/vNext-DFS/DFS-Client-DESIGN.md`
- DFS overview: `docs/dfs-overview.md`
- DFS enablement (server-focused, future): `docs/dfs-enablement-plan.md`
- Lab setup: `docs/lab-setup-smb-dfs.md`

---

## Phase 0 – Discovery & Design (Sprint 0)

**Goal**: Frame the problem, outcomes, and design for SMBClient DFS; no production code.

### Sprint 0 (2025-11, early) – Discovery & Design

- **Spec/Docs**
  - Draft **DFS Client PRD** (new): `docs/prd/dfs-client-prd.md` (planned)
    - Problem statement, scope (client-only), non-goals (server, DFSNM, DFSR).
    - Outcomes (≤5) and acceptance criteria (Given/When/Then) focused on SMBClient behavior.
  - Draft **DFS Client SPEC** (new): `docs/specs/dfs-client-spec.md` (planned)
    - Architecture (link to `DFS-Client-DESIGN.md`), interfaces, flags/config surface.
    - Mapping of PRD acceptance criteria → tests (unit & integration).
  - Draft **ADR – DFS Client Resolver Abstraction** (new): `docs/adr/dfs-client-resolver-abstraction.md` (planned)
    - Context, options, decision, consequences.
    - Placement of resolver and adapter types in namespaces.

- **Design & Alignment**
  - Review existing client types: `SMBLibrary/Client/SMB2Client.cs`, `SMBLibrary/Client/SMB1Client.cs`, `ISMBClient.cs`, `ISMBFileStore.cs`.
  - Validate the design in `DFS-Client-DESIGN.md` against the codebase and update if needed (no code changes).
  - Clarify where `DfsClientOptions`, `IDfsClientResolver`, `DfsAwareClientAdapter` will live (namespaces, assemblies).

- **Testing & Lab Prep**
  - Confirm the DFS lab plan in `lab-setup-smb-dfs.md` covers:
    - At least one Windows DFS Namespace configuration.
    - Optional Samba `msdfs` configuration.
  - Define a **minimal client DFS test matrix** in the SPEC, based on the roadmap and lab doc.

- **Exit criteria**
  - DFS Client PRD & SPEC drafted and linked from the roadmap.
  - Resolver/adapter architecture agreed and captured in an ADR.
  - No production code changed.

---

## Phase 1 – DFSC Codec & Tests (Sprints 1–2)

**Goal**: Harden DFSC codec and tests as a shared building block for client DFS.

### Sprint 1 (2025-11, late) – DFSC models & happy-path codec

- **Spec/Docs**
  - Update SPEC with:
    - DFSC structures used by the client (which referral versions are in scope for vNext).
    - Expected behavior for well-formed responses.

- **Implementation (allowed scope)**
  - Refine/improve DFSC codec types in `SMBLibrary/DFS/` as needed (server + client shared):
    - `RequestGetDfsReferral`
    - `ResponseGetDfsReferral`
    - `DfsReferralEntry`
  - Ensure these types can be used from client code without server-only assumptions.

- **Tests**
  - Add **unit tests** (MSTest) under `SMBLibrary.Tests` (e.g., `DFS/DFSCCodecTests.cs`):
    - Happy-path parsing of referral responses for in-scope versions.
    - Encoding where needed for client requests.
  - Capture or construct **golden referral blobs** based on MS docs and/or lab captures; store under `docs/labs/dfs/` or similar.

- **Exit criteria**
  - DFSC codec stable for happy-path use; unit tests passing.
  - No SMBClient behavior changed yet.

### Sprint 2 (2025-12, early) – Codec hardening & negative paths

- **Spec/Docs**
  - Update SPEC with error and edge-case behavior for DFSC parsing:
    - Handling of malformed/truncated buffers.
    - Behavior on version or flag combinations out of scope.

- **Implementation**
  - Add strict **bounds checks and guardrails** to DFSC codec:
    - Offsets within the string buffer.
    - Maximum referral count / buffer size.
  - Ensure DFSC codec exposes safe failure modes for client code (status codes, exceptions where appropriate).

- **Tests**
  - Extend DFSC unit tests with:
    - Truncated buffers.
    - Invalid offsets.
    - Unsupported versions/flags.
  - Add regression vectors for MS11-042-style patterns.

- **Exit criteria**
  - DFSC codec resilient against malformed input.
  - All new negative-path tests passing.

---

## Phase 2 – SMBClient Integration (DFS Off by Default) (Sprints 3–4)

**Goal**: Teach SMBClient to speak DFS (IOCTL/TRANS2) behind a feature flag, without changing default behavior.

### Sprint 3 (2025-12, late) – SMBClient SMB2/3 IOCTL integration

- **Spec/Docs**
  - Update SPEC with:
    - When SMBClient should issue DFS IOCTLs (path-based, config-based, or error-driven).
    - Expected statuses and behavior when DFS is disabled vs enabled.

- **Implementation**
  - Introduce **`DfsClientOptions`** and plumb it into SMB2 client construction/usage (non-breaking pattern).
  - Add **internal hooks** in `SMB2Client` to:
    - Build and send `FSCTL_DFS_GET_REFERRALS{,_EX}` IOCTLs.
    - Return raw DFSC blobs to the resolver.
  - Ensure DFS IOCTL logic is **only active** when DFS options are explicitly enabled.

- **Tests**
  - Add SMB2 client tests (unit or small integration) that:
    - Verify no DFS IOCTLs are sent when DFS is disabled.
    - Verify IOCTL call shape (CtlCode, FileId, alignment) when DFS is enabled.

- **Exit criteria**
  - SMB2 client can issue DFS IOCTLs behind a feature flag.
  - Default configuration remains DFS-off; existing tests still pass.

### Sprint 4 (2026-01, early) – SMBClient SMB1 TRANS2 + flags

- **Spec/Docs**
  - Extend SPEC to cover SMB1 DFS behavior (if SMB1 is enabled) and any differences vs SMB2/3.

- **Implementation**
  - Add DFS options handling to SMB1 client as appropriate (following the same options pattern).
  - Implement `TRANS2_GET_DFS_REFERRAL` sending in `SMB1Client` behind the DFS feature flag.

- **Tests**
  - Add SMB1 client tests (where SMB1 usage is still acceptable in your test matrix) that:
    - Verify no TRANS2 referral calls when DFS is disabled.
    - Verify `TRANS2_GET_DFS_REFERRAL` request shape when DFS is enabled.

- **Exit criteria**
  - SMB1 client can issue DFS referrals behind a feature flag.
  - Existing SMB1 tests remain unchanged when DFS is disabled.

---

## Phase 3 – Resolver & Configuration (Sprints 5)

**Goal**: Introduce the DFS client resolver and configuration wiring, still fully opt-in.

### Sprint 5 (2026-01, mid) – Resolver & static/OS-assisted configuration

- **Spec/Docs**
  - Finalize SPEC sections for:
    - `IDfsClientResolver` contract.
    - `DfsAwareClientAdapter` responsibilities and interaction with `SMB1Client`/`SMB2Client`.
    - Configuration surface for enabling DFS per connection/operation.

- **Implementation**
  - Implement `IDfsClientResolver` and an initial `DfsClientResolver` using the DFSC codec.
  - Implement `DfsAwareClientAdapter` (or equivalent integration layer) that:
    - Decides when to consult the resolver.
    - Replays operations on resolved connections.
  - Wire adapter into SMBClient usage paths in a way that is fully bypassed when DFS is disabled.

- **Tests**
  - Add unit tests for resolver:
    - Target selection based on priority/order.
    - Handling of various DFSC responses and error codes.
  - Add adapter tests:
    - Verify one or more representative operations (e.g., open/list) are reissued against resolved targets when DFS is enabled.
    - Verify no resolution occurs when DFS is disabled.

- **Docs**
  - Add configuration examples (draft) to `ClientExamples.md` or a new client DFS example doc (planned): how to enable DFS for a client.

- **Exit criteria**
  - Resolver and adapter in place, behind config/flags.
  - Clear separation between DFS and non-DFS paths.

---

## Phase 4 – Interop, Hardening & Pre-release (Sprints 6–7)

**Goal**: Validate SMBClient DFS behavior in real labs, finalize guardrails, and prepare for a preview.

### Sprint 6 (2026-01, late) – Interop & observability

- **Spec/Docs**
  - Update SPEC with interop expectations and any lab-specific quirks discovered.

- **Implementation**
  - Add logging/metrics hooks (following project logging rules) for:
    - DFS client requests (IOCTL/TRANS2).
    - Referral successes, failures, and overflow cases.
  - Tune guardrails in resolver/codec based on interop findings.

- **Tests & Labs**
  - Run integration tests against:
    - Windows DFS Namespace (required).
    - Samba `msdfs` (optional but recommended).
  - Capture and archive representative traces/PCAPs under `docs/labs/dfs/`.

- **Exit criteria**
  - No known high-severity issues in DFS-enabled client scenarios.
  - Clear logs/metrics to support future debugging.

### Sprint 7 (2026-02, early) – Hardening & preview readiness

- **Spec/Docs**
  - Update PRD/SPEC with final scope actually shipped in vNext-DFS.
  - Document any deviations from original plan and rationale.

- **Implementation**
  - Address remaining interop or robustness issues discovered in Sprint 6.
  - Ensure flags/config defaults are safe (DFS off by default).

- **Release Prep**
  - Define minimal release notes for DFS client preview (even if no immediate NuGet release yet).
  - Decide whether DFS client ships as preview-only or as a fully supported opt-in feature.

- **Exit criteria**
  - DFS client feature ready for a controlled preview.
  - Clear path to GA (future minor) documented in roadmap.

---

## Ongoing Tasks Across Phases

These tasks span multiple sprints/phases and should be revisited regularly:

- **Spec-Then-Code hygiene**
  - Keep PRD/SPEC/ADR in sync with the implementation plan and roadmap.
  - For each non-trivial implementation change, ensure SPEC/ADR is updated first or in lockstep.

- **Testing & Quality**
  - Maintain mapping from PRD acceptance criteria → SPEC → tests in `SMBLibrary.Tests`.
  - Expand tests as new edge cases are discovered, especially from interop labs.

- **Observability**
  - Revisit logging/metrics around DFS client behavior after major changes.

- **Security**
  - Periodically review DFSC codec and resolver logic with security in mind (parser resilience, avoiding leaking sensitive details in logs).
