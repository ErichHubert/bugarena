#!/usr/bin/env bash

if [[ -f /etc/profile.d/bugarena-testinfra.sh ]]; then
    # Export Docker/Testcontainers variables when the sidecar is present.
    source /etc/profile.d/bugarena-testinfra.sh
fi

case ":$PATH:" in
    *":$HOME/.local/bin:"*) ;;
    *) export PATH="$HOME/.local/bin:$PATH" ;;
esac

if command -v mise >/dev/null 2>&1; then
    eval "$(mise activate bash --shims)"
fi
