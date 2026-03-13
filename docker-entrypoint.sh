#!/usr/bin/env bash
set -Eeuo pipefail

echo "Initializing Codex agent container..."
echo "READ /home/agent/.config/AGENTS.md BEFORE ANYTHING."

mkdir -p "$HOME/.config" "$HOME/.codex" "$HOME/.cache" /workspace

git config --global user.name "coding.agent.luffy"
git config --global user.email "YOUR_GITHUB_ID+YOUR_GITHUB_USERNAME@users.noreply.github.com"
git config --global init.defaultBranch main
git config --global pull.rebase false

if [[ ! -f "$HOME/.bashrc" ]]; then
    touch "$HOME/.bashrc"
fi

if ! grep -Fq "# codex-agent-aliases" "$HOME/.bashrc"; then
    cat <<'EOF' >> "$HOME/.bashrc"

# codex-agent-aliases
alias ll='ls -alF'
EOF
fi

if [[ ! -s "$HOME/.config/AGENTS.md" ]]; then
    install -m 0644 /opt/codex-agent/AGENTS.md "$HOME/.config/AGENTS.md"
fi 

if [[ ! -s "$HOME/.codex/config.toml" ]]; then
    install -m 0644 /opt/codex-agent/config.toml "$HOME/.codex/config.toml"
fi

for tool in node npm dotnet gh; do
    version="$($tool --version 2>/dev/null | head -n 1 || true)"
    echo "$tool: ${version:-not installed}"
done

exec "$@"
