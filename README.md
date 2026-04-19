# Bugarena Agent Container

This repository contains the local agent container scaffold plus a YARP-based `CapabilityBroker` service. The stack stays isolated: named Docker volumes only, no host repo mount, no host home mount, and no Docker socket inside the agent container.

Requires Docker with the Compose plugin on the host that builds and runs the container.

## What this project is

- a reproducible local workstation for agent-driven development work
- a sidecar `CapabilityBroker` that owns provider secrets and injects auth server-side
- a reference implementation of the repo rule: human dev can talk to providers directly, agent-mode traffic goes through a broker

In practice, this repo is infrastructure, not an application product. You use it to boot an isolated Codex-capable workspace with a lean baseline toolchain, repo-managed runtimes through `mise`, and a tightly scoped outbound proxy for secret-backed providers.

## Why it exists

- keep agent work inside Docker-managed volumes instead of mounting the host repo or host home directory
- keep provider API keys out of the agent container and out of application code paths
- make local agent runs repeatable with a known toolchain, startup behavior, and network shape
- provide one place to evolve the broker pattern, templates, and validation around provider integrations

## Architecture at a glance

1. `agent` is the interactive workstation container where Codex, `mise`, system `node`, `python3`, and `gh` run.
2. `capability-broker` is a separate ASP.NET Core service that proxies only explicitly allowed upstream provider routes.
3. `docker-daemon` is an optional test-infrastructure sidecar that provides an isolated Docker engine for Testcontainers-backed integration tests.
4. Docker configs provide non-secret broker metadata, and Docker secrets provide the secret bundle.
5. Agent-mode clients call `CAPABILITY_BROKER_BASE_URL`; the broker validates provider, method, path, and secret availability before forwarding.

## Repository layout

- `Bugarena.sln`: .NET solution for the capability broker and tests.
- `infra/docker/`: `compose.agent.yml`, the Dockerfiles, and `docker-entrypoint.sh` for the agent workstation, capability broker, and optional `docker-daemon` sidecar.
- `agent/codex-home-agents.md`: Canonical global Codex instruction source that is copied into the container home.
- `agent/skills/`: Curated Codex skills that are bundled into the agent image as shared admin skills.
- `agent/codex-home-config.toml`: Canonical default Codex config source that is copied into the container home.
- `mise.toml`: Repo-local runtime declaration for `mise`-managed toolchains used by CI and local validation.
- `src/CapabilityBroker/`: ASP.NET Core + YARP reverse proxy service with provider allowlists and secret-backed auth injection.
- `tests/CapabilityBroker.Tests/`: regression tests for proxying, allowlists, and startup validation.
- `tests/Bugarena.Platform.Tests/`: standalone platform smoke tests that validate the baseline agent environment, broker reachability, and Testcontainers wiring.
- `config/capability-broker/`: repo-tracked non-secret provider config and placeholder secret bundle shape.

Project docs:
- `CONTRIBUTING.md`: contribution and PR expectations
- `CODE_OF_CONDUCT.md`: contributor behavior expectations
- `SECURITY.md`: vulnerability reporting guidance
- `SUPPORT.md`: where to ask for help

The guidance is split into four layers:
- `agent/codex-home-agents.md` is the canonical global Codex baseline stored in the repo.
- `/home/agent/.codex/AGENTS.md` is seeded from that baseline for the container user.
- `agent/skills/` contains the source for curated Bugarena skills.
- `/etc/codex/skills/` is the admin skill path inside the container, populated from `agent/skills/`.
- `agent/codex-home-config.toml` is the canonical default Codex config stored in the repo.
- `/home/agent/.codex/config.toml` is seeded from that baseline for the container user.
- cloned repos may add a repo-root `AGENTS.md` when they need repo-specific overrides
- cloned repos may add `.agents/skills/` when they have repeatable repo-scoped workflows

## Released images

Published stable GitHub Releases push these public images to GHCR:

- `ghcr.io/erichhubert/bugarena-capability-broker`
- `ghcr.io/erichhubert/bugarena-agent`

