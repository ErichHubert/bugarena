#!/usr/bin/env bash
set -Eeuo pipefail

echo "Initializing Codex agent container..."
echo "READ /home/agent/.codex/AGENTS.md BEFORE ANYTHING."

mkdir -p "$HOME/.config" "$HOME/.codex" "$HOME/.cache" /workspace

set_git_config_default() {
    local key="$1"
    local value="$2"

    if ! git config --global --get "$key" >/dev/null 2>&1; then
        git config --global "$key" "$value"
    fi
}

set_git_config_default user.name "Codex Agent"
set_git_config_default user.email "codex-agent@example.com"
set_git_config_default init.defaultBranch main
set_git_config_default pull.rebase false

if [[ ! -f "$HOME/.bashrc" ]]; then
    touch "$HOME/.bashrc"
fi

if ! grep -Fq "# codex-agent-aliases" "$HOME/.bashrc"; then
    cat <<'EOF' >> "$HOME/.bashrc"

# codex-agent-aliases
alias ll='ls -alF'
EOF
fi

if [[ ! -s "$HOME/.codex/AGENTS.md" ]]; then
    if [[ -s "$HOME/.config/AGENTS.md" ]]; then
        mv "$HOME/.config/AGENTS.md" "$HOME/.codex/AGENTS.md"
    else
        install -m 0644 /opt/codex-agent/codex-home-agents.md "$HOME/.codex/AGENTS.md"
    fi
fi

if [[ ! -s "$HOME/.codex/config.toml" ]]; then
    install -m 0644 /opt/codex-agent/codex-home-config.toml "$HOME/.codex/config.toml"
fi

for tool in node npm dotnet gh; do
    version="$($tool --version 2>/dev/null | head -n 1 || true)"
    echo "$tool: ${version:-not installed}"
done

exec "$@"
