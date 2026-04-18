# Template: .NET external provider integration (Pattern B)

## Goal
Implement one domain-facing provider client with environment-specific composition:
- local dev: direct provider + auth handler
- agent env: CapabilityBroker/proxy + no auth handler

## Structure
- `Options/ProviderOptions.cs`
- `Clients/IExternalProviderClient.cs`
- `Clients/ExternalProviderClient.cs`
- `Infrastructure/Http/ApiKeyHandler.cs`
- `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`

## Rules
- client semantics stay the same in every environment
- auth belongs in a handler or the broker, not in business code
- keep infrastructure wiring in the composition root
- `appsettings*.json` stores non-secret values only
- local secrets come from user secrets or env vars
- agent `BaseUrl` points to the broker

## Minimal interface
```csharp
public interface IExternalProviderClient
{
    Task<string> PingAsync(CancellationToken cancellationToken = default);
}
```

## DI rule
Register the same client type in both environments. Only the base URL and auth pipeline should change.

## Validation
- local dev path works with direct provider auth
- agent env path works against broker without provider secret in app
- provider client tests cover expected status/error handling
- configuration is environment-specific without duplicating business client logic
