# Bugarena Agent Container

This repository contains the local agent container scaffold plus a YARP-based `CapabilityBroker` service. The stack stays isolated: named Docker volumes only, no host repo mount, no host home mount, and no Docker socket inside the agent container.

Requires Docker with the Compose plugin on the host that builds and runs the container.

## What this project is

- a reproducible local workstation for agent-driven development work
- a sidecar `CapabilityBroker` that owns provider secrets and injects auth server-side
- a reference implementation of the repo rule: human dev can talk to providers directly, agent-mode traffic goes through a broker

In practice, this repo is infrastructure, not an application product. You use it to boot an isolated Codex-capable workspace plus a tightly scoped outbound proxy for API-key-backed providers.

## Why it exists

- keep agent work inside Docker-managed volumes instead of mounting the host repo or host home directory
- keep provider API keys out of the agent container and out of application code paths
- make local agent runs repeatable with a known toolchain, startup behavior, and network shape
- provide one place to evolve the broker pattern, templates, and validation around provider integrations

## Architecture at a glance

1. `agent` is the interactive workstation container where Codex, `dotnet`, `node`, and `gh` run.
2. `capability-broker` is a separate ASP.NET Core service that proxies only explicitly allowed upstream provider routes.
3. `docker-daemon` is an optional test-infrastructure sidecar that provides an isolated Docker engine for Testcontainers-backed integration tests.
4. Docker configs provide non-secret broker metadata, and Docker secrets provide the secret bundle.
5. Agent-mode clients call `CAPABILITY_BROKER_BASE_URL`; the broker validates provider, method, path, and secret availability before forwarding.

## Repository layout

- `compose.agent.yml`: Compose services, named volumes, Docker secret/config mounts, the shared `agent-net` network, and the profile-gated `testinfra-net`.
- `Bugarena.sln`: .NET solution for the capability broker and tests.
- `Dockerfile.agent`: Image definition with Codex, GitHub CLI, Node.js, and the .NET SDK installed.
- `Dockerfile.capability-broker`: Multi-stage image for the YARP capability proxy.
- `docker-entrypoint.sh`: Idempotent startup setup for directories, default git config, and shell aliases.
- `agent/codex-home-agents.md`: Canonical global Codex instruction source that is copied into the container home.
- `agent/codex-home-config.toml`: Canonical default Codex config source that is copied into the container home.
- `src/CapabilityBroker/`: ASP.NET Core + YARP reverse proxy service with provider allowlists and secret-backed auth injection.
- `tests/CapabilityBroker.Tests/`: regression tests for proxying, allowlists, and startup validation.
- `tests/Bugarena.Platform.Tests/`: standalone platform smoke tests that validate the agent environment, broker reachability, and Testcontainers wiring.
- `config/capability-broker/`: repo-tracked non-secret provider config and placeholder secret bundle shape.
- `agent/templates/`: Reusable templates for provider integrations, broker policies, and implementation handoffs.

The guidance is split into three layers:
- `agent/codex-home-agents.md` is the canonical global Codex baseline stored in the repo.
- `/home/agent/.codex/AGENTS.md` is seeded from that baseline for the container user.
- `agent/codex-home-config.toml` is the canonical default Codex config stored in the repo.
- `/home/agent/.codex/config.toml` is seeded from that baseline for the container user.
- cloned repos may add a repo-root `AGENTS.md` only when they need repo-specific overrides
- without a repo-root `AGENTS.md`, the global home baseline remains the only instruction file

## Build and start

Build both images:

```bash
docker compose -f compose.agent.yml build agent capability-broker
```

Start the full stack in the background:

```bash
docker compose -f compose.agent.yml up -d
```

If you want to start the full stack with a real provider secret bundle in one line:

```bash
CAPABILITY_BROKER_SECRETS_FILE=.secrets/capability-broker/provider-secrets.json docker compose -f compose.agent.yml up -d --build
```

Open a shell inside the container:

```bash
docker compose -f compose.agent.yml exec agent bash
```

If you only need the workstation, `docker compose -f compose.agent.yml up -d agent` still works.

To start the optional Testcontainers backend for integration tests, enable the `testinfra` profile:

```bash
docker compose -f compose.agent.yml --profile testinfra up -d --wait
```

On first use with a new Codex volume, Docker seeds both `/home/agent/.codex/AGENTS.md` and `/home/agent/.codex/config.toml` from the image. On startup, the entrypoint only restores either file if it is missing or empty, so manual edits inside the named volumes are preserved.

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

If the cloned repository has its own root `AGENTS.md`, treat it as a repo-specific overlay on top of `/home/agent/.codex/AGENTS.md`.

If you customize `/home/agent/.codex/AGENTS.md` or `/home/agent/.codex/config.toml` inside the named volumes, those changes persist across container restarts. Rebuilds update the image baselines, not existing volumes.

Standard .NET workflows run normally from there:

```bash
dotnet restore
dotnet build
dotnet test
```

The agent container always receives `CAPABILITY_BROKER_BASE_URL=http://capability-broker:8080`.

For Docker-backed test infrastructure, the image also carries internal `BUGARENA_TESTINFRA_*` defaults. Shell sessions export the standard Docker/Testcontainers variables only when `docker-daemon` is actually present:

