using System.Text.Json;
using System.Text.Json.Serialization;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record AgentRegisterRequest(string? ApiKey, string? Hostname);

public sealed record AgentHeartbeatRequest(string? Hostname);

public sealed record AgentLogEntry(string? EventType, JsonElement? Data, DateTimeOffset? At);

public sealed record AgentLogsRequest(IReadOnlyList<AgentLogEntry>? Events);

public sealed record AgentSessionLoginRequest(string? Username, string? Password);

public sealed record AgentSessionVerifyOtpRequest(Guid UserId, string? Code);

public sealed record AgentSessionResendOtpRequest(Guid UserId);

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
    public const string UnknownProgram = "unknown_program";

    public static string? Normalize(string? eventType) => eventType?.Trim().ToLowerInvariant() switch
    {
        Login => Login,
        Logout => Logout,
        Program => Program,
        Website or "web" => Website,
        Suspicious => Suspicious,
        UnknownProgram or "unknown" => UnknownProgram,
        _ => null
    };
}

public sealed class AgentService(AppDbContext db, AuthService authService, BehaviorScoreService behaviorScore)
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

        var seat = await db.Seats.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == agent.SeatId, cancellationToken);
        var session = await GetOpenSessionAsync(agent.Id, cancellationToken);
        var programBlacklist = await db.ProgramBlacklist.AsNoTracking()
            .OrderBy(entry => entry.ProcessName)
            .Select(entry => entry.ProcessName)
            .ToListAsync(cancellationToken);
        var programAllowlist = await db.ProgramAllowlist.AsNoTracking()
            .OrderBy(entry => entry.ProcessName)
            .Select(entry => entry.ProcessName)
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            ok = true,
            commands = Array.Empty<object>(),
            seatStatus = seat is null
                ? SeatStatus.Offline.ToString().ToLowerInvariant()
                : seat.Status.ToString().ToLowerInvariant(),
            programBlacklist,
            programAllowlist,
            session = session is null ? null : new
            {
                userId = session.Payload.UserId,
                username = session.Payload.Username,
                displayName = session.Payload.DisplayName,
                userType = session.Payload.UserType,
                startedAt = session.StartedAtUtc
            }
        });
    }

    public async Task<IResult> GetWebsiteBlacklistAsync(string? apiKey, CancellationToken cancellationToken)
    {
        var agent = await FindByKeyAsync(apiKey, cancellationToken);
        if (agent is null)
            return InvalidKey();

        var domains = await db.WebsiteBlacklist.AsNoTracking()
            .OrderBy(entry => entry.UrlPattern)
            .Select(entry => entry.UrlPattern)
            .ToListAsync(cancellationToken);

        return Results.Ok(new { domains });
    }

    public async Task<IResult> GetProgramBlacklistAsync(string? apiKey, CancellationToken cancellationToken)
    {
        var agent = await FindByKeyAsync(apiKey, cancellationToken);
        if (agent is null)
            return InvalidKey();

        var processNames = await db.ProgramBlacklist.AsNoTracking()
            .OrderBy(entry => entry.ProcessName)
            .Select(entry => entry.ProcessName)
            .ToListAsync(cancellationToken);

        return Results.Ok(new { processNames });
    }

    public async Task<IResult> GetProgramAllowlistAsync(string? apiKey, CancellationToken cancellationToken)
    {
        var agent = await FindByKeyAsync(apiKey, cancellationToken);
        if (agent is null)
            return InvalidKey();

        var processNames = await db.ProgramAllowlist.AsNoTracking()
            .OrderBy(entry => entry.ProcessName)
            .Select(entry => entry.ProcessName)
            .ToListAsync(cancellationToken);

        return Results.Ok(new { processNames });
    }

    public async Task<IResult> StartSessionLoginAsync(
        string? apiKey,
        AgentSessionLoginRequest? request,
        CancellationToken cancellationToken)
    {
        var agent = await FindByKeyAsync(apiKey, cancellationToken);
        if (agent is null)
            return InvalidKey();

        var occupied = await SeatOccupiedBySomeoneElseAsync(agent.Id, null, cancellationToken);
        if (occupied)
        {
            return Results.Conflict(new
            {
                message = "เครื่องนี้มีผู้ใช้บัญชี KINOF อยู่แล้ว กรุณาเลือกเครื่องอื่น หรือให้ผู้ดูแลสั่งออกจากระบบ"
            });
        }

        if (string.IsNullOrWhiteSpace(request?.Username) || request.Password is null)
        {
            return Results.BadRequest(new
            {
                message = "กรุณากรอกชื่อผู้ใช้หรืออีเมล และรหัสผ่านบัญชี KINOF — รหัสฉุกเฉินที่ประตูใช้ที่ Kiosk เท่านั้น"
            });
        }

        return await authService.LoginAsync(
            new LoginRequest(request.Username, request.Password),
            cancellationToken);
    }

    public async Task<IResult> VerifySessionOtpAsync(
        string? apiKey,
        AgentSessionVerifyOtpRequest? request,
        CancellationToken cancellationToken)
    {
        var agent = await FindByKeyAsync(apiKey, cancellationToken);
        if (agent is null)
            return InvalidKey();

        var (user, otp, error) = await authService.MatchLoginEmailOtpAsync(
            request?.UserId ?? Guid.Empty,
            request?.Code,
            cancellationToken);
        if (error is not null)
            return error;
        if (user is null || otp is null)
            return Results.Unauthorized();

        var seat = await db.Seats.SingleOrDefaultAsync(x => x.Id == agent.SeatId, cancellationToken);
        if (seat is null)
            return Results.Conflict(new { message = "Agent นี้ยังไม่ได้ผูกกับเครื่อง" });

        var room = await db.Rooms.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == seat.RoomId, cancellationToken);
        if (room is null)
            return Results.NotFound(new { message = "ไม่พบห้องแล็บนี้" });
        if (room.Status != RoomStatus.Open)
        {
            return Results.Json(
                new { message = "ห้องนี้ปิดหรืออยู่ระหว่างปรับปรุง จึงยังล็อกอินเครื่องไม่ได้" },
                statusCode: StatusCodes.Status403Forbidden);
        }

        var open = await GetOpenSessionAsync(agent.Id, cancellationToken);
        if (open is not null && open.Payload.UserId is Guid occupant && occupant != user.Id)
        {
            return Results.Conflict(new
            {
                message = "เครื่องนี้มีผู้ใช้บัญชี KINOF อยู่แล้ว กรุณาเลือกเครื่องอื่น หรือให้ผู้ดูแลสั่งออกจากระบบ"
            });
        }

        otp.UsedAt = DateTime.UtcNow;
        seat.Status = SeatStatus.Occupied;
        agent.LastHeartbeat = DateTime.UtcNow;
        if (open is null)
            AddSessionLog(agent.Id, user, AgentEventTypes.Login, "agent");
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            ok = true,
            user = new
            {
                id = user.Id,
                username = user.Username,
                displayName = TrackingService.ShortDisplayName(user),
                userType = TrackingService.UserTypeLabel(user.UserType)
            },
            seat = new
            {
                id = seat.Id,
                seatNumber = seat.SeatNumber,
                seatLabel = TrackingService.SeatLabel(seat.SeatNumber),
                roomId = seat.RoomId,
                status = SeatStatus.Occupied.ToString().ToLowerInvariant()
            }
        });
    }

    public async Task<IResult> ResendSessionOtpAsync(
        string? apiKey,
        AgentSessionResendOtpRequest? request,
        CancellationToken cancellationToken)
    {
        var agent = await FindByKeyAsync(apiKey, cancellationToken);
        if (agent is null)
            return InvalidKey();

        return await authService.ResendOtpAsync(
            new ResendEmailOtpRequest(request?.UserId ?? Guid.Empty),
            cancellationToken);
    }

    public async Task<IResult> LogoutSessionAsync(string? apiKey, CancellationToken cancellationToken)
    {
        var agent = await FindByKeyAsync(apiKey, cancellationToken);
        if (agent is null)
            return InvalidKey();

        var seat = await db.Seats.SingleOrDefaultAsync(x => x.Id == agent.SeatId, cancellationToken);
        var session = await GetOpenSessionAsync(agent.Id, cancellationToken);
        if (session is not null)
            AddSessionLog(agent.Id, null, AgentEventTypes.Logout, "agent", session.Payload);

        if (seat is not null)
            seat.Status = SeatStatus.Available;

        agent.LastHeartbeat = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new
        {
            ok = true,
            seatStatus = SeatStatus.Available.ToString().ToLowerInvariant()
        });
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
        var room = seat is null
            ? null
            : await db.Rooms.AsNoTracking().SingleOrDefaultAsync(item => item.Id == seat.RoomId, cancellationToken);
        var blacklist = await db.WebsiteBlacklist.AsNoTracking()
            .Select(x => x.UrlPattern)
            .ToListAsync(cancellationToken);
        var blockedPrograms = await db.ProgramBlacklist.AsNoTracking()
            .Select(x => x.ProcessName)
            .ToListAsync(cancellationToken);
        var allowedPrograms = await db.ProgramAllowlist.AsNoTracking()
            .Select(x => x.ProcessName)
            .ToListAsync(cancellationToken);
        var userCache = new Dictionary<Guid, User?>();

        var accepted = 0;
        var flagged = 0;
        var skipped = 0;
        var session = await GetOpenSessionAsync(agent.Id, cancellationToken);
        var flaggedEvents = new List<(Guid? UserId, string? DisplayName, string? Username, string EventType, AgentLogPayload Payload, DateTime At)>();

        foreach (var entry in events)
        {
            var eventType = AgentEventTypes.Normalize(entry.EventType);
            if (eventType is null)
            {
                skipped++;
                continue;
            }

            // Machine occupancy is only from verified KINOF session login — never from a
            // Windows username or a raw login log the agent made up.
            if (eventType == AgentEventTypes.Login)
            {
                skipped++;
                continue;
            }

            var payload = AgentLogPayload.Parse(entry.Data);
            if (payload.UserId is null && session?.Payload.UserId is Guid sessionUserId)
                payload = payload with { UserId = sessionUserId };
            payload = await EnrichUserAsync(payload, userCache, cancellationToken);

            if (eventType == AgentEventTypes.UnknownProgram)
            {
                var program = ProgramBlacklistService.Normalize(payload.Program) ?? payload.Program?.Trim();
                if (string.IsNullOrWhiteSpace(program) ||
                    MatchProgramBlacklist(program, blockedPrograms) is not null ||
                    MatchProgramBlacklist(program, allowedPrograms) is not null)
                {
                    skipped++;
                    continue;
                }

                payload = payload with
                {
                    Program = program,
                    Suspicious = false,
                    Activity = string.IsNullOrWhiteSpace(payload.Activity)
                        ? $"พบ {program} (ไม่ใช่รายการอนุญาต/ห้าม)"
                        : payload.Activity
                };
            }
            else if (eventType == AgentEventTypes.Suspicious)
            {
                payload = payload with { Suspicious = true };
            }
            else if (eventType == AgentEventTypes.Website)
            {
                var match = MatchBlacklist(payload.Website, blacklist);
                if (match is not null)
                    payload = payload with { Suspicious = true, MatchedPattern = match };
            }
            else if (eventType == AgentEventTypes.Program)
            {
                var match = MatchProgramBlacklist(payload.Program, blockedPrograms)
                    ?? payload.MatchedPattern;
                if (match is not null || payload.Suspicious)
                {
                    payload = payload with
                    {
                        Suspicious = true,
                        MatchedPattern = match ?? payload.Program,
                        Activity = string.IsNullOrWhiteSpace(payload.Activity)
                            ? $"บล็อก {payload.Program ?? "โปรแกรม"}"
                            : payload.Activity
                    };
                }
            }

            if (payload.Suspicious && eventType != AgentEventTypes.UnknownProgram)
            {
                flagged++;
                flaggedEvents.Add((
                    payload.UserId,
                    payload.DisplayName,
                    payload.Username,
                    eventType,
                    payload,
                    entry.At?.UtcDateTime ?? DateTime.UtcNow));
            }

            db.AgentLogs.Add(new AgentLog
            {
                AgentId = agent.Id,
                EventType = eventType,
                DataJson = payload.ToJson(),
                CreatedAt = entry.At?.UtcDateTime ?? DateTime.UtcNow
            });
            accepted++;

            if (seat is not null && eventType == AgentEventTypes.Logout)
                seat.Status = SeatStatus.Available;
        }

        agent.LastHeartbeat = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        foreach (var item in flaggedEvents)
        {
            await behaviorScore.QueueFlaggedAsync(
                item.UserId,
                item.DisplayName,
                item.Username,
                seat?.RoomId,
                seat?.Id,
                room?.Name,
                seat is null ? null : TrackingService.SeatLabel(seat.SeatNumber),
                item.EventType,
                item.Payload,
                item.At,
                cancellationToken);
        }
        return Results.Ok(new { accepted, flagged, skipped });
    }

    public static string? MatchProgramBlacklist(string? program, IReadOnlyCollection<string> processNames)
    {
        var normalized = ProgramBlacklistService.Normalize(program);
        if (normalized is null || processNames.Count == 0)
            return null;
        return processNames.FirstOrDefault(name =>
            string.Equals(name, normalized, StringComparison.OrdinalIgnoreCase));
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

    private async Task<SeatSession?> GetOpenSessionAsync(Guid agentId, CancellationToken cancellationToken)
    {
        var lastLogin = await db.AgentLogs.AsNoTracking()
            .Where(log => log.AgentId == agentId && log.EventType == AgentEventTypes.Login)
            .OrderByDescending(log => log.Id)
            .Select(log => new { log.Id, log.DataJson, log.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (lastLogin is null)
            return null;

        var loggedOut = await db.AgentLogs.AsNoTracking().AnyAsync(log =>
            log.AgentId == agentId &&
            log.EventType == AgentEventTypes.Logout &&
            log.Id > lastLogin.Id,
            cancellationToken);
        if (loggedOut)
            return null;

        return new SeatSession(AgentLogPayload.Parse(lastLogin.DataJson), lastLogin.CreatedAt);
    }

    private async Task<bool> SeatOccupiedBySomeoneElseAsync(
        Guid agentId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var open = await GetOpenSessionAsync(agentId, cancellationToken);
        if (open is null)
            return false;
        if (userId is Guid id && open.Payload.UserId == id)
            return false;
        return true;
    }

    private void AddSessionLog(
        Guid agentId,
        User? user,
        string eventType,
        string source,
        AgentLogPayload? existing = null)
    {
        db.AgentLogs.Add(new AgentLog
        {
            AgentId = agentId,
            EventType = eventType,
            DataJson = new AgentLogPayload
            {
                UserId = user?.Id ?? existing?.UserId,
                Username = user?.Username ?? existing?.Username,
                DisplayName = user is null
                    ? existing?.DisplayName
                    : TrackingService.ShortDisplayName(user),
                UserType = user is null
                    ? existing?.UserType
                    : TrackingService.UserTypeLabel(user.UserType),
                Source = source
            }.ToJson()
        });
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
