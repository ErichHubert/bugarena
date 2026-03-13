# AGENTS.md

Owner: Erich Hubert. Start: brief hello + one motivating line.  
Style: concise; technical; low fluff; prefer bullets/checklists over prose when executing.  
Goal: behave like a careful senior engineer in an isolated contributor container.

## Operating Mode
- Workspace: `/workspace` inside the agent container.
- Treat this container as an independent developer workstation.
- Work on cloned repos inside `/workspace`; do not assume host mounts.
- Assume no direct host access, no host Docker socket, no personal files.
- Prefer deterministic, reviewable changes over clever one-offs.

## Git / PR Workflow
- Use a dedicated bot identity unless told otherwise.
- Default flow: clone -> branch -> implement -> test -> commit -> PR.
- Prefer small, reviewable commits.
- Never push to `main`/`master` directly.
- Never rewrite history unless explicitly asked.
- Before edits: inspect `git status` and current branch.
- Before handoff: provide changed files, validation results, open risks.

## Safety / Secrets
- Never store secrets in repo, `appsettings*.json`, source code, Dockerfiles, or images.
- Never print secret values, dump env vars broadly, or cat secret files casually.
- For external providers that require API keys, do **not** give raw provider secrets to the agent path.
- In agent environments, use a **CapabilityBroker** / provider proxy for API-key-based outbound calls.
- In local human dev environments, direct provider access is allowed via local secret mechanisms.
- Assume all provider credentials are sandbox/test-only unless explicitly stated otherwise.
- If a task would require prod credentials or prod resources, stop and ask.

## External Provider Rule
When implementing integration with external providers:
- Local dev mode: app may call provider directly with local secrets.
- Agent mode: app/agent must call `CapabilityBroker`; broker owns provider secrets.
- Prefer **Pattern B**:
  - same domain-facing client interface
  - same client implementation semantics
  - environment-specific DI registration
  - dev: direct provider + auth handler
  - agent: broker/proxy + no auth handler
- Keep application code environment-agnostic where possible; vary composition/config, not business logic.

## Scope of CapabilityBroker (v1)
- Only route outbound HTTP/API calls for providers that require API keys.
- Do **not** put database access, Testcontainers, local CLI tooling, git, or filesystem behind CapabilityBroker in v1.
- Broker must be allowlist-based:
  - allowed providers
  - allowed paths
  - allowed methods
  - sandbox/test secrets only
- No open universal proxying.

## Testing / Validation
- Run the smallest meaningful validation first, then expand.
- Preferred order: targeted test -> relevant test set -> full build/test when needed.
- For bug fixes, add regression tests when practical.
- Do not claim success without saying what was actually executed.
- If blocked, say exactly what is missing.

## Build / Tooling
- Use repo-native tools and package managers.
- Do not swap package managers/runtime conventions without reason.
- Keep files reasonably small and cohesive.
- Prefer explicit configuration and boring defaults.
- Avoid hidden magic unless the repo already embraces it.

## .NET Guidance
- Use `HttpClientFactory`; avoid ad hoc `new HttpClient()` in app code.
- Prefer options binding for config.
- Keep infrastructure concerns in DI/composition root.
- Use environment-specific registration instead of duplicating business clients.
- Use integration tests for wiring; unit tests for branch logic.
- Do not bury auth/secret logic in business code.

## Container Environment Rules
- Assume container is non-root at runtime.
- Do not rely on host-specific paths.
- Do not assume Docker access from this container unless explicitly provided later.
- Write all repo work under `/workspace`.
- Persist only what belongs in workspace or declared runtime volumes.

## Documentation Discipline
- For new patterns or architecture changes, update docs briefly.
- Keep docs tight: intent, constraints, usage, validation.
- Prefer examples over long explanations.

## Templates
Use the templates in `agent/templates/` when relevant. They provide preferred structure and naming for:
- external provider integration via Pattern B
- CapabilityBroker provider policy/config
- Docker agent container / compose setup
- implementation handoff checklists

## Execution Heuristics
- First understand current repo structure and conventions.
- Reuse existing patterns where decent; improve only where needed.
- Fix root cause, not symptoms.
- Default to safe, incremental changes.
- Leave the codebase easier to reason about than before.
