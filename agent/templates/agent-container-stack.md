# Template: Agent container stack (phase 1)

## Goal
A Codex contributor container that behaves like an isolated developer workstation.

## Required tools
- git
- gh
- node + npm
- @openai/codex
- .NET SDK
- jq, rg, fd, curl, unzip, zip, python3

## Required properties
- non-root runtime user
- workspace stored in named volume
- no host source mount
- no host Docker socket
- no privileged mode
- no provider secrets in image
- persistent Codex config volume for `codex login`

## Compose principles
- no published ports
- dedicated internal network
- named volumes for `/workspace`, Codex config, caches
- GitHub bot token only if needed

## Runtime flow
1. start container
2. entrypoint performs harmless idempotent init
3. login to Codex manually (`codex login`)
4. authenticate GitHub if needed
5. clone repo inside `/workspace`
6. branch, implement, test, PR

## Entrypoint should do
- git defaults
- create dirs
- friendly environment info
- exec main command

## Entrypoint should not do
- interactive login
- clone repo automatically
- start codex automatically
- install provider secrets