Release publishing is release-only:

- only published stable releases tagged as `vX.Y.Z` push images
- each stable release publishes `X.Y.Z`, `X.Y`, and `latest`
- draft releases, prereleases, branch pushes, and pull requests do not publish images
- `docker-daemon` remains an internal helper image and is not published

Minimal pull examples:

```bash
docker pull ghcr.io/erichhubert/bugarena-capability-broker:0.1.0
docker pull ghcr.io/erichhubert/bugarena-agent:latest
```

Maintainer note for the first successful release publish:

- open each GHCR package page and set visibility to `Public`
- verify each package is linked to `ErichHubert/bugarena` and inherits repository access
- if a package name already exists and is not linked to this repository, connect it manually before rerunning the release workflow

## Build and start

The commands below assume your current working directory is the repository root.

1. Build both images from the host shell:

```bash
docker compose -f infra/docker/compose.agent.yml build agent capability-broker
```

2. Start the stack variant you want from the host shell:

Start the default stack in the background:

```bash
docker compose -f infra/docker/compose.agent.yml up -d
```

If you want to start the full stack with a real provider secret bundle in one line:

```bash
CAPABILITY_BROKER_SECRETS_FILE="$PWD/.secrets/capability-broker/provider-secrets.json" docker compose -f infra/docker/compose.agent.yml up -d --build
```

If you only need the workstation, this still works:

```bash
docker compose -f infra/docker/compose.agent.yml up -d agent
```

If you want the optional Testcontainers backend for integration tests, enable the `testinfra` profile before opening a shell in `agent`:

```bash
docker compose -f infra/docker/compose.agent.yml --profile testinfra up -d --wait
```

3. After the services you need are running, open a shell inside the container:

```bash
docker compose -f infra/docker/compose.agent.yml exec agent bash
```

On first use with a new `agent_home` volume, the entrypoint restores `/home/agent/.codex/AGENTS.md` and `/home/agent/.codex/config.toml` from the canonical copies in `/opt/codex-agent` when either file is missing or empty. Manual edits inside `/home/agent` are preserved across container restarts.

The default Codex config baked into the image is:

```toml
model = "gpt-5.4"
personality = "pragmatic"
approval_policy = "never"
sandbox_mode = "danger-full-access"
```

This image defaults to `danger-full-access` because the Bugarena container is intended to be the outer sandbox boundary. That avoids nested `bubblewrap` user-namespace requirements that commonly fail on hardened Ubuntu hosts and VMs.

If your container host allows unprivileged user namespaces and you explicitly want Codex's inner sandbox, override the config to `sandbox_mode = "workspace-write"`.

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

Git identity is not configured automatically. Set it explicitly inside the container before committing. If you prefer GitHub's private noreply email, use your own account-specific noreply address:

```bash
git config --global user.name "Your Name"
git config --global user.email "YOUR_GITHUB_ID+YOUR_GITHUB_USERNAME@users.noreply.github.com"
```

## Container defaults

On first start, the entrypoint seeds these non-identity global git defaults if they are not already configured:

- `init.defaultBranch=main`
- `pull.rebase=false`

It also adds an `ll` shell alias and prints the detected versions for `codex`, `gh`, `mise`, `node`, and `python3`.

## Clone and work in `/workspace`

Clone repositories inside the container so work stays in the named workspace volume:

```bash
cd /workspace
gh repo clone OWNER/REPO
cd /workspace/REPO
mise install
codex
```

If the cloned repository has its own root `AGENTS.md`, treat it as a repo-specific overlay on top of `/home/agent/.codex/AGENTS.md`.

If the cloned repository has `.agents/skills/`, Codex can use those repo-scoped skills in addition to the bundled admin skills under `/etc/codex/skills/`.
Repositories cloned under `/workspace` are trusted for `mise` by default, so `mise install` does not require an extra trust step inside the container.

If the cloned repository declares runtimes through `mise.toml` or compatible version files such as `.tool-versions`, `.nvmrc`, `.python-version`, or `global.json`, install them with `mise install` before building or testing.

