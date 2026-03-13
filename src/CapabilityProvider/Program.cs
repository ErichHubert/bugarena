using CapabilityProvider.Endpoints;
using CapabilityProvider.Options;
using CapabilityProvider.Proxy;
using CapabilityProvider.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var externalProviderConfigPath = builder.Configuration[$"{CapabilityBrokerOptions.SectionName}:ProviderConfigPath"];
if (!string.IsNullOrWhiteSpace(externalProviderConfigPath))
{
    builder.Configuration.AddJsonFile(externalProviderConfigPath, optional: false, reloadOnChange: true);
}

builder.Services
    .AddOptions<CapabilityBrokerOptions>()
    .Bind(builder.Configuration.GetSection(CapabilityBrokerOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<CapabilityBrokerOptions>, CapabilityBrokerOptionsValidator>();
builder.Services.AddSingleton<IProviderRegistry, ProviderRegistry>();
builder.Services.AddSingleton<ISecretBundleResolver, SecretBundleResolver>();
builder.Services.AddSingleton<CapabilityBrokerStartupValidator>();
builder.Services.AddSingleton<ProxyHttpClientFactory>();
builder.Services.AddSingleton<ProviderProxyService>();

builder.Services
    .AddHealthChecks()
    .AddCheck<CapabilityBrokerReadinessHealthCheck>("capability-broker-readiness");

builder.Services.AddReverseProxy();

var app = builder.Build();

app.Services.GetRequiredService<CapabilityBrokerStartupValidator>().ValidateOrThrow();

app.MapGet("/", () => Results.Ok(new
{
    service = "capability-provider",
    status = "ok"
}));
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));
app.MapHealthChecks("/health/ready", new HealthCheckOptions());
app.MapCapabilityProviderEndpoints();

app.Run();

public partial class Program;
