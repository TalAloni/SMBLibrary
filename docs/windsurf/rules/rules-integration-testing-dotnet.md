---
trigger: model_decision
---

# Rules — Integration Testing (.NET, SMBLibrary)

- Framework: MSTest.
- Scope: verify end‑to‑end protocol behaviors using in‑process server (`SMBLibrary.Server.SMBServer`) and client (`SMBLibrary.Client.SMB2Client`).

## Server harness
- Start `SMBServer` in‑process on a free local IP/port (prefer 127.0.0.1:445 alternative or a custom IP if 445 is busy). Use Direct TCP where possible.
- Register a `FileSystemShare` over a temp directory; ensure cleanup after tests.
- Subscribe to `LogEntryAdded` if you need diagnostics; do not assert on log strings.

## Client harness
- Use `SMB2Client` to connect, authenticate (as needed), `TreeConnect` to shares, and exercise file operations.
- Avoid sleeps. Use polling only where protocol semantics require waiting (e.g., ChangeNotify completion); prefer short timeouts.

## Determinism & isolation
- Each test uses its own temp directory/share name and avoids global ports. If tests run in parallel, allocate ports dynamically.
- Clean up: disconnect clients, stop server, delete temp directories.

## Patterns to test
- Negotiation (dialects 2.02/2.10/3.0 where enabled), signing/encryption key derivation sanity.
- CRUD and directory listings, information queries, rename/delete semantics.
- IOCTL pass‑throughs as applicable (e.g., reparse, query network info); assert NTSTATUS and payload sizes, not internal buffers.
- ChangeNotify: pending → completion → cancel flows (use small output buffers to exercise overflow behavior).

## Do not
- Do not introduce external containers/services.
- Do not rely on machine state (Windows file sharing services may occupy ports 139/445). Skip or dynamically rebind when unavailable.
- Do not emit external telemetry from the core; tests should not require OTEL/Serilog.

## Running
- Run locally with: `dotnet test -v minimal`

