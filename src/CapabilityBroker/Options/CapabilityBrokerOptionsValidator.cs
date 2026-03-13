using Microsoft.Extensions.Options;

namespace CapabilityBroker.Options;

public sealed class CapabilityBrokerOptionsValidator : IValidateOptions<CapabilityBrokerOptions>
{
    public ValidateOptionsResult Validate(string? name, CapabilityBrokerOptions options)
    {
        var errors = new List<string>();

        if (options.RequestTimeoutSeconds <= 0)
        {
            errors.Add("CapabilityBroker:RequestTimeoutSeconds must be greater than 0.");
        }

        foreach (var (providerName, provider) in options.Providers)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                errors.Add("CapabilityBroker provider names cannot be empty.");
                continue;
            }

            if (!Uri.TryCreate(provider.BaseUrl, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add($"CapabilityBroker:Providers:{providerName}:BaseUrl must be an absolute http/https URL.");
            }

            if (provider.AllowedMethods.Count == 0)
            {
                errors.Add($"CapabilityBroker:Providers:{providerName}:AllowedMethods must include at least one method.");
            }

            if (provider.AllowedPathPrefixes.Count == 0)
            {
                errors.Add($"CapabilityBroker:Providers:{providerName}:AllowedPathPrefixes must include at least one path prefix.");
            }

            foreach (var pathPrefix in provider.AllowedPathPrefixes)
            {
                if (string.IsNullOrWhiteSpace(pathPrefix) || !pathPrefix.StartsWith('/'))
                {
                    errors.Add($"CapabilityBroker:Providers:{providerName}:AllowedPathPrefixes entries must start with '/'.");
                }
            }

            foreach (var header in provider.StaticRequestHeaders)
            {
                if (string.IsNullOrWhiteSpace(header.Key))
                {
                    errors.Add($"CapabilityBroker:Providers:{providerName}:StaticRequestHeaders cannot contain an empty header name.");
                }

                if (string.IsNullOrWhiteSpace(header.Value))
                {
                    errors.Add($"CapabilityBroker:Providers:{providerName}:StaticRequestHeaders:{header.Key} cannot be empty.");
                }
            }

            ValidateAuth(providerName, provider.Auth, errors);
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateAuth(string providerName, ProviderAuthOptions auth, ICollection<string> errors)
    {
        switch (auth.Type)
        {
            case ProviderAuthType.None:
                return;
            case ProviderAuthType.BearerToken:
                if (string.IsNullOrWhiteSpace(auth.SecretKey))
                {
                    errors.Add($"CapabilityBroker:Providers:{providerName}:Auth:SecretKey is required for BearerToken auth.");
                }

                return;
            case ProviderAuthType.ApiKeyHeader:
                if (string.IsNullOrWhiteSpace(auth.SecretKey))
                {
                    errors.Add($"CapabilityBroker:Providers:{providerName}:Auth:SecretKey is required for ApiKeyHeader auth.");
                }

                if (string.IsNullOrWhiteSpace(auth.HeaderName))
                {
                    errors.Add($"CapabilityBroker:Providers:{providerName}:Auth:HeaderName is required for ApiKeyHeader auth.");
                }

                return;
            case ProviderAuthType.QueryApiKey:
                if (string.IsNullOrWhiteSpace(auth.SecretKey))
                {
                    errors.Add($"CapabilityBroker:Providers:{providerName}:Auth:SecretKey is required for QueryApiKey auth.");
                }

                if (string.IsNullOrWhiteSpace(auth.QueryParameterName))
                {
                    errors.Add($"CapabilityBroker:Providers:{providerName}:Auth:QueryParameterName is required for QueryApiKey auth.");
                }

                return;
            default:
                errors.Add($"CapabilityBroker:Providers:{providerName}:Auth:Type '{auth.Type}' is not supported.");
                return;
        }
    }
}
