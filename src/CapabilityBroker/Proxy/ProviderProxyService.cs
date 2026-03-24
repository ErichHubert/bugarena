using CapabilityBroker.Models;
using CapabilityBroker.Options;
using CapabilityBroker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Forwarder;

namespace CapabilityBroker.Proxy;

public sealed class ProviderProxyService
{
    private readonly ForwarderRequestConfig _forwarderRequestConfig;
    private readonly IHttpForwarder _httpForwarder;
    private readonly ILogger<ProviderProxyService> _logger;
    private readonly IProviderRegistry _providerRegistry;
    private readonly ProxyHttpClientFactory _proxyHttpClientFactory;
    private readonly ISecretBundleResolver _secretBundleResolver;

    public ProviderProxyService(
        IHttpForwarder httpForwarder,
        IOptions<CapabilityBrokerOptions> options,
        ILogger<ProviderProxyService> logger,
        IProviderRegistry providerRegistry,
        ProxyHttpClientFactory proxyHttpClientFactory,
        ISecretBundleResolver secretBundleResolver)
    {
        _httpForwarder = httpForwarder;
        _logger = logger;
        _providerRegistry = providerRegistry;
        _proxyHttpClientFactory = proxyHttpClientFactory;
        _secretBundleResolver = secretBundleResolver;
        _forwarderRequestConfig = new ForwarderRequestConfig
        {
            ActivityTimeout = TimeSpan.FromSeconds(options.Value.RequestTimeoutSeconds)
        };
    }

    public async Task ProxyAsync(HttpContext httpContext, string providerId, CancellationToken cancellationToken)
    {
        if (!_providerRegistry.TryGet(providerId, out var provider) || provider is null)
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status404NotFound,
                "Provider not found",
                $"Provider '{providerId}' is not configured.");
            return;
        }

        if (!provider.IsMethodAllowed(httpContext.Request.Method))
        {
            httpContext.Response.Headers.Allow = string.Join(", ", provider.AllowedMethods);
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status405MethodNotAllowed,
                "Method not allowed",
                $"Method '{httpContext.Request.Method}' is not allowed for provider '{provider.ProviderId}'.");
            return;
        }

        var upstreamPath = GetUpstreamPath(httpContext, providerId);
        if (!provider.IsPathAllowed(upstreamPath))
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Path not allowed",
                $"Path '{upstreamPath}' is not allowed for provider '{provider.ProviderId}'.");
            return;
        }

        var secret = provider.RequiresSecret
            ? _secretBundleResolver.GetSecret(provider.SecretKey!)
            : null;

        if (provider.RequiresSecret && string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogWarning("Provider secret unavailable for {ProviderId}.", provider.ProviderId);

            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status503ServiceUnavailable,
                "Provider secret unavailable",
                $"Provider '{provider.ProviderId}' is missing required secret material.");
            return;
        }

        httpContext.Items[CapabilityProxyTransformer.UpstreamPathItemKey] = upstreamPath;

        var error = await _httpForwarder.SendAsync(
            httpContext,
            provider.DestinationPrefix,
            _proxyHttpClientFactory.Invoker,
            _forwarderRequestConfig,
            new CapabilityProxyTransformer(provider, secret));

        if (error == ForwarderError.None)
        {
            return;
        }

        var errorFeature = httpContext.Features.Get<IForwarderErrorFeature>();
        _logger.LogWarning(
            errorFeature?.Exception,
            "Forwarding to provider {ProviderId} failed with {ForwarderError}.",
            provider.ProviderId,
            error);

        if (!httpContext.Response.HasStarted)
        {
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status502BadGateway,
                "Upstream request failed",
                $"Proxying to provider '{provider.ProviderId}' failed.");
        }
    }

    private static PathString GetUpstreamPath(HttpContext httpContext, string providerId)
    {
        var routePrefix = new PathString($"/providers/{providerId}");
        _ = httpContext.Request.Path.StartsWithSegments(
            routePrefix,
            StringComparison.OrdinalIgnoreCase,
            out _,
            out var remainder);

        return remainder.HasValue ? remainder : new PathString("/");
    }

    private static Task WriteProblemAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail)
    {
        var result = Results.Problem(
            detail: detail,
            statusCode: statusCode,
            title: title,
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = httpContext.TraceIdentifier
            });

        return result.ExecuteAsync(httpContext);
    }
}
