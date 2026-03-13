using CapabilityProvider.Proxy;

namespace CapabilityProvider.Endpoints;

public static class CapabilityProviderEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCapabilityProviderEndpoints(this IEndpointRouteBuilder endpoints)
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
