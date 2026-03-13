using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CapabilityProvider.Services;

public sealed class CapabilityBrokerReadinessHealthCheck : IHealthCheck
{
    private readonly IProviderRegistry _providerRegistry;
    private readonly ISecretBundleResolver _secretBundleResolver;

    public CapabilityBrokerReadinessHealthCheck(
        IProviderRegistry providerRegistry,
        ISecretBundleResolver secretBundleResolver)
    {
        _providerRegistry = providerRegistry;
        _secretBundleResolver = secretBundleResolver;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        foreach (var provider in _providerRegistry.GetAll())
        {
            if (!provider.RequiresSecret)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(_secretBundleResolver.GetSecret(provider.SecretKey!)))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Provider '{provider.ProviderId}' is missing secret '{provider.SecretKey}'."));
            }
        }

        return Task.FromResult(HealthCheckResult.Healthy("Capability provider is ready."));
    }
}
