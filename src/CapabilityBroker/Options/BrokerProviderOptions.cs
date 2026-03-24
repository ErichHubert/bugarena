namespace CapabilityBroker.Options;

public sealed class BrokerProviderOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public List<string> AllowedMethods { get; set; } = [];

    public List<string> AllowedPathPrefixes { get; set; } = [];

    public Dictionary<string, string> StaticRequestHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public ProviderAuthOptions Auth { get; set; } = new();
}
