# DFS Overview (SMB + DFSC)

## What is DFS
- **DFS Namespaces** present a virtual tree of folders mapped to targets across servers. Clients access a DFS path (for example, `\\domain\namespace\folder`) and receive a referral to actual targets.
- Protocols involved:
  - **[MS-DFSC]**: DFS Referral Protocol (request/response wire format for referrals).
  - **[MS-SMB]/[MS-SMB2]**: Transports DFS referral requests (SMB1 via `TRANS2_GET_DFS_REFERRAL`; SMB2 via IOCTL `FSCTL_DFS_GET_REFERRALS` / `_EX`).

## How DFS referrals flow over SMB
- **SMB1**: Client sends `TRANS2_GET_DFS_REFERRAL` request; server replies with DFSC referral structure.
- **SMB2+**: Client issues `SMB2 IOCTL` with `CtlCode=FSCTL_DFS_GET_REFERRALS` (or `_EX`).
  - Server must be DFS-capable or fail with `STATUS_FS_DRIVER_REQUIRED`.
  - The IOCTL response buffer contains the DFSC referral blob; `FileId` must be `{FFFFFFFFFFFFFFFF, FFFFFFFFFFFFFFFF}`.

## Key server-side behaviors
- When receiving DFS referral IOCTLs:
  - If not DFS-capable → return `STATUS_FS_DRIVER_REQUIRED`.
  - Otherwise, invoke DFSC provider/service to produce referral buffer, honoring `MaxOutputResponse` and `STATUS_BUFFER_OVERFLOW` semantics.
- On `SMB2 CREATE` with `SMB2_FLAGS_DFS_OPERATIONS` against a DFS share:
  - If DFS-capable, normalize the DFS path via DFSC (namespace logic) before continuing the create.

## DFSC structures (high level)
- `REQ_GET_DFS_REFERRAL{,_EX}` input includes `MaxReferralLevel` and path.
- `RESP_GET_DFS_REFERRAL` includes:
  - `PathConsumed`, `NumberOfReferrals`, `ReferralHeaderFlags`.
  - A list of referral entries (versions v1/v2/v3/v4) with offets into a string buffer containing Unicode paths.

## Citations
- [MS-DFSC] Distributed File System (DFS): Referral Protocol
  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dfsc/3109f4be-2dbb-42c9-9b8e-0b34f7a2135e
- [MS-SMB2] 3.3.5.15.2 Handling a DFS Referral Information Request
  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-smb2/a9d1d0f9-0614-49f9-bf2e-021ce5cfcb2e
- [MS-SMB2] 3.3.5.9 Receiving an SMB2 CREATE Request (DFS normalization)
  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-smb2/8c61e928-9242-44ed-96a0-98d1032d0d39
- [MS-FSCC] FSCTL codes (DFS IOCTL values)
  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/efbfe127-73ad-4140-9967-ec6500e66d5e
- DFS Namespace troubleshooting guidance
  https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/troubleshoot-dfs-namespace-guidance
