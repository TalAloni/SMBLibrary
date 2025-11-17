# DFS Enablement Plan (SMBLibrary)

Goal: Add DFS referral support so this SMB server can be DFS-capable and optionally act as a simple namespace server. No code changes have been made yet; this is a plan only.

## Scope & assumptions
- Support SMB2/3 IOCTL path (`FSCTL_DFS_GET_REFERRALS{,_EX}`) and SMB1 `TRANS2_GET_DFS_REFERRAL`.
- Provide a pluggable DFS provider with a default static mapping (JSON/YAML) for namespace → targets; AD-integrated DFS (DFSNM) is out of scope for v1.
- Honor DFSC semantics for buffer sizes, TTL, `PathConsumed`, and referral versioning.

## Milestones
- [ ] **DFSC model & codec**
  - Implement `ResponseGetDfsReferral` encoder/decoder per [MS-DFSC].
  - Define concrete `DfsReferralEntry` types (v1/v2/v3/v4) with offsets into a shared string buffer.
  - Unit tests with golden vectors (valid, truncated, malformed) to harden against parser bugs (see MS11-042 context).
- [ ] **SMB2 IOCTL integration**
  - In `Server/SMB2/IOCtlHelper.cs`, replace `STATUS_FS_DRIVER_REQUIRED` with a call to `IDfsProvider.GetReferral(req, maxOut, isExtended)`.
  - Enforce: `FileId == FFFFFFFFFFFFFFFF/FFFFFFFFFFFFFFFF`; else `STATUS_INVALID_PARAMETER`.
  - Map provider results to IOCTL response buffer; on provider `STATUS_BUFFER_OVERFLOW`, return overflow with partial buffer per [MS-SMB2] 3.3.5.15.2.
- [ ] **SMB1 TRANS2 integration**
  - Handle `TRANS2_GET_DFS_REFERRAL` in the SMB1 transaction dispatcher to call the same provider and return DFSC response.
- [ ] **DFS capability & share flags**
  - Add server-level `IsDfsCapable` flag and set it when provider is configured (per [MS-SMB2] 3.3.4.8).
  - Respect share-level `Dfs`/`DfsRoot` flags; send DFS normalization on `SMB2 CREATE` when `SMB2_FLAGS_DFS_OPERATIONS` is set and the share is DFS (per 3.3.5.9).
- [ ] **Provider & configuration**
  - `IDfsProvider` interface + default `StaticDfsProvider` backed by config (namespace roots, folders, targets, ordering/priority, TTL).
  - Support both standalone and domain-style namespace shapes in config (names are not validated against AD in v1).
- [ ] **Observability & limits**
  - Add verbose logs + counters around DFS IOCTLs (requests, successes, overflows, failures, TTL usage).
  - Guardrails: max referrals per response, max string buffer, sanity checks on offsets.
- [ ] **Interop & tests**
  - Unit: DFSC codec tests, IOCTL contract tests.
  - Integration: Windows client `\server\namespace` path against this server returning static referrals.
  - Negative: invalid FileId, too-small buffers, non-DFS shares.

## Risks & mitigations
- **Parsing vulnerabilities** (CVE-2011-1869 history):
  - Strict bounds checking; fuzz malformed inputs; centralized codec.
- **Interop quirks** (STATUS_BUFFER_OVERFLOW, alignment):
  - Follow [MS-SMB2] structure alignment and append-only buffer layout; add golden captures.
- **Namespace complexity**:
  - Start with static provider; keep provider interface to swap later for AD/DFSNM-backed provider.

## References
- [MS-DFSC] DFS Referral Protocol
  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dfsc/3109f4be-2dbb-42c9-9b8e-0b34f7a2135e
- [MS-SMB2] 3.3.5.15.2 Handling a DFS Referral Information Request
  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-smb2/a9d1d0f9-0614-49f9-bf2e-021ce5cfcb2e
- [MS-SMB2] 3.3.5.9 Receiving an SMB2 CREATE Request (DFS normalization)
  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-smb2/8c61e928-9242-44ed-96a0-98d1032d0d39
- [MS-FSCC] FSCTL values
  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/efbfe127-73ad-4140-9967-ec6500e66d5e
- Security bulletin context (parser hardening): MS11-042 (DFS referral response vulnerability)
  https://learn.microsoft.com/en-us/security-updates/securitybulletins/2011/ms11-042#dfs-referral-response-vulnerability---cve-2011-1869
