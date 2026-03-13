using CapabilityBroker.Models;

namespace CapabilityBroker.Services;

public interface IProviderRegistry
{
    IReadOnlyCollection<ProviderDefinition> GetAll();

    bool TryGet(string providerId, out ProviderDefinition? provider);
}
