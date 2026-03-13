#!/usr/bin/env bash
set -Eeuo pipefail

echo "Initializing Codex agent container..."

mkdir -p "$HOME/.config" "$HOME/.codex" "$HOME/.cache" /workspace

git config --global user.name "Codex Agent"
git config --global user.email "codex-agent@example.com"
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

install -m 0644 /opt/codex-agent/AGENTS.md /workspace/AGENTS.md

for tool in node npm dotnet gh; do
    version="$($tool --version 2>/dev/null | head -n 1 || true)"
    echo "$tool: ${version:-not installed}"
done

exec "$@"
