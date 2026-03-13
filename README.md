# Bugarena Agent Container

This repository contains the Phase 1 local agent container scaffold: an isolated developer workstation for Codex, GitHub CLI, Node.js, and .NET work. The container uses named Docker volumes only and does not mount the host repo, host home directory, or Docker socket.

Requires Docker with the Compose plugin on the host that builds and runs the container.

## Repository layout

- `compose.agent.yml`: Compose service, named volumes, and internal network for the agent workstation.
- `Dockerfile.agent`: Image definition with Codex, GitHub CLI, Node.js, and the .NET SDK installed.
- `docker-entrypoint.sh`: Idempotent startup setup for directories, default git config, and shell aliases.
- `agent/AGENTS.md`: Contributor operating guidance for work done inside the container.
- `agent/templates/`: Reusable templates for provider integrations, broker policies, and implementation handoffs.

The guidance is split into three layers:
- `.config/AGENTS.md` is the canonical container baseline stored in the repo.
- `/home/agent/.config/AGENTS.md` is seeded from that baseline for the container user.
- `.codex/config.toml` is the canonical default Codex config stored in the repo.
- `/home/agent/.codex/config.toml` is seeded from that baseline for the container user.
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

On first use with a new config volume, Docker seeds `/home/agent/.config/AGENTS.md` from the image. On first use with a new Codex volume, Docker seeds `/home/agent/.codex/config.toml` from the image. On startup, the entrypoint only restores either file if it is missing or empty, so manual edits inside the named volumes are preserved.

The default Codex config baked into the image is:

```toml
model = "gpt-5.4"
personality = "pragmatic"
approval_policy = "never"
sandbox_mode = "workspace-write"
```

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

## Container defaults

On first start, the entrypoint seeds these global git defaults if they are not already configured:

- `user.name=Codex Agent`
- `user.email=codex-agent@example.com`
- `init.defaultBranch=main`
- `pull.rebase=false`

It also adds an `ll` shell alias and prints the detected versions for `node`, `npm`, `dotnet`, and `gh`.

## Clone and work in `/workspace`

Clone repositories inside the container so work stays in the named workspace volume:

```bash
cd /workspace
gh repo clone OWNER/REPO
cd /workspace/REPO
codex
```

If the cloned repository has its own root `AGENTS.md`, that file should point to `/home/agent/.config/AGENTS.md` first and then apply repo-specific rules.

If you customize `/home/agent/.config/AGENTS.md` or `/home/agent/.codex/config.toml` inside the named volumes, those changes persist across container restarts. Rebuilds update the image baselines, not existing volumes.

Standard .NET workflows run normally from there:

```bash
dotnet restore
dotnet build
dotnet test
```

## Persistence

- Repositories and other working files persist in `/workspace` via the `agent_workspace` named volume.
- Codex auth and config persist in `/home/agent/.codex` via the `agent_codex` named volume.
- GitHub CLI auth and other config persist in `/home/agent/.config` via the `agent_config` named volume.
- Cache data persists in `/home/agent/.cache` via the `agent_cache` named volume.
- Global git config is written in `/home/agent/.gitconfig`, so the entrypoint re-seeds its defaults when a new container is created unless you extend the image or mount additional home-directory state.

Stop the environment without removing volumes:

```bash
docker compose -f compose.agent.yml down
```
