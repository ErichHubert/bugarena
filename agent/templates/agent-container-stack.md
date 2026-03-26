# Template: Agent container stack

## Goal
Codex contributor container that behaves like an isolated dev workstation.

## Include
- git, gh, system node + npm/corepack, python3, `mise`, `@openai/codex`
- jq, rg, fd, curl, unzip, zip, file, make, cmake, and baseline native build tools

## Rules
- non-root runtime user
- named volume for `/workspace`
- named volume for `/home/agent`
- keep the canonical Codex home baseline in `agent/codex-home-agents.md`
- seed `/home/agent/.codex/AGENTS.md` from that baseline
- keep the canonical default Codex config in `agent/codex-home-config.toml`
- seed `/home/agent/.codex/config.toml` from that baseline
- do not keep a repo-root `AGENTS.md` in the container repo unless the repo itself truly needs overrides
- only restore the home `AGENTS.md` on startup if it is missing or empty
- only restore the home `config.toml` on startup if it is missing or empty
- allow cloned repos to provide repo-root `AGENTS.md` only for repo-specific overrides
- persistent Codex config for `codex login`
- no host source mount
- no host Docker socket
- no privileged mode
- no provider secrets in the image
- no published ports unless required
- do not assume app runtimes such as `.NET`, Go, Java, or Rust are baked into the image
- use `mise` as the standard runtime bootstrap layer for repo-specific languages and versions

## Flow
1. start container
2. entrypoint does harmless idempotent setup, restores the home `AGENTS.md` and home Codex `config.toml` if needed, and reminds the user to read `/home/agent/.codex/AGENTS.md`
3. run `codex login`
4. run `gh auth login` or `gh auth status`
5. run `gh repo clone ...` into `/workspace`
6. if the repo declares runtimes through `mise.toml` or compatible version files, run `mise install`
7. read repo-root `AGENTS.md` inside the cloned repo only if that repo provides repo-specific overrides
8. branch, implement, test, commit, and `gh pr create`

## Entrypoint
- do: set git defaults, create dirs, activate `mise` in shells, print environment summary, exec command
- do not: force login, auto-clone repos, auto-start Codex, inject secrets
