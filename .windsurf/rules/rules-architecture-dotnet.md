---
description: SMBLibrary — Architecture (C#/.NET)
---

# Scope
SMBLibrary is a C# SMB 1.0/2.x/3.x server and client implementation. The project emphasizes backward compatibility, low allocations, and protocol‑accurate behavior across multiple .NET targets.

# Core principles
- Keep the core cross‑platform and dependency‑light. Platform‑specific code belongs in `SMBLibrary.Win32`.
- Separate protocol concerns by dialect: SMB1 vs SMB2/3 handlers, shared helpers where appropriate.
- Use ports/adapters. `INTFileStore` is the storage port; adapters implement it over real filesystems or named pipes.
- Favor NTSTATUS return codes in protocol paths. Exceptions are for truly exceptional failures.
- Maintain compatibility across targets: `net20; net40; netstandard2.0` in `SMBLibrary.csproj` and `net472; net6.0` in tests.
- Avoid new language/runtime features that break legacy targets; where needed, guard with `#if` blocks.
- Logging via events (`LogEntryAdded`). Do not introduce external logging frameworks in core.
- Thread safety: guard shared maps with `lock`; snapshot event handlers before invocation.

# High‑level structure
- `SMBLibrary/Server/SMBServer*`: host‑agnostic server orchestration (listener, sessions, shares).
- `SMBLibrary/Server/SMB1/*`, `SMBLibrary/Server/SMB2/*`: dialect‑specific handlers and helpers.
- `SMBLibrary/Server/Shares/*`: `ISMBShare` implementations (file system, named pipes).
- `SMBLibrary/NTFileStore/*`: object store contract and structures; adapters map to storage backends.
- `SMBLibrary/Client/*`: client implementation (`SMB2Client`, `SMB2FileStore`, helpers).
- `SMBLibrary.Services/*` + `RPC/NDR/*`: srvsvc/workstation services over named pipes and DCE/RPC codecs.
- `Utilities/*`: byte/endianness helpers; keep reusable and allocation‑aware.

# Protocol patterns
- Codec pattern: `new T(byte[] buffer)` for decode, `GetBytes()` for encode, using Little‑Endian helpers.
- SMB2 IOCTL: validate FSCTL flags and FileId rules; pass to `INTFileStore.DeviceIOControl`.
- Transactions (SMB1): assemble primary + secondary requests into complete operations before dispatch.
- Change notifications: asynchronous model via `NotifyChange` + `Cancel`, returning interim `STATUS_PENDING` and async completion.
- Security: `GSSProvider` injection; NTLM signing/encryption per spec; do not hard‑wire external auth stacks.

# Backward compatibility
- Prefer explicit types over `var` where clarity matters for older compilers.
- Avoid Span/Memory unless guarded behind `#if NETSTANDARD2_0` (and only where justified by profiling).
- Keep public API surface stable; default to `internal` and use `InternalsVisibleTo` for tests.

# Observability
- Use server/share events for visibility. Avoid adding external telemetry dependencies to core.

# DFS and optional features
- DFS referrals are currently scaffolded. Implement behind feature flags and configuration, with strict bounds checks and tests.
