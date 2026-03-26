using System.Diagnostics;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bugarena.Platform.Tests;

public sealed class AgentEnvironmentSmokeTests
{
    [Theory]
    [InlineData("git", "--version")]
    [InlineData("gh", "--version")]
    [InlineData("codex", "--version")]
    [InlineData("mise", "--version")]
    public async Task ToolchainCommandsAreExecutableAsync(string fileName, string arguments)
    {
        var result = await RunProcessAsync(fileName, arguments);

        Assert.True(result.ExitCode == 0, $"Expected '{fileName} {arguments}' to succeed but it exited with code {result.ExitCode}.{Environment.NewLine}{result.CombinedOutput}");
        Assert.False(string.IsNullOrWhiteSpace(result.CombinedOutput), $"Expected '{fileName} {arguments}' to produce output.");
    }

    [Fact]
    public async Task CapabilityBrokerReadyEndpointIsReachableAsync()
    {
        var baseUrl = RequireEnvironmentVariable("CAPABILITY_BROKER_BASE_URL");

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(AppendTrailingSlash(baseUrl), UriKind.Absolute)
        };

        using var response = await httpClient.GetAsync("health/ready");
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected broker readiness endpoint to succeed but received {(int)response.StatusCode}.{Environment.NewLine}{responseBody}");
    }

    [Fact]
    public async Task TestcontainersCanStartPostgresAndRunAQueryAsync()
    {
        var dockerHost = RequireEnvironmentVariable("DOCKER_HOST");
        Assert.Contains("docker-daemon:2375", dockerHost, StringComparison.Ordinal);

        await using var container = new PostgreSqlBuilder("postgres:16-alpine")
            .Build();

        await container.StartAsync();

        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand("SELECT 1", connection);
        var result = await command.ExecuteScalarAsync();

        Assert.Equal(1, Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static string RequireEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new Xunit.Sdk.XunitException($"Expected environment variable '{name}' to be set for the platform smoke tests.");
    }

    private static string AppendTrailingSlash(string value) =>
        value.EndsWith("/", StringComparison.Ordinal) ? value : $"{value}/";

    private static async Task<ProcessResult> RunProcessAsync(string fileName, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        return new ProcessResult(
            process.ExitCode,
            standardOutput,
            standardError,
            string.Join(
                Environment.NewLine,
                new[] { standardOutput.TrimEnd(), standardError.TrimEnd() }
                    .Where(output => !string.IsNullOrWhiteSpace(output))));
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError, string CombinedOutput);
}
