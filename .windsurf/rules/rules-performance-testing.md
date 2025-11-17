---
trigger: model_decision
---

# Rules — Performance Testing [Model Decision]

- Budgets: define p95 latency and throughput targets per critical path; track regressions.
- Load: use k6 or similar for HTTP scenarios; seed minimal datasets; keep tests repeatable.
- Microbench: use BenchmarkDotNet for .NET hotspots; document scenarios and baselines.
- Telemetry: capture spans/metrics during perf runs; store baselines and compare in PRs when feasible.
