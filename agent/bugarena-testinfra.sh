#!/usr/bin/env bash

# Export the Docker/Testcontainers environment only when the isolated DinD sidecar exists.
bugarena_testinfra_service_name="${BUGARENA_TESTINFRA_SERVICE_NAME:-docker-daemon}"

if [[ -z "${DOCKER_HOST:-}" ]] && [[ -n "${BUGARENA_TESTINFRA_DOCKER_HOST:-}" ]]; then
    if command -v getent >/dev/null 2>&1 && getent hosts "$bugarena_testinfra_service_name" >/dev/null 2>&1; then
        export DOCKER_HOST="$BUGARENA_TESTINFRA_DOCKER_HOST"
        export TESTCONTAINERS_HOST_OVERRIDE="${TESTCONTAINERS_HOST_OVERRIDE:-${BUGARENA_TESTINFRA_TESTCONTAINERS_HOST_OVERRIDE:-$bugarena_testinfra_service_name}}"
        export TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE="${TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE:-${BUGARENA_TESTINFRA_TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE:-/var/run/docker.sock}}"
    fi
fi

unset bugarena_testinfra_service_name
