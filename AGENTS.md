# AGENTS.md

READ `/workspace/AGENTS.md` BEFORE ANYTHING (skip if missing).

Repo rules only. Style: telegraph; low filler; min tokens.

## Workflow
- Use `gh` for GitHub work.
- Small, reviewable commits.
- No direct push to `main` or `master`.
- No history rewrite unless asked.

## Engineering
- Clean architecture. Clean code. Fix root cause, not symptoms.
- Reuse existing repo patterns before introducing new ones.
- Prefer explicit, boring configuration.
- Keep files small and cohesive.
- Prefer one file per class when it helps, not as dogma.
- Split or refactor before files become hard to review; roughly 500 LOC is a smell, not a law.

## Providers
- Never commit or print secrets.
- Agent-mode outbound API-key calls go through `CapabilityBroker`.
- Prefer one domain-facing client with environment-specific DI.
- Dev: direct provider + auth handler.
- Agent: broker/proxy + no auth handler.

## Validation / Docs
- Run the smallest meaningful validation first, then expand.
- Report exact commands run, results, and gaps.
- Add regression tests for bugs when practical.
- Update docs for every completed change.
- Use `agent/templates/` when helpful.

## Definition Of Done
- code is clean, cohesive, and consistent with the repo
- validation ran and was reported
- docs were updated
- branch was committed
- PR was opened with `gh pr create`
- PR title and description are meaningful and include summary, validation, and follow-ups or risks
