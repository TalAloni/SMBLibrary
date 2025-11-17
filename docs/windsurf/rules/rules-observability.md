---
trigger: model_decision
---

# Rules — Observability (SMBLibrary)

- Core logging is event‑driven. Use `LogEntryAdded` events and existing `Severity` levels (Verbose/Information/Debug/etc.).
- Do NOT add external logging or telemetry frameworks to the core library.
- Log protocol decisions at Verbose where helpful (e.g., IOCTL validation failures, transaction parsing errors).
- Never log secrets, credentials, or raw network payloads. Summarize sizes/types instead of dumping buffers.
- Prefer deterministic, concise messages that aid reproduction (e.g., SessionID, TreeID, FileId abbreviated).
- Host applications integrating SMBLibrary may wire logs to their preferred sinks (e.g., Serilog/ETW) outside the core.
