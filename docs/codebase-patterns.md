# Codebase Patterns — SMBLibrary

Date: 2025-11-16
Scope: Architecture, patterns, code style, testing style, organization, and deeper dives into protocol paths and key components. This complements docs/repo-analysis.md and dfs-overview.md.

## Repository layout (high level)
- **SMBLibrary/**: Core server/client protocol implementation (SMB1/2/3, NT file store abstractions, codecs, utilities).
- **SMBLibrary.Adapters/**: Adapters that implement `INTFileStore` over backing file systems.
- **SMBLibrary.Win32/**: Windows-specific interop/support.
- **SMBServer/**: WinForms host UI for running the server interactively.
- **Utilities/**: Common byte/endianness helpers, low-level utilities reused across the core.
- **SMBLibrary.Tests/**: MSTest unit/integration tests (crypto, parsing, NT file store, client flows).

## Architectural patterns
- **Layered server core**
  - Entry and orchestration: `SMBLibrary.Server.SMBServer` manages listener, transport selection, and lifecycle (Start/Stop) with events for connection/log entries.
  - Per-dialect handling: SMB1 (`Server/SMB1/*`) vs SMB2/3 (`Server/SMB2/*`) with helpers (e.g., `TransactionHelper`, `IOCtlHelper`).
  - State management per connection: `SMB2ConnectionState` (sessions, async contexts) extends a base `ConnectionState`.
- **Ports and adapters**
  - `INTFileStore` defines the object store API (open/read/write/directory/security/fsctl/notify).
  - `ISMBShare` bridges share name to `INTFileStore`.
  - `FileSystemShare` implements `ISMBShare`, exposing `AccessRequested` event for pluggable authorization.
  - `SMBLibrary.Adapters.NTFileSystemAdapter` provides a concrete `INTFileStore` over a filesystem abstraction.
- **Codec-centric protocol model**
  - Command and structure classes follow a consistent pattern: decode via ctor(byte[]), encode via `GetBytes()`. Utilities provide LittleEndian read/write.
- **Event-driven extensibility**
  - Logging through `LogEntryAdded` and authorization through `FileSystemShare.AccessRequested` allow application-specific policies without forking core logic.

## Organization & multi-targeting
- `SMBLibrary.csproj` targets `net20; net40; netstandard2.0` for broad compatibility.
- Tests target `net472; net6.0`; `InternalsVisibleTo("SMBLibrary.Tests")` allows testing internals.
- Release build uses ILRepack to merge `Utilities` into the core assembly.

## Code style & conventions
- Private fields prefixed with `m_`; PascalCase for types/methods.
- Minimal exceptions on protocol paths; NTSTATUS is the primary error signaling mechanism.
- Low-level, allocation-aware parsing/writing (arrays, explicit offsets) to support legacy targets.
- Thread-safety via `lock` around shared dictionaries; event invocation captures delegate locally.

## Testing style
- MSTest (`[TestClass]`, `[TestMethod]`), via `Microsoft.NET.Test.Sdk`, `MSTest.TestFramework/Adapter`.
- Heavy use of deterministic vectors:
  - SMB2 crypto/signing: `SMB2EncryptionTests`, `SMB2SigningTests`.
  - NTLM/RC4: `NTLM/*Tests.cs`.
  - SMB2 parsing: `SMB2/*ParsingTests.cs`.
  - NT file store semantics: `NTFileStore/*Tests.cs`.
- Some integration flows exist (e.g., `Client/SMB2ClientTests.cs`, `IntegrationTests/LoginTests.cs`).

## Observability & error handling
- Logging abstraction via events; severity levels recorded; no external logging framework.
- Protocol operations return `NTStatus` consistently; helpers translate exceptions to statuses where appropriate.

---

# Deeper dives

## SMB2 pipeline (request lifecycle)
- **Listener/accept**: `SMBServer.Start()` binds and begins accept; new sockets are registered with a `ConnectionManager`.
- **Connection state**: `SMB2ConnectionState` manages:
  - Session table keyed by `SessionId`; generation skips invalid IDs (0/0xFFFFFFFF). Thread-safe with locks.
  - Pending async operations table keyed by `AsyncId` for compound/async commands.
- **Command handling**: Per-command handlers validate header fields and route to share/open-specific logic. Examples:
  - `IOCtlHelper.GetIOCtlResponse(...)` enforces FSCTL rules (e.g., DFS IOCTLs require DFS capability and special FileId) and calls into the store (`DeviceIOControl`).
  - CREATE path (not shown here) validates flags; when DFS is enabled and `SMB2_FLAGS_DFS_OPERATIONS` is set, a DFS normalization step would be invoked per spec (currently not wired).
- **Error semantics**:
  - Invalid parameters → `STATUS_INVALID_PARAMETER`.
  - Non-FSCTL IOCTL flags → `STATUS_NOT_SUPPORTED`.
  - Missing open for per-handle IOCTLs → `STATUS_FILE_CLOSED`.
  - Underlying store failures bubble up as NTSTATUS.
- **DFS IOCTLs (current state)**:
  - `FSCTL_DFS_GET_REFERRALS{,_EX}` returns `STATUS_FS_DRIVER_REQUIRED` (server not DFS-capable yet), per `Server/SMB2/IOCtlHelper.cs`.

Key files:
- `SMBLibrary/Server/SMB2/IOCtlHelper.cs`
- `SMBLibrary/Server/ConnectionState/SMB2ConnectionState.cs`
- `SMBLibrary/SMB2/Commands/*` (codec and command models)

## SMB1 transactions (TRANS/TRANS2)
- **Multi-part assembly**: `Server/SMB1/TransactionHelper` reassembles primary + secondary requests into complete transactions using a per-process state (`ProcessStateObject`).
- **Subcommand dispatch**: Once complete, `TransactionSubcommand.GetSubcommandRequest(...)` parses the subcommand, logs, and routes to specific handlers.
- **Error and interim responses**: Interim responses returned while gathering secondaries; final error mapped to `ErrorResponse` when status not success.
- **DFS**: SMB1 scaffolding for `TRANS2_GET_DFS_REFERRAL` requests exists (wrappers) but server-side referral generation is not yet wired.

Key files:
- `SMBLibrary/Server/SMB1/TransactionHelper.cs`
- `SMBLibrary/SMB1/Transaction2Subcommands/*`

## INTFileStore and adapters
- **Contract**: `INTFileStore` exposes file/directory ops, filesystem info, security descriptors, change notifications, and `DeviceIOControl` for FSCTL passthrough.
- **Default adapter**: `SMBLibrary.Adapters.NTFileSystemAdapter` maps protocol semantics to the underlying filesystem abstraction, with:
  - Guardrails for ADS (`path.Contains(":")`) when streams unsupported.
  - Detailed status mapping (e.g., `STATUS_FILE_IS_A_DIRECTORY`, `STATUS_NO_SUCH_FILE`).
  - Verbose logging on I/O exceptions.
- **Share abstraction**: `FileSystemShare` provides access control via `AccessRequested` event and exposes `CachingPolicy`.

Key files:
- `SMBLibrary/NTFileStore/INTFileStore.cs`
- `SMBLibrary.Adapters/NTFileSystemAdapter/NTFileSystemAdapter.cs`
- `SMBLibrary/Server/Shares/FileSystemShare.cs`

## Connection/session management
- **Session lifecycle**: `AllocateSessionID()`, `CreateSession(...)`, `GetSession(...)`, `RemoveSession(...)` with locking.
- **Async contexts**: `CreateAsyncContext(...)`, `GetAsyncContext(...)`, `RemoveAsyncContext(...)` manage outstanding async operations.
- **Keep-alives**: Optional background thread to send SMB keepalive based on inactivity window.

Key files:
- `SMBLibrary/Server/ConnectionState/SMB2ConnectionState.cs`
- `SMBLibrary/Server/SMBServer.cs`

## Error/status handling
- **NTSTATUS-first** approach across protocol paths.
- **Mapping**: Helpers convert exceptions from filesystem to NTSTATUS (e.g., in adapter `ToNTStatus(ex)`).
- **Protocol-specific validation**: Enforced in helpers (e.g., IOCTL FileId rules, FSCTL flags) with clear logging before returning errors.

## Security/authentication
- **Stack presence**: `SMBLibrary.Authentication.GSSAPI` namespace is referenced by `SMBServer`; tests validate NTLM signing/encryption and crypto primitives.
- **Session setup**: SMB2/3 signing and encryption keys computed per spec (see tests), indicating correct crypto pipeline wiring.
- **Extensibility**: Security provider (`GSSProvider`) is injected into `SMBServer` constructor for flexibility.

## Observability
- **Logging**: Event-driven logging via `LogEntryAdded`. Verbose messages around key protocol decisions (e.g., IOCTL failures, transaction subcommands).
- **Opportunity**: Consider a thin logging abstraction to allow consumers to plug in structured logging without affecting legacy targets.

## Performance & compatibility
- **Compatibility**: Multi-targeting down to `net20` limits use of newer runtime features, but maximizes reach.
- **Parsing**: Low-level buffer manipulation minimizes allocations; good for throughput.
- **Release packaging**: ILRepack merges Utilities into a single assembly for simpler deployment.
- **Future perf work**: In `netstandard2.0` builds, consider optional Span/Memory optimizations behind `#if` for hotspots.

## DFS status summary
- Present: DFSC request structure, SMB1 TRANS2 wrappers, IOCTL constants, SMB2 ShareFlags for DFS.
- Missing: DFSC response encoder/decoder, concrete referral entry structs, IOCTL wiring, and provider/service for namespace/referrals. See docs/repo-analysis.md and docs/dfs-enablement-plan.md for a stepwise plan.

---

# Best practices observed
- Clear layering (protocol ↔ shares ↔ store) and SOLID separation via interfaces.
- Defensive, spec-aligned parsing; status code discipline.
- Deterministic tests with vectors; internal APIs exposed to tests when needed.

## Change notifications (SMB2 CHANGE_NOTIFY) — contract, server semantics, cancel
- **Contract**
  - `INTFileStore.NotifyChange(out ioRequest, handle, filter, watchTree, outputBuffer, callback, context)` returns `STATUS_PENDING` to arm notifications, or `STATUS_NOT_SUPPORTED`/error.
- **Server semantics** (`Server/SMB2/ChangeNotifyHelper`)
  - Creates an `SMB2AsyncContext`, calls `FileStore.NotifyChange`, and returns an interim error response with `STATUS_PENDING` (async header).
  - If the store returns `STATUS_NOT_SUPPORTED`, it coerces to `STATUS_PENDING` to prevent client retry flooding, per in‑code rationale.
  - Completion callback builds `ChangeNotifyResponse` (or `ErrorResponse`) with async header and optional signing, enqueued via `SMBServer.EnqueueResponse`.
- **Cancel path** (`Server/SMB2/CancelHelper`)
  - Locates async context and calls `FileStore.Cancel(ioRequest)`. On success/`STATUS_CANCELLED`/`STATUS_NOT_SUPPORTED`, removes context and returns `STATUS_CANCELLED` async error response. Otherwise, sends no response (client timeout governs).
- **Tests**
  - No explicit CHANGE_NOTIFY unit tests were found under `SMBLibrary.Tests`; consider adding store fakes to validate pending/completion/cancel flows and buffer semantics.

## Oplocks and leases — current coverage and edge cases
- **Coverage observed**
  - `SMB2\Enums\Create\OplockLevel.cs` defines levels (None/Level2/Exclusive/Batch/Lease).
  - Client receive path accounts for unsolicited `OplockBreak` messages (MessageId `0xFFFFFFFFFFFFFFFF`) to avoid discarding valid breaks.
- **Gaps/notes**
  - No dedicated server‑side lease/oplock management tables or explicit break handling routines were located; creation/ack paths likely accept defaults.
  - If lease/oplock interop becomes a focus, introduce explicit state, break notification, and ack handling per `[MS-SMB2]` with tests for downgrade and break‑and‑retry scenarios.

## Win32 interop — contents and platform considerations
- **Project contents** (`SMBLibrary.Win32`)
  - `NTFileStore/NTDirectoryFileSystem.cs`, `PendingRequestCollection.cs` (filesystem provider utilities).
  - `Security/*` (Windows security helpers), `ProcessHelper.cs`, `ThreadingHelper.cs`.
- **Considerations**
  - Keep platform‑specific code isolated here; core library remains multi‑targeted (`net20; net40; netstandard2.0`).
  - When extending adapters on Windows, prefer adding Win32 shims here and integrating via `SMBLibrary.Adapters` (`INTFileStore` implementations).

## Client file store (SMB2FileStore) — operations and error mapping
- **Operations → SMB2 commands**
  - `CreateFile` → `CreateRequest` (captures `CreateAction` to `FileStatus`).
  - `ReadFile`/`WriteFile` set `Header.CreditCharge` from payload length (65,536 bytes/credit).
  - `QueryDirectory` paginates until status != `STATUS_SUCCESS`, appending directory entries.
  - `QueryInfo`/`SetInfo` for file and filesystem info; `GetSecurityInformation` decodes security descriptors.
  - `DeviceIOControl` handles FSCTLs and returns output on success/overflow.
  - `Disconnect` sends `TreeDisconnect`.
- **Error mapping & timeouts**
  - Returns server `Header.Status` directly when a response arrives.
  - If `WaitForCommand` returns null: maps to `STATUS_INVALID_SMB` when connection terminated, else `STATUS_IO_TIMEOUT`.
- **Not implemented (client)**
  - `LockFile`, `UnlockFile`, `NotifyChange`, `Cancel`, `SetFileSystemInformation` are placeholders.
  - Share‑level encryption is honored by sending via `SMB2Client.TrySendCommand(request, encryptShareData)`.

---

# File references (non-exhaustive)
- `SMBLibrary/Server/SMBServer.cs`
- `SMBLibrary/Server/SMB2/IOCtlHelper.cs`
- `SMBLibrary/Server/ConnectionState/SMB2ConnectionState.cs`
- `SMBLibrary/Server/SMB1/TransactionHelper.cs`
- `SMBLibrary/NTFileStore/INTFileStore.cs`
- `SMBLibrary.Adapters/NTFileSystemAdapter/NTFileSystemAdapter.cs`
- `SMBLibrary/Server/Shares/FileSystemShare.cs`
- `SMBServer/Program.cs`
- Tests under `SMBLibrary.Tests/*`
