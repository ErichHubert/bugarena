using System.Net;

namespace CapabilityProvider.Proxy;

public sealed class ProxyHttpClientFactory : IDisposable
{
    public ProxyHttpClientFactory()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            EnableMultipleHttp2Connections = true,
            UseCookies = false,
            UseProxy = false
        };

        Invoker = new HttpMessageInvoker(handler, disposeHandler: true);
    }

    public HttpMessageInvoker Invoker { get; }

    public void Dispose() => Invoker.Dispose();
}
