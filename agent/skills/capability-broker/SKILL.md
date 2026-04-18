---
name: capability-broker
description: Design and implement allowlisted CapabilityBroker integrations for secret-backed outbound HTTP providers. Use when work touches broker provider config, auth injection, route validation, startup validation, or regression tests in repositories that follow the Bugarena CapabilityBroker pattern.
---

# Capability Broker

- Read `references/capability-broker-v1.md` before making broker design changes.
- Keep the broker narrow: secret-backed outbound HTTP only. Do not proxy databases, filesystem access, local CLI execution, or arbitrary URLs.
- Preserve default-deny behavior for provider, path, and method checks.
- Update runtime code, repo-tracked provider config, and broker regression tests together.
- Redact sensitive data from logs, errors, and test assertions.
