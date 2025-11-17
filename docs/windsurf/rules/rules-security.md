---
trigger: model_decision
---

# Rules — Security [Model Decision]

- Never commit secrets. Use env vars or local secret stores. Redact tokens/keys from logs and telemetry.
- Validate inputs at boundaries; encode outputs to avoid injection. Treat external data as untrusted.
- Guard external calls with timeouts/retries/circuit breakers; prefer least-privilege credentials.
- Review dependencies regularly; pin versions for critical components. Remove unused packages/config.
- Handle PII with care: avoid storing unless required; redact in logs; document retention.
