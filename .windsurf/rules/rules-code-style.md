---
trigger: model_decision
---

# Rules — Code Style (SMBLibrary)

- Targets: `net20; net40; netstandard2.0` in core. Prefer classic C# constructs compatible with older frameworks.
- Names:
  - Private fields use `m_` prefix (e.g., `m_sessionID`).
  - PascalCase for types/methods/properties; ALLCAPS only for constants.
  - 4-space indentation; braces on new line consistent with existing files.
- Namespaces & language features:
  - Do NOT use file-scoped namespaces, records, or modern keywords that break older targets.
  - Avoid `Span<T>/Memory<T>` and `ref struct` in core; if ever needed, guard with `#if NETSTANDARD2_0`.
- Exceptions vs statuses:
  - Protocol paths return `NTStatus` codes; throw exceptions only for truly exceptional/internal errors.
- Allocation & performance:
  - Favor explicit loops and buffer reuse over LINQ/alloc-heavy patterns.
  - Use `LittleEndianReader/Writer`, `ByteReader/Writer` for encoding/decoding.
- Dependencies:
  - Keep core dependency-light. Do NOT add logging/telemetry frameworks to core; logging flows via `LogEntryAdded` events.
  - Platform-specific code goes in `SMBLibrary.Win32`; wire via adapters.
- Comments & headers:
  - Preserve copyright headers and concise comments explaining protocol decisions.
- Nullable/analyzers:
  - Do not enable nullable or add analyzer requirements that conflict with legacy targets. Keep style consistent with existing files.

References: SMBLibrary source tree; existing conventions in `SMBLibrary/*`.
