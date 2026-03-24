# AGENTS.md

READ THIS FIRST. Container baseline. Style: telegraph; low filler; min tokens.

## Baseline
- Workspace: `/workspace`.
- Treat container as isolated dev workstation.
- Clone repos into `/workspace`.
- First in any repo: `git status`, current branch, `gh auth status`.
- Use `gh` for auth, clone, PRs, reviews, and CI.
- Use `gh run list` and `gh run view` for CI when relevant.
- No push to `main` or `master`. No rewrite/amend unless asked.

## Environment
- non-root user
- no host source mount
- no host Docker socket
- no privileged mode

## Tools / Secrets
- Use repo-native tools and package managers.
- Never commit or print secrets.
- Auth persists in container volumes for Codex and GitHub CLI.
- Treat provider credentials as sandbox/test unless explicitly stated otherwise.

## CapabilityBroker
- If `CAPABILITY_BROKER_BASE_URL` is set, assume a `CapabilityBroker` is available for any repo in this container.
- For agent-mode outbound calls to API-key-backed providers, prefer broker routes: `${CAPABILITY_BROKER_BASE_URL}/providers/{provider}/{allowed-upstream-path}`.
- Do not put raw provider secrets in repo config, source, prompts, or app code for agent-mode paths.
- Prefer environment-specific composition:
  - dev: direct provider + auth handler
  - agent: broker/proxy + no auth handler
- If `CAPABILITY_BROKER_BASE_URL` is not set, do not assume broker access exists.

## Rule Split
- `/home/agent/.config/AGENTS.md` is the home baseline.
- If the repo has its own `AGENTS.md`, read that after this file.
