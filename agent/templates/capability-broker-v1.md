# Template: CapabilityBroker v1

## Scope
CapabilityBroker v1 handles **only external API-key-based provider calls**.
It is **not** a universal proxy.

## Responsibilities
- expose narrow provider endpoints or mapped passthrough routes
- validate provider + path + method against allowlist
- load sandbox/test provider secret internally
- inject auth header server-side
- forward request to allowed upstream
- redact sensitive logs

## Non-goals
- no database brokering
- no filesystem brokering
- no CLI execution brokering
- no arbitrary URL forwarding
- no prod secrets

## Suggested layout
- `Options/BrokerProviderOptions.cs`
- `Options/BrokerRoutePolicy.cs`
- `Services/ISecretResolver.cs`
- `Services/SecretResolver.cs`
- `Services/IProviderPolicy.cs`
- `Services/ProviderPolicy.cs`
- `Endpoints/ProviderEndpoints.cs`

## Minimal config model
```json
{
  "CapabilityBroker": {
    "Providers": {
      "openai": {
        "BaseUrl": "https://api.openai.com",
        "AllowedPaths": ["/v1/responses", "/v1/embeddings"],
        "AllowedMethods": ["POST"],
        "SecretName": "OPENAI_SANDBOX_KEY"
      }
    }
  }
}
```

## Decision logic
1. identify provider from route
2. verify provider exists
3. verify request method is allowed
4. verify path is allowed
5. resolve sandbox secret internally
6. attach upstream auth
7. forward request

## Security rules
- default deny
- sandbox/test secrets only
- do not expose secret values in logs/errors
- no host-port if only internal use is needed
- keep broker on dedicated network with agent

## Preferred route shape
Examples:
- `POST /providers/openai/responses`
- `POST /providers/openai/embeddings`

Avoid:
- `POST /proxy?url=https://...`
- any route that lets caller choose arbitrary target host

## Validation checklist
- allowed provider/path works
- disallowed path is rejected
- missing secret fails safely
- logs do not include auth headers or secret values
