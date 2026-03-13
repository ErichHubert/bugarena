namespace CapabilityProvider.Models;

public sealed class ProviderSecretBundle
{
    public Dictionary<string, string> Secrets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