If you customize anything under `/home/agent`, including `/home/agent/.codex`, auth state, caches, user-installed tools, or `mise` data, those changes persist across container restarts. Rebuilds update the image baselines, not existing volumes.

This repo declares `.NET 10` in `mise.toml`. In an interactive shell, the normal .NET commands work after `mise install`:

```bash
dotnet restore
dotnet build
dotnet test
```

For scripted or non-interactive commands, prefer the explicit form:

```bash
mise exec -- dotnet restore
mise exec -- dotnet build
mise exec -- dotnet test
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
docker compose -f infra/docker/compose.agent.yml --profile testinfra up -d --wait
docker compose -f infra/docker/compose.agent.yml exec -T agent mkdir -p /workspace/bugarena
AGENT_ID=$(docker compose -f infra/docker/compose.agent.yml ps -q agent)
docker cp ./. "${AGENT_ID}:/workspace/bugarena"
docker compose -f infra/docker/compose.agent.yml exec -T --user root agent chown -R agent:agent /workspace/bugarena
docker compose -f infra/docker/compose.agent.yml exec -T agent bash -lc 'cd /workspace/bugarena && find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +'
docker compose -f infra/docker/compose.agent.yml exec -T agent bash -lc 'cd /workspace/bugarena && mise install'
docker compose -f infra/docker/compose.agent.yml exec -T agent bash -lc 'cd /workspace/bugarena && mise exec -- dotnet restore Bugarena.sln'
docker compose -f infra/docker/compose.agent.yml exec -T agent bash -lc 'cd /workspace/bugarena && mise exec -- dotnet build Bugarena.sln --no-restore'
docker compose -f infra/docker/compose.agent.yml exec -T agent bash -lc 'cd /workspace/bugarena && mise exec -- dotnet test Bugarena.sln --no-build'
docker compose -f infra/docker/compose.agent.yml exec -T agent bash -lc "cd /workspace/bugarena && mise exec -- dotnet test tests/Bugarena.Platform.Tests/Bugarena.Platform.Tests.csproj"
```

The cleanup step removes host-generated `bin/` and `obj/` artifacts before the container rebuilds the repo. That avoids platform-specific test adapter collisions such as duplicate xUnit runner discovery warnings.

The platform smoke tests cover:

- the non-interactive baseline toolchain inside `agent` (`git`, `gh`, `codex`, and `mise`)
- `CapabilityBroker` readiness through `CAPABILITY_BROKER_BASE_URL`
- a Testcontainers-backed `postgres:16-alpine` container created through `docker-daemon`

GitHub Actions uses the same model: the runner orchestrates Docker, but the authoritative validation commands run inside `agent`. CI bootstraps this repo's `.NET` runtime with `mise` before restore/build/test. CI never starts an interactive Codex session and never performs `codex login`.

## Security automation

The repository uses a layered GitHub security baseline. Some checks run as GitHub-native repository features, and some run as repository-owned workflows.

Repository-owned workflows:

- `Bugarena - Build and Test`: builds the stack and runs the authoritative containerized validation flow inside `agent`, bootstrapping `.NET` through `mise` and including a high/critical NuGet audit during restore
- `Bugarena - Security Scan`: runs Trivy image scans for `agent`, `capability-broker`, and the repo-built `docker-daemon` sidecar image and publishes an SPDX SBOM artifact
- `Bugarena - Publish Images`: publishes release-only `agent` and `capability-broker` multi-arch images to GHCR from stable GitHub Releases

Repository settings to enable in GitHub:

- the Renovate GitHub App (or an equivalent Renovate runner) for dependency PRs from `renovate.json`
- Dependabot alerts so GitHub advisory data can surface vulnerable dependencies
- CodeQL default setup for code scanning
- Secret scanning and push protection
- branch protection or rulesets that require `Bugarena - Build and Test` and `Bugarena - Security Scan` to pass before merge

