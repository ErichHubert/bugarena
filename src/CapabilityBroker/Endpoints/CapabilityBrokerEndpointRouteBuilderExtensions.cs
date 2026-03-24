using CapabilityBroker.Proxy;

namespace CapabilityBroker.Endpoints;

public static class CapabilityBrokerEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCapabilityBrokerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/providers");

        group.Map("/{provider}", ProxyAsync);
        group.Map("/{provider}/{**catchall}", ProxyAsync);

        return endpoints;
    }

    private static Task ProxyAsync(
        string provider,
        HttpContext httpContext,
        ProviderProxyService proxyService,
        CancellationToken cancellationToken) =>
        proxyService.ProxyAsync(httpContext, provider, cancellationToken);
}
