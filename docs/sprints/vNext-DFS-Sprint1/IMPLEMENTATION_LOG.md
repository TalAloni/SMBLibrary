# vNext-DFS-Sprint1 Implementation Log

## [2025-11-16] DFSC-A2-Guard-HeaderOnlyReferral - DFSC negative-path guard for header-only referrals

**Implemented**: Added a negative-path guard in `ResponseGetDfsReferral` so that when `NumberOfReferrals > 0` but the DFSC buffer only contains the 8-byte header (no referral entries), the constructor throws an `ArgumentException` instead of accepting a malformed response.
**Tests Added**: 1 unit, 0 integration
**Files Changed**: `SMBLibrary/DFS/ResponseGetDfsReferral.cs`, `SMBLibrary.Tests/DFS/ResponseGetDfsReferralTests.cs`
**AC Status**: Partially met (Phase A2 – codec hardening & negative paths; additional malformed/offset/overflow cases still planned).

## [2025-11-16] DFS-B1-ResolutionResult-OriginalPath - DfsResolutionResult original path plumbing

**Implemented**: Extended `DfsResolutionResult` with an `OriginalPath` property and updated `DfsClientResolver` to populate it for both DFS-disabled and DFS-enabled-not-implemented paths. This ensures callers can inspect the original UNC path independently of any resolved path, aligning with the resolver/result design in the DFS client SPEC.
**Tests Added**: 0 new test classes, 2 assertions added in existing unit tests (`DfsClientResolverTests`).
**Files Changed**: `SMBLibrary/Client/DFS/DfsResolutionResult.cs`, `SMBLibrary/Client/DFS/DfsClientResolver.cs`, `SMBLibrary.Tests/Client/DfsClientResolverTests.cs`
**AC Status**: Partially met (Phase B1 – extend `DfsResolutionResult` for downstream needs; additional metadata and selection logic to follow in later slices).

## [2025-11-16] DFSC-A1-SingleReferral-StubEntries - DFSC codec happy-path stub for single referral

**Implemented**: Added a minimal happy-path behavior in `ResponseGetDfsReferral` so that when `NumberOfReferrals > 0` and the buffer has more than the 8-byte header, the codec populates `ReferralEntries` with stub referral entry instances (without yet parsing full DFSC entry fields or string buffers). This unblocks early DFS client work by ensuring callers can distinguish “no referrals” from “at least one referral present”.
**Tests Added**: 1 unit (`ParseResponseGetDfsReferral_SingleReferral_PopulatesReferralEntries` in `ResponseGetDfsReferralTests`).
**Files Changed**: `SMBLibrary/DFS/ResponseGetDfsReferral.cs`, `SMBLibrary.Tests/DFS/ResponseGetDfsReferralTests.cs`
**AC Status**: Partially met (Phase A1 – DFSC models & happy-path codec; full field/offset parsing and negative-path coverage still to follow).

## [2025-11-16] DFSC-A1-DfsReferralEntryV1-Model - DFSC v1 entry model

**Implemented**: Introduced `DfsReferralEntryV1` as a concrete subclass of `DfsReferralEntry` with basic properties (`VersionNumber`, `Size`, `TimeToLive`, `DfsPath`, `NetworkAddress`) to model v1 referral entries in managed code. No wire-format parsing is performed yet.
**Tests Added**: 1 unit (`DfsReferralEntryV1Tests`).
**Files Changed**: `SMBLibrary/DFS/DfsReferralEntryV1.cs`, `SMBLibrary.Tests/DFS/DfsReferralEntryV1Tests.cs`
**AC Status**: Partially met (Phase A1 – DFSC entry models; header/offset parsing from raw buffers to follow in later slices).

## [2025-11-16] DFSC-A1-DfsReferralEntryV1-Strings - DFSC v1 string parsing

