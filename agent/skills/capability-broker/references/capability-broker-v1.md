# Template: CapabilityBroker v1

## Scope
Allowlisted outbound HTTP for secret-backed provider APIs only. Not a universal proxy. The default examples should be domain or data APIs, not LLM vendors.

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
      "market-data": {
        "BaseUrl": "https://api.example-marketdata.com",
        "AllowedPaths": ["/v1/quotes", "/v1/news"],
        "AllowedMethods": ["GET"],
        "SecretName": "MARKET_DATA_SANDBOX_KEY"
      }
    }
  }
}
```

## Rules
- default deny
- route shape should stay narrow, for example `/providers/market-data/v1/quotes`
- do not expose secret values in logs or errors

## Validation
- allowed provider/path works
- disallowed path is rejected
- missing secret fails safely
- logs do not include auth headers or secret values
