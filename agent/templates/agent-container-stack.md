# Template: Agent container stack

## Goal
Codex contributor container that behaves like an isolated dev workstation.

## Include
- git, gh, node + npm, `@openai/codex`, .NET SDK
- jq, rg, fd, curl, unzip, zip, python3

## Rules
- non-root runtime user
- named volume for `/workspace`
- seed `/workspace/AGENTS.md` from the image on container start
- expect repo-root `AGENTS.md` files to point back to `/workspace/AGENTS.md`
- persistent Codex config for `codex login`
- no host source mount
- no host Docker socket
- no privileged mode
- no provider secrets in the image
- no published ports unless required

## Flow
1. start container
2. entrypoint does harmless idempotent setup, writes container `AGENTS.md` to `/workspace/AGENTS.md`, and reminds the user to read it
3. run `codex login`
4. run `gh auth login` or `gh auth status`
5. run `gh repo clone ...` into `/workspace`
6. read repo-root `AGENTS.md` inside the cloned repo after the container baseline
7. branch, implement, test, commit, and `gh pr create`

## Entrypoint
- do: set git defaults, create dirs, print environment summary, exec command
- do not: force login, auto-clone repos, auto-start Codex, inject secrets
