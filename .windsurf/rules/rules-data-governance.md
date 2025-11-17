---
trigger: model_decision
---

# Rules — Data Governance [Model Decision]

- Minimize collection: only store what is needed; classify PII; document retention and deletion paths.
- Redaction: strip secrets/PII from logs and telemetry; sample logs conservatively.
- Access: least privilege; segregate dev/test/prod data; avoid production data in dev.
- Exports: support data export/deletion workflows; hash or tokenize PII when possible.
