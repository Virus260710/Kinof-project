using System.Text.Json;
using System.Text.Json.Serialization;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record AgentRegisterRequest(string? ApiKey, string? Hostname);

public sealed record AgentHeartbeatRequest(string? Hostname);

public sealed record AgentLogEntry(string? EventType, JsonElement? Data, DateTimeOffset? At);

public sealed record AgentLogsRequest(IReadOnlyList<AgentLogEntry>? Events);

/// <summary>
/// Canonical shape stored in <see cref="AgentLog.DataJson"/> so readers never have to
/// cope with whatever key casing an agent happened to send.
/// </summary>
public sealed record AgentLogPayload
{
    public Guid? UserId { get; init; }
    public string? Username { get; init; }
    public string? DisplayName { get; init; }
    public string? UserType { get; init; }
    public string? Program { get; init; }
    public string? Website { get; init; }
    public string? Activity { get; init; }
    public int? DurationMinutes { get; init; }
    public bool Suspicious { get; init; }
    public string? MatchedPattern { get; init; }
    public string? Source { get; init; }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static AgentLogPayload Parse(JsonElement? data)
    {
        if (data is not JsonElement element || element.ValueKind != JsonValueKind.Object)
            return new AgentLogPayload();
        try
        {
            return element.Deserialize<AgentLogPayload>(SerializerOptions) ?? new AgentLogPayload();
        }
        catch (JsonException)
        {
            return new AgentLogPayload();
        }
    }

    public static AgentLogPayload Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new AgentLogPayload();
        try
        {
            return JsonSerializer.Deserialize<AgentLogPayload>(json, SerializerOptions) ?? new AgentLogPayload();
        }
        catch (JsonException)
        {
            return new AgentLogPayload();
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);
}

public static class AgentEventTypes
{
    public const string Login = "login";
    public const string Logout = "logout";
    public const string Program = "program";
    public const string Website = "website";
    public const string Suspicious = "suspicious";

    public static string? Normalize(string? eventType) => eventType?.Trim().ToLowerInvariant() switch
    {
        Login => Login,
        Logout => Logout,
        Program => Program,
        Website or "web" => Website,
        Suspicious => Suspicious,
        _ => null
    };
}

public sealed class AgentService(AppDbContext db)
{
    public const int HeartbeatTimeoutSeconds = 60;
    private const int MaxEventsPerRequest = 200;

    public static bool IsOnline(DateTime? lastHeartbeat, DateTime nowUtc) =>
        lastHeartbeat is DateTime beat && beat >= nowUtc.AddSeconds(-HeartbeatTimeoutSeconds);