**Implemented**: Extended `ResponseGetDfsReferral` to parse DFS referral v1 string fields `DfsPath` and `NetworkAddress` using `DfsPathOffset` and `NetworkAddressOffset` (byte offsets from the start of each referral entry) and populate both the `DfsReferralEntryV1` properties and the `StringBuffer` collection. Only v1 entries are parsed; other versions still use stub entries.
**Tests Added**: 1 unit (`ParseResponseGetDfsReferral_SingleReferralV1_ParsesStringFields`) and an extended header test (`ParseResponseGetDfsReferral_SingleReferralV1_ParsesHeaderFields`) in `ResponseGetDfsReferralTests`.
**Files Changed**: `SMBLibrary/DFS/ResponseGetDfsReferral.cs`, `SMBLibrary.Tests/DFS/ResponseGetDfsReferralTests.cs`
**AC Status**: Partially met (Phase A1 – v1 happy-path header and string parsing; multi-entry support, additional versions, and negative-offset/bounds tests to follow).

## [2025-11-16] DFS-B2-DfsReferralSelector-SingleEntry - Pure resolver selection helper (single v1 referral)

**Implemented**: Added a pure resolver selection helper `DfsReferralSelector.SelectResolvedPath` in `SMBLibrary.Client.DFS` that, given an `originalPath`, `PathConsumed`, and a single `DfsReferralEntry` (currently v1), computes a resolved UNC path by replacing the DFS namespace prefix with the selected `NetworkAddress` while preserving the remaining suffix. This helper has no transport or caching dependencies.
**Tests Added**: 1 new test class `DfsReferralSelectorTests` with two unit tests covering simple path rewrite when the DFS path is a prefix (and when it equals the original path).
**Files Changed**: `SMBLibrary/Client/DFS/DfsReferralSelector.cs`, `SMBLibrary.Tests/Client/DfsReferralSelectorTests.cs`
**AC Status**: Partially met (Phase B2 – basic single-referral selection and path rewrite; multi-referral ordering, error handling, and integration into `DfsClientResolver` to follow).

## [2025-11-16] DFSC-A2-DfsReferralEntryV1-NegativePaths - DFSC v1 bounds and malformed input hardening

**Implemented**: Hardened `ResponseGetDfsReferral` v1 parsing with strict bounds checks for referral entry size and string offsets. The constructor now throws `ArgumentException` when a v1 entry declares a size that exceeds the remaining buffer, when the minimal header for an entry is truncated, or when `DfsPathOffset`/`NetworkAddressOffset` point outside the buffer. Happy-path tests were updated to construct minimally valid v1 headers.
**Tests Added**: 2 unit tests (`Ctor_WhenV1EntrySizeExceedsBuffer_ThrowsArgumentException`, `Ctor_WhenV1DfsPathOffsetOutsideBuffer_ThrowsArgumentException`) and an updated happy-path test (`ParseResponseGetDfsReferral_SingleReferral_PopulatesReferralEntries`) in `ResponseGetDfsReferralTests`.
**Files Changed**: `SMBLibrary/DFS/ResponseGetDfsReferral.cs`, `SMBLibrary.Tests/DFS/ResponseGetDfsReferralTests.cs`
**AC Status**: Partially met (Phase A2 – v1-specific negative paths and bounds checks; additional versions and more exhaustive malformed vectors remain for future slices).

## [2025-11-16] DFS-B2-DfsReferralSelector-MultiEntry - Multi-entry resolver selection helper

**Implemented**: Extended `DfsReferralSelector` with a multi-entry overload that takes an array of `DfsReferralEntry` instances and selects the first usable v1 referral using the existing single-entry helper. This provides a simple, deterministic selection strategy for multiple referrals without introducing priorities or target sets yet.
**Tests Added**: 2 unit tests in `DfsReferralSelectorTests` covering selection of the first usable entry and the case where no entries are usable (returns `null`).
**Files Changed**: `SMBLibrary/Client/DFS/DfsReferralSelector.cs`, `SMBLibrary.Tests/Client/DfsReferralSelectorTests.cs`
**AC Status**: Partially met (Phase B2 – basic multi-referral selection; priority/ordering semantics and integration into `DfsClientResolver` left for later slices).

## [2025-11-16] DFS-C1-IDfsReferralTransport-Interface - DFS referral transport seam

