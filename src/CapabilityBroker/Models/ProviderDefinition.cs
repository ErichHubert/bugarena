using CapabilityBroker.Options;
using Microsoft.AspNetCore.Http;

namespace CapabilityBroker.Models;

public sealed class ProviderDefinition
{
    public ProviderDefinition(
        string providerId,
        Uri baseUri,
        IEnumerable<string> allowedMethods,
        IEnumerable<string> allowedPathPrefixes,
        ProviderAuthType authType,
        string? secretKey,
        string? headerName,
        string? queryParameterName,
        string? scheme,
        IReadOnlyDictionary<string, string> staticRequestHeaders)
    {
        ProviderId = providerId;
        BaseUri = baseUri;
        DestinationPrefix = baseUri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        AllowedMethods = new HashSet<string>(
            allowedMethods.Select(static method => method.ToUpperInvariant()),
            StringComparer.OrdinalIgnoreCase);
        AllowedPathPrefixes = allowedPathPrefixes
            .Select(NormalizePathPrefix)
            .ToArray();
        AuthType = authType;
        SecretKey = secretKey;
        HeaderName = headerName;
        QueryParameterName = queryParameterName;
        Scheme = string.IsNullOrWhiteSpace(scheme) ? "Bearer" : scheme;
        StaticRequestHeaders = staticRequestHeaders;
    }

    public string ProviderId { get; }

    public Uri BaseUri { get; }

    public string DestinationPrefix { get; }

    public IReadOnlyCollection<string> AllowedMethods { get; }

    public IReadOnlyCollection<PathString> AllowedPathPrefixes { get; }

    public ProviderAuthType AuthType { get; }

    public string? SecretKey { get; }

    public string? HeaderName { get; }

    public string? QueryParameterName { get; }

    public string Scheme { get; }

    public IReadOnlyDictionary<string, string> StaticRequestHeaders { get; }

    public bool RequiresSecret => AuthType != ProviderAuthType.None;

    public bool IsMethodAllowed(string method) => AllowedMethods.Contains(method);

    public bool IsPathAllowed(PathString path) =>
        AllowedPathPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.Ordinal));

    private static PathString NormalizePathPrefix(string value)
    {
        var normalized = value.StartsWith('/') ? value : $"/{value}";
        normalized = normalized.Length > 1 ? normalized.TrimEnd('/') : normalized;

        return new PathString(normalized);
    }
}