    public async Task<IResult> RegisterAsync(AgentRegisterRequest? request, CancellationToken cancellationToken)
    {
        var agent = await FindByKeyAsync(request?.ApiKey, cancellationToken);
        if (agent is null)
            return InvalidKey();

        var seat = await db.Seats.SingleOrDefaultAsync(x => x.Id == agent.SeatId, cancellationToken);
        if (seat is null)
            return Results.Conflict(new { message = "Agent นี้ยังไม่ได้ผูกกับเครื่อง" });

        var hostname = request?.Hostname?.Trim();
        if (!string.IsNullOrWhiteSpace(hostname))
        {
            agent.Hostname = hostname;
            if (string.IsNullOrWhiteSpace(seat.ComputerName))
                seat.ComputerName = hostname;
        }

        agent.LastHeartbeat = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            agentId = agent.Id,
            seatId = agent.SeatId,
            roomId = seat.RoomId,
            seatNumber = seat.SeatNumber,
            hostname = agent.Hostname
        });
    }

    public async Task<IResult> HeartbeatAsync(
        string? apiKey,
        AgentHeartbeatRequest? request,
        CancellationToken cancellationToken)
    {
        var agent = await FindByKeyAsync(apiKey, cancellationToken);
        if (agent is null)
            return InvalidKey();

        var hostname = request?.Hostname?.Trim();
        if (!string.IsNullOrWhiteSpace(hostname))
            agent.Hostname = hostname;
        agent.LastHeartbeat = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        // Remote logout/lock commands land here in Phase 7.
        return Results.Ok(new { ok = true, commands = Array.Empty<object>() });
    }

    public async Task<IResult> IngestLogsAsync(
        string? apiKey,
        AgentLogsRequest? request,
        CancellationToken cancellationToken)
    {
        var agent = await FindByKeyAsync(apiKey, cancellationToken);
        if (agent is null)
            return InvalidKey();

        var events = request?.Events;
        if (events is null || events.Count == 0)
            return Results.BadRequest(new { message = "ต้องระบุ events อย่างน้อย 1 รายการ" });
        if (events.Count > MaxEventsPerRequest)
            return Results.BadRequest(new { message = $"ส่งได้สูงสุด {MaxEventsPerRequest} events ต่อครั้ง" });

        var seat = await db.Seats.SingleOrDefaultAsync(x => x.Id == agent.SeatId, cancellationToken);
        var blacklist = await db.WebsiteBlacklist.AsNoTracking()
            .Select(x => x.UrlPattern)
            .ToListAsync(cancellationToken);
        var userCache = new Dictionary<Guid, User?>();

        var accepted = 0;
        var flagged = 0;
        var skipped = 0;

        foreach (var entry in events)
        {
            var eventType = AgentEventTypes.Normalize(entry.EventType);
            if (eventType is null)
            {
                skipped++;
                continue;
            }

            var payload = AgentLogPayload.Parse(entry.Data);
            payload = await EnrichUserAsync(payload, userCache, cancellationToken);

            if (eventType == AgentEventTypes.Suspicious)
            {
                payload = payload with { Suspicious = true };
            }
            else if (eventType == AgentEventTypes.Website)
            {
                var match = MatchBlacklist(payload.Website, blacklist);
                if (match is not null)
                    payload = payload with { Suspicious = true, MatchedPattern = match };
            }

            if (payload.Suspicious)
                flagged++;

            db.AgentLogs.Add(new AgentLog
            {
                AgentId = agent.Id,
                EventType = eventType,
                DataJson = payload.ToJson(),
                CreatedAt = entry.At?.UtcDateTime ?? DateTime.UtcNow
            });
            accepted++;

            if (seat is not null)
            {
                if (eventType == AgentEventTypes.Login) seat.Status = SeatStatus.Occupied;
                if (eventType == AgentEventTypes.Logout) seat.Status = SeatStatus.Available;
            }
        }

        agent.LastHeartbeat = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { accepted, flagged, skipped });
    }

    public static string? MatchBlacklist(string? website, IReadOnlyCollection<string> patterns)
    {
        if (string.IsNullOrWhiteSpace(website) || patterns.Count == 0)
            return null;
        var value = website.Trim().ToLowerInvariant();
        return patterns.FirstOrDefault(pattern =>
            !string.IsNullOrWhiteSpace(pattern) &&
            value.Contains(pattern.Trim().ToLowerInvariant(), StringComparison.Ordinal));
    }

    private async Task<AgentLogPayload> EnrichUserAsync(
        AgentLogPayload payload,
        Dictionary<Guid, User?> cache,
        CancellationToken cancellationToken)
    {
        if (payload.UserId is not Guid userId)
            return payload;

        if (!cache.TryGetValue(userId, out var user))
        {
            user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
            cache[userId] = user;
        }

        if (user is null)
            return payload;

        return payload with
        {
            Username = user.Username,
            DisplayName = string.IsNullOrWhiteSpace(payload.DisplayName)
                ? TrackingService.ShortDisplayName(user)
                : payload.DisplayName,
            UserType = TrackingService.UserTypeLabel(user.UserType)
        };
    }

    private Task<Agent?> FindByKeyAsync(string? apiKey, CancellationToken cancellationToken)
    {
        var key = apiKey?.Trim();
        return string.IsNullOrWhiteSpace(key)
            ? Task.FromResult<Agent?>(null)
            : db.Agents.SingleOrDefaultAsync(x => x.ApiKey == key, cancellationToken);
    }

    private static IResult InvalidKey() =>
        Results.Json(new { message = "Agent key ไม่ถูกต้อง" }, statusCode: StatusCodes.Status401Unauthorized);
}
