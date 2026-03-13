namespace CapabilityBroker.Services;

public sealed class CapabilityBrokerStartupValidator
{
    private readonly IProviderRegistry _providerRegistry;
    private readonly ISecretBundleResolver _secretBundleResolver;

    public CapabilityBrokerStartupValidator(
        IProviderRegistry providerRegistry,
        ISecretBundleResolver secretBundleResolver)
    {
        _providerRegistry = providerRegistry;
        _secretBundleResolver = secretBundleResolver;
    }

    public void ValidateOrThrow()
    {
        foreach (var provider in _providerRegistry.GetAll())
        {
            if (!provider.RequiresSecret)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(_secretBundleResolver.GetSecret(provider.SecretKey!)))
            {
                throw new InvalidOperationException(
                    $"Provider '{provider.ProviderId}' is configured to require a secret, but the secret '{provider.SecretKey}' is unavailable.");
            }
        }
    }
}