- `DOCKER_HOST=tcp://docker-daemon:2375`
- `TESTCONTAINERS_HOST_OVERRIDE=docker-daemon`
- `TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock`

That means:

- with `--profile testinfra`, repo tooling inside `agent` can use the isolated `docker-daemon` sidecar
- without `--profile testinfra`, those standard Docker/Testcontainers variables stay unset, so other repos do not get a broken `DOCKER_HOST`

The broker URL is always available over `agent-net`. Docker-backed integration tests remain separate from `CapabilityBroker` and never use the host Docker socket.

## Validate the agent environment

The `tests/Bugarena.Platform.Tests` project is intentionally outside `Bugarena.sln`. It validates the isolated workstation itself and is meant to run inside the `agent` container, not on the host and not by launching a live Codex session.

For a local validation run from a host checkout:

```bash
docker compose -f compose.agent.yml --profile testinfra up -d --wait
docker compose -f compose.agent.yml exec -T agent mkdir -p /workspace/bugarena
AGENT_ID=$(docker compose -f compose.agent.yml ps -q agent)
docker cp ./. "${AGENT_ID}:/workspace/bugarena"
docker compose -f compose.agent.yml exec -T --user root agent chown -R agent:agent /workspace/bugarena
docker compose -f compose.agent.yml exec -T agent bash -lc 'cd /workspace/bugarena && dotnet restore Bugarena.sln'
docker compose -f compose.agent.yml exec -T agent bash -lc 'cd /workspace/bugarena && dotnet build Bugarena.sln --no-restore'
docker compose -f compose.agent.yml exec -T agent bash -lc 'cd /workspace/bugarena && dotnet test Bugarena.sln --no-build'
docker compose -f compose.agent.yml exec -T agent bash -lc "cd /workspace/bugarena && dotnet test tests/Bugarena.Platform.Tests/Bugarena.Platform.Tests.csproj"
```

The platform smoke tests cover:

- the non-interactive toolchain inside `agent` (`dotnet`, `git`, `gh`, and `codex --version`)
- `CapabilityBroker` readiness through `CAPABILITY_BROKER_BASE_URL`
- a Testcontainers-backed `postgres:16-alpine` container created through `docker-daemon`

GitHub Actions uses the same model: the runner orchestrates Docker, but the authoritative validation commands run inside `agent`. CI never starts an interactive Codex session and never performs `codex login`.

## Capability broker

`CapabilityBroker` is an allowlist-based outbound proxy for API-key-backed providers. It is built with ASP.NET Core and YARP and is intended for agent-mode traffic only. Provider secrets stay in the proxy service; callers send normal requests to:

```text
/providers/{provider}/{allowed-upstream-path}
```

The service validates:
- configured provider name
- allowed HTTP method
- allowed path prefix
- required secret availability

This keeps the trust boundary narrow: the agent can make domain-level provider calls, but it does not get raw provider keys and it cannot turn the broker into a general-purpose internet proxy.

Broker scope:
- yes: outbound HTTP for allowlisted API-key-backed providers
- yes: server-side auth injection using the configured secret bundle
- no: arbitrary URL forwarding
- no: database, filesystem, git, or CLI brokering
- no: storing provider secrets in source, appsettings, or the agent container image

Health endpoints:
- `/health/live`
- `/health/ready`

## Provider configuration

Non-secret provider metadata lives in `config/capability-broker/providers.json`. The checked-in default is empty so the compose stack stays safe to start. Add providers by editing that file.

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

Supported auth modes:

- `None`: no auth injection.
- `BearerToken`: injects `Authorization: <Scheme> <SecretKey>`. Required: `SecretKey`. Optional: `Scheme` (defaults to `Bearer`).
- `ApiKeyHeader`: injects the secret into a custom request header. Required: `SecretKey`, `HeaderName`.
- `QueryApiKey`: injects the secret into a query-string parameter. Required: `SecretKey`, `QueryParameterName`.

Example auth fragments:

```json
{
  "Auth": {
    "Type": "ApiKeyHeader",
    "SecretKey": "anthropic-sandbox",
    "HeaderName": "x-api-key"
  }
}
```

```json
{
  "Auth": {
    "Type": "QueryApiKey",
    "SecretKey": "finnhub-sandbox",
    "QueryParameterName": "token"
  }
}
```

## Secret handling

Provider secrets are not stored in source, `appsettings`, or Dockerfiles. Compose mounts a Docker secret into the `CapabilityBroker` container and the service reads the secret bundle from the mounted file path.

Default compose behavior points at the placeholder bundle:

```text
config/capability-broker/provider-secrets.placeholder.json
```

For a real local setup, create an ignored secret file and point Compose at it:

```bash
mkdir -p .secrets/capability-broker
cat > .secrets/capability-broker/provider-secrets.json <<'EOF'
{
  "Secrets": {
    "openai-sandbox": "replace-with-sandbox-key"
  }
}
EOF

CAPABILITY_BROKER_SECRETS_FILE=.secrets/capability-broker/provider-secrets.json \
  docker compose -f compose.agent.yml up -d capability-broker
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

If you started the `testinfra` profile, shut it down with:

```bash
docker compose -f compose.agent.yml --profile testinfra down -v
```
