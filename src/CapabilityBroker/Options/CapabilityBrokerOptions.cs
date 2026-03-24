namespace CapabilityBroker.Options;

public sealed class CapabilityBrokerOptions
{
    public const string SectionName = "CapabilityBroker";

    public string? ProviderConfigPath { get; set; }

    public string? SecretBundlePath { get; set; }

    public int RequestTimeoutSeconds { get; set; } = 100;

    public Dictionary<string, BrokerProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
