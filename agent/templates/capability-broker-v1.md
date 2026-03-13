# Template: CapabilityBroker v1

## Scope
Allowlisted outbound HTTP for API-key providers only. Not a universal proxy.

## Responsibilities
- validate provider, method, and path
- load sandbox/test secret internally
- inject auth server-side
- forward to allowed upstream
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

## Minimal config
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

## Rules
- default deny
- route shape should stay narrow, for example `/providers/openai/responses`
- do not expose secret values in logs or errors

## Validation
- allowed provider/path works
- disallowed path is rejected
- missing secret fails safely
- logs do not include auth headers or secret values