The checked-in Renovate configuration in `renovate.json` follows the same shape as the `aegis-gateway` baseline: Renovate's `config:best-practices` preset, a weekly Monday morning schedule in `Europe/Berlin`, an enabled dependency dashboard, and grouped non-major updates for:

- GitHub Actions
- NuGet packages
- Docker images and compose-managed container references

GitHub Actions are pinned to immutable commit SHAs in workflow files so Renovate can update them without relying on mutable tags.

Major dependency updates are disabled by default in Renovate for this repo so larger upgrade work stays intentional and reviewable.

Normal non-major dependency updates also wait for a 14-day minimum release age before Renovate opens a PR, which reduces the chance of immediately adopting a bad upstream release while still allowing security alert PRs to bypass that wait.

## Capability broker

`CapabilityBroker` is an allowlist-based outbound proxy for secret-backed external APIs. It is built with ASP.NET Core and YARP and is intended for agent-mode traffic only. The primary use case is domain and data APIs such as market data, ticketing, CRM, or geocoding services. LLM APIs can also fit this pattern, but they are not the main design target. Provider secrets stay in the proxy service; callers send normal requests to:

```text
/providers/{provider}/{allowed-upstream-path}
```

The service validates:
- configured provider name
- allowed HTTP method
- allowed path prefix
- required secret availability

This keeps the trust boundary narrow: the agent can make domain-specific API calls, but it does not get raw provider keys and it cannot turn the broker into a general-purpose internet proxy.

Broker scope:
- yes: outbound HTTP for allowlisted secret-backed external APIs
- yes: server-side auth injection using the configured secret bundle
- no: arbitrary URL forwarding
- no: database, filesystem, git, or CLI brokering
- no: storing provider secrets in source, appsettings, or the agent container image

Health endpoints:
- `/health/live`
- `/health/ready`

## Provider configuration

Non-secret provider metadata lives in `config/capability-broker/providers.json`. The checked-in default is empty so the compose stack stays safe to start. Add providers by editing that file.

Example data API configuration:

```json
{
  "CapabilityBroker": {
    "RequestTimeoutSeconds": 100,
    "Providers": {
      "market-data": {
        "BaseUrl": "https://api.example-marketdata.com",
        "AllowedMethods": ["GET"],
        "AllowedPathPrefixes": ["/v1/quotes", "/v1/news"],
        "Auth": {
          "Type": "BearerToken",
          "SecretKey": "market-data-sandbox"
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

Example auth fragments for non-LLM APIs:

```json
{
  "Auth": {
    "Type": "ApiKeyHeader",
    "SecretKey": "geo-data-sandbox",
    "HeaderName": "x-api-key"
  }
}
```

```json
{
  "Auth": {
    "Type": "QueryApiKey",
    "SecretKey": "exchange-rates-sandbox",
    "QueryParameterName": "api_key"
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
    "market-data-sandbox": "replace-with-sandbox-key",
    "geo-data-sandbox": "replace-with-sandbox-key",
    "exchange-rates-sandbox": "replace-with-sandbox-key"
  }
}
EOF

CAPABILITY_BROKER_SECRETS_FILE="$PWD/.secrets/capability-broker/provider-secrets.json" \
  docker compose -f infra/docker/compose.agent.yml up -d capability-broker
```

The bundle key must match the provider `Auth.SecretKey` value.

## Persistence

- Repositories and other working files persist in `/workspace` via the `agent_workspace` named volume.
- All agent home state persists in `/home/agent` via the `agent_home` named volume.
- That home volume includes Codex config, auth state, shell config, caches, user-installed tools, git config, SSH material, and `mise` runtime/cache data.
- Global git config is written in `/home/agent/.gitconfig`, so the entrypoint only seeds its defaults when a new home volume is missing that config.

Stop the environment without removing volumes:

```bash
docker compose -f infra/docker/compose.agent.yml down
```

If you started the `testinfra` profile, shut it down with:

```bash
docker compose -f infra/docker/compose.agent.yml --profile testinfra down -v
```
