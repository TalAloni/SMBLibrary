---
trigger: model_decision
---

# Rules — LLM & Prompting [Model Decision]

- Retrieval-first (RAG): ground responses; include short chunk citations (IDs) and keep quotes brief.
- Prompt hygiene: cap prompt size; avoid sending full docs; prefer summaries and IDs.
- Prompt registry/versioning: track prompt versions; log prompt hash and token counts; no raw prompts/responses in logs.
- Safety/resilience: timeouts + jittered retries; circuit breakers; do not block UI threads; stream when useful.
- Provider-agnostic: wrap behind a client; support backoffs and fallbacks; no provider-specific logic in handlers.
- Injection guards: validate/escape user inputs; avoid untrusted tool calls; constrain tools and functions.
- Evaluation: snapshot inputs/outputs (sanitized) for offline eval; keep golden baselines under tests/.
