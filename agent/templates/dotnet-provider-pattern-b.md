# Template: .NET external provider integration (Pattern B)

## Goal
Implement one domain-facing provider client with environment-specific composition:
- local dev: direct provider + auth handler
- agent env: CapabilityBroker/proxy + no auth handler

## Preferred structure
- `Options/ProviderOptions.cs`
- `Clients/IExternalProviderClient.cs`
- `Clients/ExternalProviderClient.cs`
- `Infrastructure/Http/ApiKeyHandler.cs`
- `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`

## ProviderOptions
Example fields:
- `BaseUrl`
- `ApiKey` (dev only; never from repo config)
- `TimeoutSeconds`
- optional `Mode` or infer via environment-specific registration

## Interface
```csharp
public interface IExternalProviderClient
{
    Task<string> PingAsync(CancellationToken cancellationToken = default);
}
```

## Client principles
- client code should not know whether upstream is real provider or broker
- client should focus on request/response semantics
- auth belongs in handler or broker

Example:
```csharp
public sealed class ExternalProviderClient : IExternalProviderClient
{
    private readonly HttpClient _httpClient;

    public ExternalProviderClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> PingAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("/v1/ping", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
```

## Auth handler (dev only)
```csharp
public sealed class ApiKeyHandler : DelegatingHandler
{
    private readonly ProviderOptions _options;

    public ApiKeyHandler(IOptions<ProviderOptions> options)
    {
        _options = options.Value;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Provider API key is missing.");

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
        return base.SendAsync(request, cancellationToken);
    }
}
```

## DI registration (Pattern B)
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExternalProvider(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<ProviderOptions>(configuration.GetSection("Provider"));

        var options = configuration.GetSection("Provider").Get<ProviderOptions>()
            ?? throw new InvalidOperationException("Missing provider configuration.");

        if (environment.IsEnvironment("Agent"))
        {
            services.AddHttpClient<IExternalProviderClient, ExternalProviderClient>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            });
        }
        else
        {
            services.AddTransient<ApiKeyHandler>();

            services.AddHttpClient<IExternalProviderClient, ExternalProviderClient>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddHttpMessageHandler<ApiKeyHandler>();
        }

        return services;
    }
}
```

## Configuration rules
- `appsettings*.json`: non-secret values only
- local dev secret: user secrets / local secret store / env var
- agent env: BaseUrl should point to CapabilityBroker
- never commit provider secrets

## Validation checklist
- local dev path works with direct provider auth
- agent env path works against broker without provider secret in app
- provider client tests cover expected status/error handling
- configuration is environment-specific without duplicating business client logic
