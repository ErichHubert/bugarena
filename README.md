# Bugarena Agent Container

This repository contains the local agent container scaffold plus a YARP-based `CapabilityProvider` service. The stack stays isolated: named Docker volumes only, no host repo mount, no host home mount, and no Docker socket inside the agent container.

Requires Docker with the Compose plugin on the host that builds and runs the container.

## Repository layout

- `compose.agent.yml`: Compose services, named volumes, Docker secret/config mounts, and the shared `agent-net` network.
- `Bugarena.sln`: .NET solution for the capability provider and tests.
- `Dockerfile.agent`: Image definition with Codex, GitHub CLI, Node.js, and the .NET SDK installed.
- `Dockerfile.capability-provider`: Multi-stage image for the YARP capability proxy.
- `docker-entrypoint.sh`: Idempotent startup setup for directories, default git config, and shell aliases.
- `src/CapabilityProvider/`: ASP.NET Core + YARP reverse proxy service with provider allowlists and secret-backed auth injection.
- `tests/CapabilityProvider.Tests/`: regression tests for proxying, allowlists, and startup validation.
- `config/capability-provider/`: repo-tracked non-secret provider config and placeholder secret bundle shape.
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

Build both images:

```bash
docker compose -f compose.agent.yml build agent capability-provider
```

Start the full stack in the background:

```bash
docker compose -f compose.agent.yml up -d
```

Open a shell inside the container:

```bash
docker compose -f compose.agent.yml exec agent bash
```

If you only need the workstation, `docker compose -f compose.agent.yml up -d agent` still works.

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
- `user.email=YOUR_GITHUB_ID+YOUR_GITHUB_USERNAME@users.noreply.github.com`
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

The agent container receives `CAPABILITY_PROVIDER_BASE_URL=http://capability-provider:8080`, so agent-mode clients can target the internal proxy over `agent-net`.

## Capability provider

`CapabilityProvider` is an allowlist-based outbound proxy for API-key-backed providers. It is built with ASP.NET Core and YARP and is intended for agent-mode traffic only. Provider secrets stay in the proxy service; callers send normal requests to:

```text
/providers/{provider}/{allowed-upstream-path}
```

The service validates:
- configured provider name
- allowed HTTP method
- allowed path prefix
- required secret availability

Health endpoints:
- `/health/live`
- `/health/ready`

## Provider configuration

Non-secret provider metadata lives in `config/capability-provider/providers.json`. The checked-in default is empty so the compose stack stays safe to start. Add providers by editing that file.

Example:

```json
{
  "CapabilityBroker": {
    "RequestTimeoutSeconds": 100,
    "Providers": {
      "openai": {
        "BaseUrl": "https://api.openai.com",
        "AllowedMethods": ["POST"],
        "AllowedPathPrefixes": ["/v1/responses", "/v1/embeddings"],
        "Auth": {
          "Type": "BearerToken",
          "SecretKey": "openai-sandbox"
        }
      }
    }
  }
}
```

## Secret handling

Provider secrets are not stored in source, `appsettings`, or Dockerfiles. Compose mounts a Docker secret into the `CapabilityProvider` container and the service reads the secret bundle from the mounted file path.

Default compose behavior points at the placeholder bundle:

```text
config/capability-provider/provider-secrets.placeholder.json
```

For a real local setup, create an ignored secret file and point Compose at it:

```bash
mkdir -p .secrets/capability-provider
cat > .secrets/capability-provider/provider-secrets.json <<'EOF'
{
  "Secrets": {
    "openai-sandbox": "replace-with-sandbox-key"
  }
}
EOF

CAPABILITY_PROVIDER_SECRETS_FILE=.secrets/capability-provider/provider-secrets.json \
  docker compose -f compose.agent.yml up -d capability-provider
```

The bundle key must match the provider `Auth.SecretKey` value.

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
