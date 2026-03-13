namespace CapabilityProvider.Services;

public interface ISecretBundleResolver
{
    string? GetSecret(string secretKey);
}
