using Kinof.Api.Services;

namespace Kinof.Api;

public static class AgentEndpoints
{
    public const string ApiKeyHeader = "X-Agent-Key";

    /// <summary>
    /// Tracking agents authenticate with their own API key header instead of a JWT,
    /// so this group is intentionally left out of the authorization pipeline.
    /// </summary>
    public static void MapAgentEndpoints(this WebApplication app)
    {
        var agent = app.MapGroup("/api/agent");

        agent.MapPost("/register", (
            AgentRegisterRequest request,
            AgentService service,
            CancellationToken cancellationToken) =>
            service.RegisterAsync(request, cancellationToken));

        agent.MapPost("/heartbeat", (
            AgentHeartbeatRequest? request,
            HttpRequest httpRequest,
            AgentService service,
            CancellationToken cancellationToken) =>
            service.HeartbeatAsync(ReadKey(httpRequest), request, cancellationToken));

        agent.MapPost("/logs", (
            AgentLogsRequest request,
            HttpRequest httpRequest,
            AgentService service,
            CancellationToken cancellationToken) =>
            service.IngestLogsAsync(ReadKey(httpRequest), request, cancellationToken));
    }

    private static string? ReadKey(HttpRequest request) =>
        request.Headers.TryGetValue(ApiKeyHeader, out var values) ? values.ToString() : null;
}
