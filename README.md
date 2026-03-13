# Bugarena Agent Container

This repository contains the Phase 1 local agent container scaffold: an isolated developer workstation for Codex, GitHub CLI, and .NET work. The container uses named Docker volumes only and does not mount the host repo, host home directory, or Docker socket.

The guidance is split into two layers:
- `.config/AGENTS.md` is the canonical container baseline stored in the repo.
- `/home/agent/.config/AGENTS.md` is seeded from that baseline for the container user.
- each cloned repo should provide its own repo-root `AGENTS.md` for project-specific rules
- repo-root `AGENTS.md` should start with `READ /home/agent/.config/AGENTS.md BEFORE ANYTHING (skip if missing).`

## Build and start

Build the image:

```bash
docker compose -f compose.agent.yml build agent
```

Start the container in the background:

```bash
docker compose -f compose.agent.yml up -d agent
```

Open a shell inside the container:

```bash
docker compose -f compose.agent.yml exec agent bash
```

On first use with a new config volume, Docker seeds `/home/agent/.config/AGENTS.md` from the image. On startup, the entrypoint only restores that file if it is missing or empty, so manual edits inside the config volume are preserved. 

## Authenticate tools

Inside the running container, log into Codex with your ChatGPT Plus account:

```bash
codex login
```

If device auth is preferable:

```bash
codex login --device-auth
```

Authenticate GitHub CLI separately when needed:

```bash
gh auth login
gh auth setup-git
gh auth status
```

## Clone and work in `/workspace`

Clone repositories inside the container so work stays in the named workspace volume:

```bash
cd /workspace
gh repo clone OWNER/REPO
cd /workspace/REPO
codex
```

If the cloned repository has its own root `AGENTS.md`, that file should point to `/home/agent/.config/AGENTS.md` first and then apply repo-specific rules.

If you customize `/home/agent/.config/AGENTS.md` inside the volume, those changes persist across container restarts. Rebuilds update the image baseline, not existing config volumes.

Standard .NET workflows run normally from there:

```bash
dotnet restore
dotnet build
dotnet test
```

## Persistence

- Repositories and other working files persist in `/workspace` via the `agent_workspace` named volume.
- Codex auth persists in `/home/agent/.codex` via the `agent_codex` named volume.
- GitHub CLI auth and other config persist in `/home/agent/.config` via the `agent_config` named volume.
- Cache data persists in `/home/agent/.cache` via the `agent_cache` named volume.

Stop the environment without removing volumes:

```bash
docker compose -f compose.agent.yml down
```
