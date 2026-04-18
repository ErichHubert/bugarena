---
name: agent-container-stack
description: Design or refine Codex-oriented agent workstation/container repositories with Dockerfiles, entrypoints, persistent Codex home baselines, isolated volumes, and repo-managed runtimes. Use when changing a Codex container stack or designing similar agent workstations.
---

# Agent Container Stack

- Read `references/agent-container-stack.md` before changing the stack shape.
- Preserve the isolated workstation model: named volumes for workspace and home, no host repo mount, no host home mount, and no host Docker socket.
- Keep `agent/codex-home-agents.md` and `agent/codex-home-config.toml` as the canonical home baselines seeded by the entrypoint.
- Keep repo-specific instructions in repo-root `AGENTS.md` files and repeatable repo workflows in repo-local `.agents/skills`, not in the global home baseline.
- Prefer `mise` for repo-specific runtimes instead of baking extra language stacks into the image unless there is a clear reason.
