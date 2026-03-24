namespace CapabilityBroker.Options;

public sealed class ProviderAuthOptions
{
    public ProviderAuthType Type { get; set; } = ProviderAuthType.None;

    public string? SecretKey { get; set; }

    public string? HeaderName { get; set; }

    public string? QueryParameterName { get; set; }

    public string? Scheme { get; set; } = "Bearer";
}
