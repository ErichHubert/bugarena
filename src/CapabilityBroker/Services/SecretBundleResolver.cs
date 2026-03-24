using System.Text.Json;
using CapabilityBroker.Models;
using CapabilityBroker.Options;
using Microsoft.Extensions.Options;

namespace CapabilityBroker.Services;

public sealed class SecretBundleResolver : ISecretBundleResolver
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly object _sync = new();
    private readonly IOptions<CapabilityBrokerOptions> _options;
    private IReadOnlyDictionary<string, string> _cachedSecrets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private DateTime _cachedLastWriteTimeUtc = DateTime.MinValue;
    private string? _cachedPath;

    public SecretBundleResolver(IOptions<CapabilityBrokerOptions> options)
    {
        _options = options;
    }

    public string? GetSecret(string secretKey)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return null;
        }

        var path = _options.Value.SecretBundlePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        var lastWriteTimeUtc = File.GetLastWriteTimeUtc(fullPath);

        lock (_sync)
        {
            if (!string.Equals(_cachedPath, fullPath, StringComparison.Ordinal) ||
                _cachedLastWriteTimeUtc != lastWriteTimeUtc)
            {
                _cachedSecrets = LoadSecrets(fullPath);
                _cachedPath = fullPath;
                _cachedLastWriteTimeUtc = lastWriteTimeUtc;
            }

            return _cachedSecrets.TryGetValue(secretKey, out var secret)
                ? secret
                : null;
        }
    }

    private static IReadOnlyDictionary<string, string> LoadSecrets(string fullPath)
    {
        using var stream = File.OpenRead(fullPath);
        var bundle = JsonSerializer.Deserialize<ProviderSecretBundle>(stream, SerializerOptions) ?? new ProviderSecretBundle();

        return new Dictionary<string, string>(bundle.Secrets, StringComparer.OrdinalIgnoreCase);
    }
}
