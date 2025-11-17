---
title: DFS Client Configuration Surface & API Wiring
status: proposed
date: 2025-11-16
---

## Context

DFS client internals for SMB2 (codec, resolver, DFS transport, DFS-aware INTFileStore adapter, and composition factory) are now implemented and merged behind `SMBLibrary.Client.DFS` without changing existing public client APIs.

The next step in vNext-DFS is to define how callers opt into DFS behavior from `SMB2Client` / `ISMBClient` without breaking existing consumers. The DFS client SPEC (§3.1, §3.3, §4) calls out open questions about:

- Where DFS configuration lives (constructors vs factories vs adapters).
- How to keep DFS disabled by default and per-client/per-connection.
- How to avoid breaking changes to `ISMBClient` / `ISMBFileStore`.

Two high-level options were considered:

- **Option A – Extend `SMB2Client` APIs directly**
  - Add DFS-aware constructor overloads and/or `TreeConnect` overloads that accept `DfsClientOptions`.
  - Potentially add a `DfsClientOptions` property on `SMB2Client`.

- **Option B – Introduce DFS-aware client/adapter via factory**
  - Keep `ISMBClient` and existing `SMB2Client` methods unchanged.
  - Expose a DFS-aware client or file store via a dedicated factory (in `SMBLibrary.Client.DFS`) that wires in the existing DFS resolver/adapter.

This ADR selects a conservative, factory-based approach for vNext-DFS, leaving room to add convenience overloads later without binding the core `SMB2Client` API to DFS-specific concerns.

## Decision

### 1. Keep existing public interfaces unchanged

- `ISMBClient` and `ISMBFileStore` **MUST NOT** change for vNext-DFS:
  - No new parameters on `ISMBClient.TreeConnect`.
  - No DFS-specific members on `ISMBClient` / `ISMBFileStore`.

- Existing `SMB2Client` constructors and `TreeConnect(string shareName, out NTStatus status)` remain unchanged and continue to return non-DFS-aware `SMB2FileStore` instances.

### 2. Introduce a DFS client factory in `SMBLibrary.Client.DFS`

- Add a new factory type in the DFS client namespace:

  - `SMBLibrary.Client.DFS.DfsClientFactory` (name may be adjusted slightly during implementation, but MUST live under `SMBLibrary.Client.DFS`).

- Initial public surface (conceptual signature):

  ```csharp
  namespace SMBLibrary.Client.DFS
  {
      public static class DfsClientFactory
      {
          // Creates a DFS-aware ISMBFileStore for a specific SMB2 share.
          public static ISMBFileStore CreateDfsAwareFileStore(
              SMB2Client client,
              string shareName,
              DfsClientOptions options,
              out NTStatus status);
      }
  }
  ```

- Behavior:

  - `options == null` or `options.Enabled == false`:
    - The factory MUST behave like a simple `TreeConnect`:
      - Call `client.TreeConnect(shareName, out status)`.
      - Return the resulting `ISMBFileStore` unchanged.

  - `options.Enabled == true`:
    - The factory MUST:
      1. Call `client.TreeConnect(shareName, out status)`.
         - If `status != STATUS_SUCCESS` or the returned store is null, propagate as-is.
      2. Internally compose a DFS-aware `INTFileStore` using existing DFS building blocks:
         - Use `DfsFileStoreFactory` + `Smb2DfsReferralTransport.CreateUsingDeviceIOControl` to wrap the underlying `SMB2FileStore` into a `DfsAwareClientAdapter`.
      3. Return the DFS-aware `ISMBFileStore` to the caller.

  - DFS remains completely opt-in:
    - Callers must explicitly call `DfsClientFactory.CreateDfsAwareFileStore` with `options.Enabled == true` to participate in DFS resolution.
    - Default/legacy usage of `SMB2Client.TreeConnect` is unchanged and non-DFS-aware.

### 3. SMB1 and future extensions

- For vNext-DFS, the factory is **SMB2-focused**:
  - First supported path is `SMB2Client` + SMB2/3 DFS IOCTLs.

- SMB1 DFS support MAY be added later via:
  - Additional overloads or methods on `DfsClientFactory` that accept SMB1-specific clients or file stores, still composing DFS behavior via `DfsFileStoreFactory` and appropriate SMB1 transport.

### 4. Naming & visibility

- `DfsClientOptions` remains in `SMBLibrary.Client.DFS` and is used as the options type for DFS configuration.
- `DfsClientFactory` is a **public** entry point so library consumers can opt into DFS explicitly.
- Internal types (`DfsFileStoreFactory`, `DfsAwareClientAdapter`, `Smb2DfsReferralTransport`) remain internal and are not exposed directly to consumers.

## Alternatives Considered

### A. Extend `SMB2Client` with DFS-aware overloads

- Example:

  ```csharp
  public ISMBFileStore TreeConnect(string shareName, DfsClientOptions options, out NTStatus status);
  ```

- Pros:
  - Very convenient for callers; no factory indirection.
  - `SMB2Client` feels like it "just supports DFS".

- Cons:
  - Bleeds DFS-specific concerns into the core client API.
  - Would either:
    - Require new signatures on `ISMBClient` (breaking interface change), or
    - Introduce `SMB2Client`-only methods that are not part of the shared interface.
  - Harder to keep clean separation between DFS code and non-DFS code.

- Decision: **defer**. We may add convenience overloads or wrappers in a later minor once the factory pattern is battle-tested.

### B. Client-level DFS-aware `ISMBClient` adapter

- Idea:
  - Wrap `ISMBClient` in a `DfsAwareSmbClient : ISMBClient` that intercepts `TreeConnect` and returns DFS-aware file stores.

- Pros:
  - Aligns with the adapter concept in the SPEC.
  - Keeps DFS logic entirely out of `SMB2Client`.

- Cons:
  - More complex to introduce up-front.
  - Adds an extra layer that may be redundant given we already have a DFS-aware `INTFileStore` adapter and a file-store-level factory.

- Decision: **not chosen for the initial vNext-DFS wiring**. We may still introduce a client-level adapter in the future if higher-level consumer ergonomics require it.

## Consequences

- **Pros**
  - No breaking changes to `ISMBClient` / `ISMBFileStore` or `SMB2Client`.
  - DFS behavior is explicitly opt-in via a factory method, clarifying when DFS is in play.
  - Keeps DFS implementation details (resolver, transport, adapter) behind the `SMBLibrary.Client.DFS` facade.
  - Fits the SPEC's guidance to use options/factories rather than altering existing constructors where possible.

- **Cons**
  - Callers must adopt a slightly different pattern (`DfsClientFactory.CreateDfsAwareFileStore`) instead of a more discoverable `SMB2Client` overload.
  - Future addition of convenience overloads on `SMB2Client` will need to wrap this factory to avoid duplicating composition logic.

- **Follow-up Work**
  - Update `docs/specs/dfs-client-spec.md` to:
    - Document `DfsClientFactory` as the primary DFS entry point for SMB2.
    - Clarify that `ISMBClient` / `SMB2Client` public APIs remain unchanged for vNext-DFS.
  - Implement `DfsClientFactory.CreateDfsAwareFileStore` in `SMBLibrary.Client.DFS` using the already-merged DFS internals.
  - Add MSTest coverage for the factory behavior (DFS disabled vs enabled, `STATUS_FS_DRIVER_REQUIRED`, successful referrals).
