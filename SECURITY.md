# Security Policy

## Supported versions

Security fixes are applied on a best-effort basis to the latest state of `main`.

## Reporting a vulnerability

- Do not open a public GitHub issue with vulnerability details.
- Prefer GitHub private vulnerability reporting when it is enabled for this repository.
- If private reporting is not available, contact the maintainer through GitHub without posting exploit details publicly.

Please include:

- the affected component
- reproduction steps
- impact
- any proposed mitigation

Useful report categories for this repository include:

- provider secret exposure
- auth header or query injection mistakes
- broker allowlist bypasses
- unintended open-proxy behavior
- container isolation escapes
- workflow or build leakage of sensitive data

## Disclosure handling

- Reports are handled on a best-effort basis.
- Coordinate disclosure before publishing technical details or proof-of-concept code.
- Rotate any exposed credentials immediately; do not wait for a code fix first.
