using CapabilityBroker.Models;
using CapabilityBroker.Options;
using Microsoft.Extensions.Options;

namespace CapabilityBroker.Services;

public sealed class ProviderRegistry : IProviderRegistry
{
    private readonly IReadOnlyCollection<ProviderDefinition> _allProviders;
    private readonly Dictionary<string, ProviderDefinition> _providers;

    public ProviderRegistry(IOptions<CapabilityBrokerOptions> options)
    {
        _providers = new Dictionary<string, ProviderDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var (providerName, providerOptions) in options.Value.Providers)
        {
            var definition = new ProviderDefinition(
                providerName,
                new Uri(providerOptions.BaseUrl, UriKind.Absolute),
                providerOptions.AllowedMethods,
                providerOptions.AllowedPathPrefixes,
                providerOptions.Auth.Type,
                providerOptions.Auth.SecretKey,
                providerOptions.Auth.HeaderName,
                providerOptions.Auth.QueryParameterName,
                providerOptions.Auth.Scheme,
                new Dictionary<string, string>(providerOptions.StaticRequestHeaders, StringComparer.OrdinalIgnoreCase));

            _providers[providerName] = definition;
        }

        _allProviders = _providers.Values.ToArray();
    }

    public IReadOnlyCollection<ProviderDefinition> GetAll() => _allProviders;

    public bool TryGet(string providerId, out ProviderDefinition? provider) =>
        _providers.TryGetValue(providerId, out provider);
}
