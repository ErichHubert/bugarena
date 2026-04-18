---
name: dotnet-provider-integration
description: Implement broker-aware .NET external provider clients with one domain-facing client and environment-specific composition. Use when adding or updating provider clients, handlers, DI wiring, base URLs, or provider options for direct local access versus agent-mode broker access.
---

# Dotnet Provider Integration

- Read `references/dotnet-provider-pattern-b.md` before changing provider wiring.
- Keep one domain-facing client type across environments; vary composition, not client semantics.
- Keep provider auth in an HTTP handler for local development or in the broker for agent mode, never in business logic.
- Keep non-secret configuration in `appsettings*.json`; load secrets from user secrets, environment variables, or broker-side resolution.
- Update options, dependency injection, and provider client tests together.
