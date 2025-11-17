# SMBLibrary Repo Analysis — DFS

## Findings
- **DFS request/response types present (partial)**
  - `SMBLibrary/DFS/RequestGetDfsReferral.cs` — Implements `[MS-DFSC] REQ_GET_DFS_REFERRAL` (client/server data structure).
  - `SMBLibrary/DFS/ResponseGetDfsReferral.cs` — Present but all methods throw `NotImplementedException`.
  - `SMBLibrary/DFS/DfsReferralEntry.cs` — Abstract base, no concrete referral entry types.
- **SMB1 TRANS2 wrappers present**
  - `SMBLibrary/SMB1/Transaction2Subcommands/Transaction2GetDfsReferralRequest.cs`
  - `SMBLibrary/SMB1/Transaction2Subcommands/Transaction2GetDfsReferralResponse.cs`
- **SMB2 IOCTL path explicitly not implemented for DFS**
  - `SMBLibrary/Server/SMB2/IOCtlHelper.cs` returns `STATUS_FS_DRIVER_REQUIRED` for `FSCTL_DFS_GET_REFERRALS` and `FSCTL_DFS_GET_REFERRALS_EX`.
- **DFS-related enums defined**
  - `SMBLibrary/NTFileStore/Enums/IoControlCode.cs` includes `FSCTL_DFS_GET_REFERRALS` (0x00060194) and `FSCTL_DFS_GET_REFERRALS_EX` (0x000601B0).
  - `SMBLibrary/SMB2/Enums/TreeConnect/ShareFlags.cs` includes `Dfs` and `DfsRoot` flags.
  - `SMBLibrary/SMB1/Enums/Negotiate/Capabilities.cs` includes `CAP_DFS` capability.

## Observations
- DFS support is scaffolded but not functional end-to-end.
  - SMB2 DFS referral IOCTLs are rejected with `STATUS_FS_DRIVER_REQUIRED` → indicates server is not DFS-capable.
  - `[MS-DFSC]` response building/parsing is missing (`ResponseGetDfsReferral` not implemented; no referral entry structs for v1/v2/v3/v4).
  - No evidence of a DFS provider/service or path normalization per `[MS-SMB2]` `SMB2_FLAGS_DFS_OPERATIONS`.
- Impact today:
  - Server cannot act as a DFS Namespace server nor return referrals.
  - Windows DFS clients will fail DFS IOCTLs; DFS paths will not resolve through this server.

## Pointers to gap locations
- Implement `[MS-DFSC]` response encoder/decoder: `SMBLibrary/DFS/ResponseGetDfsReferral.cs`, `DfsReferralEntry` concrete types.
- Wire SMB2 IOCTL DFS flow in: `SMBLibrary/Server/SMB2/IOCtlHelper.cs`.
- SMB1 TRANS2 handling in server dispatcher (`Server/SMB1/*`) likely needs DFS referral routing.
- Consider share/instance-level DFS flags: `SMB2 ShareFlags.Dfs/DfsRoot`, SMB1 `CAP_DFS` exposure.

## References
- [MS-DFSC] Distributed File System (DFS): Referral Protocol
  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dfsc/3109f4be-2dbb-42c9-9b8e-0b34f7a2135e
- [MS-SMB2] 3.3.5.15.2 Handling a DFS Referral Information Request (server behavior)
  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-smb2/a9d1d0f9-0614-49f9-bf2e-021ce5cfcb2e
- [MS-SMB2] 3.3.5.9 Receiving an SMB2 CREATE Request (DFS path normalization)
  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-smb2/8c61e928-9242-44ed-96a0-98d1032d0d39
- [MS-FSCC] File System Control Codes (FSCTL values)
  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/efbfe127-73ad-4140-9967-ec6500e66d5e
- DFS Namespace troubleshooting guidance
  https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/troubleshoot-dfs-namespace-guidance
