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

## Rule Split
- `/workspace/AGENTS.md` is the container baseline.
- If the repo has its own `AGENTS.md`, read that after this file.
