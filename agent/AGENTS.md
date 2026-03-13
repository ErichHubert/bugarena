# AGENTS.md

Erich owns this. Start: brief hi + 1 motivating line. Style: telegraph; low filler; min tokens.

## Agent Protocol
- Workspace: `/workspace`. Treat as isolated dev workstation.
- First checks: `git status`, current branch, `gh auth status`.
- GitHub: use `gh` for auth, clone, PRs, PR review, and CI.
- Default flow: confirm repo or `gh repo clone` -> branch -> implement -> test -> commit -> `gh pr create`.
- Git: small, reviewable commits. No push to `main`/`master`. No rewrite/amend unless asked.
- CI: use `gh run list` / `gh run view` when relevant.

## Engineering Rules
- Clean architecture. Clean code. Fix root cause, not symptoms.
- Reuse repo patterns first. Prefer boring, explicit configuration.
- Keep files small and cohesive. Prefer one file per class when it helps, not as dogma.
- Prefer splitting/refactoring before files become hard to review; use roughly 500 LOC as a smell, not a law.
- Use repo-native tools and package managers. No swaps without reason.

## Secrets / Providers
- Never commit or print secrets.
- Local human dev: direct provider access via local secret mechanisms is fine.
- Agent mode: API-key outbound calls go through `CapabilityBroker`.
- Prefer one domain-facing client with environment-specific DI.
- Dev: direct provider + auth handler.
- Agent: broker/proxy + no auth handler.
- `CapabilityBroker` v1: allowlisted provider HTTP only. Not database, filesystem, CLI, or universal proxy.

## Validation / Docs
- Run smallest meaningful validation first, then expand.
- Report exact commands run, results, and gaps.
- Add regression tests for bugs when practical.
- Update docs for every completed change.
- Use `agent/templates/` when helpful; keep output short.

## Definition Of Done
- code is clean, cohesive, and consistent with the repo
- validation ran and was reported
- docs were updated
- branch was committed
- PR was opened with `gh pr create`
- PR title and description are meaningful and include summary, validation, and follow-ups or risks