**Implemented**: Introduced the `IDfsReferralTransport` interface in `SMBLibrary.Client.DFS` to represent a DFS referral transport seam independent of SMB1/SMB2 specifics. A simple fake implementation used in tests allows callers to configure the returned `NTStatus`, buffer contents, and `OutputCount` to drive resolver behavior in later phases.
**Tests Added**: 1 new test class `DfsReferralTransportTests` with a unit test validating that a fake transport returns the expected status, buffer, and output count.
**Files Changed**: `SMBLibrary/Client/DFS/IDfsReferralTransport.cs`, `SMBLibrary.Tests/Client/DfsReferralTransportTests.cs`
**AC Status**: Partially met (Phase C1 – transport seam interface in place; real SMB2/SMB1 transport implementations and resolver wiring will follow in subsequent slices).

## [2025-11-16] DFS-C2-DfsClientResolver-SuccessPath-SingleV1 - Resolver success path via DFSC codec and selector

**Implemented**: Extended `DfsClientResolver` to use an injected `IDfsReferralTransport` and the DFSC codec for DFS-enabled paths. On `STATUS_SUCCESS`, the resolver now parses the DFSC buffer with `ResponseGetDfsReferral`, then uses `DfsReferralSelector` and `PathConsumed` to compute a resolved UNC path for a single v1 referral entry. `STATUS_FS_DRIVER_REQUIRED` is mapped to `NotApplicable` (fall back to the original path), while other statuses still map to `Error` with the original path.
**Tests Added**: Additional tests in `DfsClientResolverTests`, including a new success-path test (`Resolve_WhenDfsEnabledAndTransportReturnsSuccessWithSingleV1Referral_ReturnsSuccessAndRewrittenPath`) that uses a fake transport and a minimal v1 DFSC buffer.
**Files Changed**: `SMBLibrary/Client/DFS/DfsClientResolver.cs`, `SMBLibrary.Tests/Client/DfsClientResolverTests.cs`
**AC Status**: Partially met (Phase C2/B2 integration – basic success path over a single v1 referral; multi-entry referral order, TTL/caching, and real SMB2/SMB1 transports remain for future slices).

## [2025-11-16] DFS-B3-DfsClientResolver-Cache-TtlV1 - Minimal in-memory referral cache (single v1 path)

**Implemented**: Added a per-instance, in-memory DFS referral cache inside `DfsClientResolver`, keyed by `originalPath` and populated only for DFS-enabled resolutions with a v1 referral and a positive `TimeToLive`. Cached entries store the resolved UNC path and an expiration timestamp computed from the v1 `TimeToLive`. Subsequent resolutions for the same path, while TTL is valid, return the cached result without calling the transport; expired entries are evicted on access.
**Tests Added**: Two new tests in `DfsClientResolverTests` (`Resolve_WhenDfsEnabledAndV1ReferralHasTtl_CachesResultAndSkipsSecondTransportCall`, `Resolve_WhenDfsEnabledAndV1ReferralHasZeroTtl_DoesNotCacheResult`) using the fake transport with call counting and v1 DFSC buffers.
**Files Changed**: `SMBLibrary/Client/DFS/DfsClientResolver.cs`, `SMBLibrary.Tests/Client/DfsClientResolverTests.cs`
**AC Status**: Partially met (Phase B3 – basic TTL-aware caching per resolver instance for v1-only; multi-path keys, connection scoping, and eviction limits remain for later phases).

## [2025-11-16] DFSC-A1-DfsReferralEntryV2-Model - DFSC v2 entry model

**Implemented**: Introduced `DfsReferralEntryV2` as a concrete subclass of `DfsReferralEntry` with basic properties for version 2 referrals, including header fields (`VersionNumber`, `Size`, `TimeToLive`, `ServerType`, `ReferralEntryFlags`) and string fields (`DfsPath`, `DfsAlternatePath`, `NetworkAddress`). No wire-format parsing is performed yet.
**Tests Added**: 1 unit (`DfsReferralEntryV2Tests`) verifying that the model properties preserve assigned values and that instances are usable via the abstract base type.
**Files Changed**: `SMBLibrary/DFS/DfsReferralEntryV2.cs`, `SMBLibrary.Tests/DFS/DfsReferralEntryV2Tests.cs`
**AC Status**: Partially met (Phase A1 – DFSC entry models; v2 header/offset parsing and negative-path tests will follow in later slices).
