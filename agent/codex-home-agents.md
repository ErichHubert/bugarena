# Codex Home Baseline

READ THIS FIRST. Global Codex baseline for any repo in this container.

Style: concise; technical; low fluff.

## Operating Mode
- Workspace: `/workspace`.
- Treat the container as an isolated dev workstation.
- Clone repos into `/workspace`.
- Look for repo runtime declarations such as `mise.toml`, `.tool-versions`, `global.json`, `.nvmrc`, and `.python-version` before building or testing.
- Prefer deterministic, reviewable changes over clever one-offs.

## Git / PR Workflow
- First in any repo: `git status`, current branch, `gh auth status`.
- Use `gh` for auth, clone, PRs, reviews, and CI when available.
- Prefer small, reviewable commits.
- Never push to `main` or `master` directly.
- Never rewrite history unless explicitly asked.
- Do not stop at implementation. A task is done only when the requirement is met, the change is self-reviewed and validated, and affected docs or supporting artifacts are updated so the PR is ready for review.
- If any part is missing, say exactly what is unverified or deferred.
- Before handoff: provide changed files, validation results, and open risks.

## Environment
- non-root user
- no host source mount
- no host Docker socket
- no privileged mode
- Repos under `/workspace` are trusted for `mise` config by default.

## Test Infrastructure
- If `DOCKER_HOST` is set in a shell session, assume the isolated `docker-daemon` Testcontainers backend is available.
- Use that backend for Docker-backed integration tests; do not assume host Docker access exists.
- If `DOCKER_HOST` is not set, do not assume Docker-backed test infrastructure is available.
- Keep test infrastructure separate from `CapabilityBroker`; broker rules are only for secret-bearing outbound HTTP providers.

## Tools / Secrets
- Use repo-native tools and package managers.
- Use `mise` to install repo-declared runtimes before build/test when those runtime files are present.
- Prefer `mise exec -- <command>` for scripted or non-interactive commands.
- Do not reach for OS package managers when a runtime or version requirement can be satisfied through `mise`.
- Never commit or print secrets.
- Treat provider credentials as sandbox/test unless explicitly stated otherwise.
- Keep provider secrets out of source, Dockerfiles, prompts, and app code on agent-mode paths.

## CapabilityBroker
- If `CAPABILITY_BROKER_BASE_URL` is set, assume a `CapabilityBroker` is available for any repo in this container.
- For agent-mode outbound calls to secret-backed external APIs, prefer broker routes: `${CAPABILITY_BROKER_BASE_URL}/providers/{provider}/{allowed-upstream-path}`.
- If `CAPABILITY_BROKER_BASE_URL` is not set, do not assume broker access exists.

## External Provider Rule
- Local dev: direct provider + auth handler.
- Agent mode: broker/proxy + no auth handler.
- Prefer one domain-facing client with environment-specific composition.
- Keep application code environment-agnostic where possible; vary wiring, not business logic.

## CapabilityBroker Scope
- Only route outbound HTTP/API calls for secret-backed external APIs.
- Do not put database access, Testcontainers, local CLI tooling, git, or filesystem behind `CapabilityBroker`.
- Keep broker access allowlist-based: provider, path, method, sandbox/test secrets.
- No open universal proxying.

## Validation
- Run the smallest meaningful validation first, then expand.
- Preferred order: targeted check -> relevant test set -> broader build/test when needed.
- Do not claim success without saying what actually ran.
- If blocked, say exactly what is missing.

## Build / Tooling
- Use repo-native tools and package managers.
- Do not swap package managers or runtime conventions without reason.
- In interactive shells, run `mise install` once per repo when runtime declarations exist, then use the repo-native commands.
- Prefer explicit configuration and boring defaults.

## Documentation
- Update docs briefly for new patterns or architecture changes.
- Keep docs tight: intent, constraints, usage, validation.
