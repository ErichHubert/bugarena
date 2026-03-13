using CapabilityBroker.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy.Forwarder;

namespace CapabilityBroker.Proxy;

public sealed class CapabilityProxyTransformer : HttpTransformer
{
    public const string UpstreamPathItemKey = "CapabilityBroker.UpstreamPath";

    private readonly ProviderDefinition _provider;
    private readonly string? _secret;

    public CapabilityProxyTransformer(ProviderDefinition provider, string? secret)
    {
        _provider = provider;
        _secret = secret;
    }

    public override async ValueTask TransformRequestAsync(
        HttpContext httpContext,
        HttpRequestMessage proxyRequest,
        string destinationPrefix,
        CancellationToken cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

        proxyRequest.Headers.Host = null;
        proxyRequest.RequestUri = BuildDestinationUri(httpContext, destinationPrefix);

        foreach (var header in _provider.StaticRequestHeaders)
        {
            proxyRequest.Headers.Remove(header.Key);
            proxyRequest.Content?.Headers.Remove(header.Key);

            if (!proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                proxyRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        switch (_provider.AuthType)
        {
            case Options.ProviderAuthType.BearerToken:
                proxyRequest.Headers.Remove(HeaderNames.Authorization);
                proxyRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_provider.Scheme, _secret);
                break;
            case Options.ProviderAuthType.ApiKeyHeader:
                if (!string.IsNullOrWhiteSpace(_provider.HeaderName))
                {
                    proxyRequest.Headers.Remove(_provider.HeaderName);
                    proxyRequest.Content?.Headers.Remove(_provider.HeaderName);
                    proxyRequest.Headers.TryAddWithoutValidation(_provider.HeaderName, _secret);
                }

                break;
        }
    }

    private Uri BuildDestinationUri(HttpContext httpContext, string destinationPrefix)
    {
        var upstreamPath = httpContext.Items.TryGetValue(UpstreamPathItemKey, out var value) && value is PathString path
            ? path
            : new PathString("/");
        var destination = new UriBuilder(destinationPrefix)
        {
            Path = CombinePath(new Uri(destinationPrefix).AbsolutePath, upstreamPath.Value),
            Query = BuildQueryString(httpContext)
        };

        return destination.Uri;
    }

    private string BuildQueryString(HttpContext httpContext)
    {
        if (_provider.AuthType != Options.ProviderAuthType.QueryApiKey ||
            string.IsNullOrWhiteSpace(_provider.QueryParameterName))
        {
            return httpContext.Request.QueryString.HasValue
                ? httpContext.Request.QueryString.Value!.TrimStart('?')
                : string.Empty;
        }

        var builder = new QueryBuilder();

        foreach (var queryValue in httpContext.Request.Query)
        {
            if (string.Equals(queryValue.Key, _provider.QueryParameterName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var value in queryValue.Value)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    builder.Add(queryValue.Key, value);
                }
            }
        }

        builder.Add(_provider.QueryParameterName, _secret ?? string.Empty);
        return builder.ToQueryString().Value?.TrimStart('?') ?? string.Empty;
    }

    private static string CombinePath(string basePath, string? requestPath)
    {
        var normalizedBase = string.IsNullOrEmpty(basePath) || basePath == "/"
            ? string.Empty
            : basePath.TrimEnd('/');
        var normalizedRequest = string.IsNullOrEmpty(requestPath) ? string.Empty : requestPath;

        if (string.IsNullOrEmpty(normalizedRequest))
        {
            return string.IsNullOrEmpty(normalizedBase) ? "/" : normalizedBase;
        }

        return $"{normalizedBase}{normalizedRequest}";
    }
}
