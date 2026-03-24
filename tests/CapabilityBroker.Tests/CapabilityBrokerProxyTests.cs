using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace CapabilityBroker.Tests;

public sealed class CapabilityBrokerProxyTests
{
    [Fact]
    public async Task ForwardsAllowedRequestAndInjectsBearerTokenAsync()
    {
        await using var upstream = await TestUpstreamServer.StartAsync();
        using var secretFile = new TemporaryJsonFile(
            """
            {
              "Secrets": {
                "openai-sandbox": "broker-test-token"
              }
            }
            """);
        using var factory = CreateFactory(CreateProviderSettings(upstream.BaseUrl, secretFile.Path));
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/providers/openai/v1/responses?stream=false")
        {
            Content = new StringContent("""{"input":"hello"}""", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "client-side-token");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var capturedRequest = await upstream.WaitForRequestAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post.Method, capturedRequest!.Method);
        Assert.Equal("/v1/responses", capturedRequest.Path);
        Assert.Equal("?stream=false", capturedRequest.QueryString);
        Assert.Equal("Bearer broker-test-token", capturedRequest.Authorization);
        Assert.Contains("hello", capturedRequest.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectsDisallowedPathWithoutForwardingAsync()
    {
        await using var upstream = await TestUpstreamServer.StartAsync();
        using var secretFile = new TemporaryJsonFile(
            """
            {
              "Secrets": {
                "openai-sandbox": "broker-test-token"
              }
            }
            """);
        using var factory = CreateFactory(CreateProviderSettings(upstream.BaseUrl, secretFile.Path));
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            "/providers/openai/v1/files",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(await upstream.TryWaitForRequestAsync(TimeSpan.FromMilliseconds(300)));
    }

    [Fact]
    public async Task StartupFailsWhenConfiguredSecretIsMissingAsync()
    {
        await using var upstream = await TestUpstreamServer.StartAsync();
        using var secretFile = new TemporaryJsonFile(
            """
            {
              "Secrets": {}
            }
            """);
        using var factory = CreateFactory(CreateProviderSettings(upstream.BaseUrl, secretFile.Path));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var client = factory.CreateClient();
            _ = await client.GetAsync("/health/live");
        });

        Assert.Contains("openai-sandbox", exception.Message, StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IReadOnlyDictionary<string, string?> configuration) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(configuration);
                });
            });

    private static Dictionary<string, string?> CreateProviderSettings(string baseUrl, string secretBundlePath) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["CapabilityBroker:SecretBundlePath"] = secretBundlePath,
            ["CapabilityBroker:RequestTimeoutSeconds"] = "15",
            ["CapabilityBroker:Providers:openai:BaseUrl"] = baseUrl,
            ["CapabilityBroker:Providers:openai:AllowedMethods:0"] = "POST",
            ["CapabilityBroker:Providers:openai:AllowedPathPrefixes:0"] = "/v1/responses",
            ["CapabilityBroker:Providers:openai:Auth:Type"] = "BearerToken",
            ["CapabilityBroker:Providers:openai:Auth:SecretKey"] = "openai-sandbox"
        };

    private sealed class TemporaryJsonFile : IDisposable
    {
        public TemporaryJsonFile(string contents)
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"{Guid.NewGuid():N}.json");
            File.WriteAllText(Path, contents);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    private sealed class TestUpstreamServer : IAsyncDisposable
    {
        private readonly WebApplication _application;
        private readonly TaskCompletionSource<CapturedRequest> _capturedRequest;

        private TestUpstreamServer(
            WebApplication application,
            string baseUrl,
            TaskCompletionSource<CapturedRequest> capturedRequest)
        {
            _application = application;
            BaseUrl = baseUrl.TrimEnd('/');
            _capturedRequest = capturedRequest;
        }

        public string BaseUrl { get; }

        public static async Task<TestUpstreamServer> StartAsync()
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var capturedRequest = new TaskCompletionSource<CapturedRequest>(TaskCreationOptions.RunContinuationsAsynchronously);

            var app = builder.Build();

            app.MapPost("/v1/responses", async context =>
            {
                using var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync(context.RequestAborted);

                capturedRequest.TrySetResult(new CapturedRequest(
                    context.Request.Method,
                    context.Request.Path.Value ?? string.Empty,
                    context.Request.QueryString.Value ?? string.Empty,
                    context.Request.Headers[HeaderNames.Authorization].ToString(),
                    body));

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"ok":true}""", context.RequestAborted);
            });

            await app.StartAsync();

            var addressFeature = app.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>();
            var baseUrl = addressFeature?.Addresses.Single() ?? throw new InvalidOperationException("Upstream server did not expose an address.");

            return new TestUpstreamServer(app, baseUrl, capturedRequest);
        }

        public async Task<CapturedRequest> WaitForRequestAsync(TimeSpan timeout)
        {
            var capturedRequest = await TryWaitForRequestAsync(timeout);
            return capturedRequest ?? throw new TimeoutException("Timed out waiting for upstream request.");
        }

        public async Task<CapturedRequest?> TryWaitForRequestAsync(TimeSpan timeout)
        {
            var completedTask = await Task.WhenAny(_capturedRequest.Task, Task.Delay(timeout));
            return completedTask == _capturedRequest.Task
                ? await _capturedRequest.Task
                : null;
        }

        public ValueTask DisposeAsync() => _application.DisposeAsync();
    }

    private sealed record CapturedRequest(
        string Method,
        string Path,
        string QueryString,
        string Authorization,
        string Body);
}
