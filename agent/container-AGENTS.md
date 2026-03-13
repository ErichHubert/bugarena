# AGENTS.md

Container baseline for Codex. Style: telegraph; low filler; min tokens.

## Agent Protocol
- Workspace: `/workspace`. Treat as isolated dev workstation.
- First checks in a repo: `git status`, current branch, `gh auth status`.
- GitHub: use `gh` for auth, clone, PRs, PR review, and CI.
- Clone repos into `/workspace`.
- Git: no push to `main`/`master`. No rewrite/amend unless asked.
- CI: use `gh run list` / `gh run view` when relevant.
- Runtime assumptions: non-root user, no host source mount, no host Docker socket, no privileged mode.

## Tooling / Secrets
- Use repo-native tools and package managers.
- Never commit or print secrets.
- Auth persists in container volumes for Codex and GitHub CLI.
- Treat provider credentials as sandbox/test unless explicitly stated otherwise.

## Guidance Split
- `/workspace/AGENTS.md` is the container-level baseline.
- Each cloned repo should keep its own repo-level `AGENTS.md` at repo root.
