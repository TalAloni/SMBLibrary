# SMBLibrary Agent Instructions

## Project Overview

SMBLibrary is an open-source C# SMB 1.0/CIFS, SMB 2.0, SMB 2.1 and SMB 3.0 server and client implementation. It provides .NET developers with a way to share directories and virtual file systems with any OS that supports the SMB protocol.

## Repository Structure

```
SMBLibrary/
├── SMBLibrary/              # Core library
│   ├── Client/              # SMB client implementations
│   │   ├── DFS/             # DFS client support (PR #326)
│   │   ├── SMB1Client.cs    # SMB1 client
│   │   ├── SMB2Client.cs    # SMB2/3 client
│   │   ├── SMB1FileStore.cs # SMB1 file store operations
│   │   └── SMB2FileStore.cs # SMB2 file store operations
│   ├── DFS/                 # DFS protocol data structures (MS-DFSC)
│   ├── Server/              # SMB server implementation
│   ├── SMB1/                # SMB1 protocol structures
│   ├── SMB2/                # SMB2/3 protocol structures
│   ├── NTFileStore/         # NT file store abstractions
│   ├── Authentication/      # Auth mechanisms (NTLM, etc.)
│   ├── NetBios/             # NetBIOS over TCP
│   ├── RPC/                 # RPC/DCE structures
│   └── Services/            # Named pipe services
├── SMBLibrary.Win32/        # Windows-specific integrations
├── SMBLibrary.Adapters/     # IFileSystem to INTFileStore adapters
├── SMBLibrary.Tests/        # Unit tests
├── SMBServer/               # Example server application
└── Utilities/               # Shared utilities
```

## Coding Conventions

### Style Guidelines
- **Naming**: PascalCase for public members, camelCase for private fields
- **Braces**: Allman style (opening brace on new line)
- **Indentation**: 4 spaces
- **Line endings**: CRLF (Windows)
- **No trailing whitespace**

### Code Organization
- Protocol data structures go in `SMBLibrary/SMB1/`, `SMBLibrary/SMB2/`, or `SMBLibrary/DFS/`
- Client logic goes in `SMBLibrary/Client/`
- Server logic goes in `SMBLibrary/Server/`
- Each protocol message/structure should have its own file
- Include `GetBytes()` for serialization and constructor from `byte[]` for parsing

### Documentation
- XML doc comments on public APIs
- Reference MS-* spec sections in comments (e.g., `/// [MS-DFSC] 2.2.3`)
- Keep external docs minimal; link to Microsoft specs for details

### Testing
- Unit tests in `SMBLibrary.Tests/`
- Round-trip tests for protocol structures (parse → serialize → compare)
- Name test files as `{ClassName}Tests.cs`

## Microsoft Protocol Specifications

Key specifications for this codebase:
- **[MS-SMB]**: SMB 1.0/CIFS Protocol
- **[MS-SMB2]**: SMB 2.x and 3.x Protocols  
- **[MS-DFSC]**: DFS Referral Protocol (for DFS client support)
- **[MS-FSCC]**: File System Control Codes
- **[MS-ERREF]**: Windows Error Codes

See `docs/MS-DFSC-SUMMARY.md` for DFS-specific implementation guidance.

## DFS Implementation Notes (PR #326)

### Architecture
- DFS support is **opt-in** via `DfsClientFactory.CreateDfsAwareFileStore()`
- Does not modify core `SMB2Client` or `ISMBFileStore` interfaces
- Uses `FSCTL_DFS_GET_REFERRALS` / `FSCTL_DFS_GET_REFERRALS_EX` IOCTLs

### Key Components
- `DfsPathResolver`: Resolves DFS paths to actual server/share paths
- `DfsAwareClientAdapter`: Wraps `ISMBFileStore` with DFS resolution
- `ReferralCache`: Caches referral responses per MS-DFSC
- `DomainCache`: Caches domain/DC information
- `DfsSessionManager`: Manages SMB sessions across multiple servers

### Protocol Structures (SMBLibrary/DFS/)
- `RequestGetDfsReferral`: REQ_GET_DFS_REFERRAL (section 2.2.2)
- `RequestGetDfsReferralEx`: REQ_GET_DFS_REFERRAL_EX (section 2.2.3)
- `ResponseGetDfsReferral`: RESP_GET_DFS_REFERRAL (section 2.2.4)
- `DfsReferralEntryV1-V4`: Referral entry structures (section 2.2.5)

### Important: REQ_GET_DFS_REFERRAL_EX Format
Per MS-DFSC 2.2.3.1, the `RequestData` field must include length prefixes:
```
RequestFileNameLength (2 bytes)
RequestFileName (variable, Unicode)
SiteNameLength (2 bytes) - optional, when SiteName flag set
SiteName (variable, Unicode) - optional
```

## Pull Request Guidelines

- Keep PRs small and focused
- Include unit tests for new protocol structures
- Maintain backward compatibility
- Reference MS-* spec sections for protocol changes
- Tal prefers C# only (no PowerShell scripts in repo)

## Contribution Terms

By contributing, you agree to irrevocably assign worldwide copyright and IP rights to SMBLibrary and/or Tal Aloni.
