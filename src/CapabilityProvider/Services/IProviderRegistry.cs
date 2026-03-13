using CapabilityProvider.Models;

namespace CapabilityProvider.Services;

public interface IProviderRegistry
{
    IReadOnlyCollection<ProviderDefinition> GetAll();

    bool TryGet(string providerId, out ProviderDefinition? provider);
}
