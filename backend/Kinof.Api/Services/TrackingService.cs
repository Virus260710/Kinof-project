using System.Globalization;
using System.Security.Cryptography;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record TrackingUserResponse(string DisplayName, string? Username, string? UserType);

public sealed record TrackingSessionResponse(TrackingUserResponse User, string StartedAt);

public sealed record TrackingActivityResponse(
    string Id,
    Guid SeatId,
    Guid RoomId,
    string RoomName,
    string SeatLabel,
    TrackingUserResponse? User,
    string At,
    string Activity,
    string ActivityType,
    int? DurationMinutes,
    bool Suspicious,
    string? Website,
    string? Program);

public sealed record UpdateRoomStatusRequest(string? Status);

public sealed record RoomBulkActionRequest(string? Action);

public sealed record CreateAgentRequest(Guid SeatId, string? Hostname);

/// <summary>Open session derived from the latest login/logout pair of a seat.</summary>
public sealed record SeatSession(AgentLogPayload Payload, DateTime StartedAtUtc);

public sealed class TrackingService(AppDbContext db, AuditLogService auditLog)
{
    private const int MaxActivityRows = 500;

    private sealed class ActivityJoin
    {
        public required AgentLog Log { get; init; }
        public required Seat Seat { get; init; }
        public required Room Room { get; init; }
    }

    private sealed record ActivityRow(
        long Id,
        string EventType,
        string? DataJson,
        DateTime CreatedAt,
        Guid SeatId,
        int SeatNumber,
        Guid RoomId,
        string RoomName);

    // ---------------------------------------------------------------- reads

    public async Task<IResult> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var sessions = await GetOpenSessionsAsync(cancellationToken);

        var openRoomIds = (await db.Rooms.AsNoTracking()
            .Where(room => room.Status == RoomStatus.Open)
            .Select(room => room.Id)
            .ToListAsync(cancellationToken)).ToHashSet();

        var seats = await db.Seats.AsNoTracking()
            .Select(seat => new { seat.Id, seat.RoomId })
            .ToListAsync(cancellationToken);
        var onlineSeatIds = await GetOnlineSeatIdsAsync(now, cancellationToken);

        var openRoomSeats = seats.Where(seat => openRoomIds.Contains(seat.RoomId)).ToList();

        var pendingHelpRequests = await db.ProblemReports.AsNoTracking()
            .CountAsync(report => report.Status != ProblemReportStatus.Resolved, cancellationToken);

        var (dayStart, dayEnd) = BangkokDayRangeUtc(null);
        var todayLogs = await db.AgentLogs.AsNoTracking()
            .Where(log => log.CreatedAt >= dayStart!.Value && log.CreatedAt < dayEnd!.Value)
            .Select(log => new { log.EventType, log.DataJson })
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            activeUsers = sessions.Count,
            machinesReady = openRoomSeats.Count(seat => onlineSeatIds.Contains(seat.Id)),
            machinesTotal = openRoomSeats.Count,
            pendingHelpRequests,
            websitesToday = todayLogs.Count(log => log.EventType == AgentEventTypes.Website),
            flaggedCount = todayLogs.Count(log =>
                log.EventType == AgentEventTypes.Suspicious ||
                (log.EventType == AgentEventTypes.Website && AgentLogPayload.Parse(log.DataJson).Suspicious))
        });
    }

    public async Task<IResult> GetRoomsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rooms = await db.Rooms.AsNoTracking().OrderBy(room => room.Name).ToListAsync(cancellationToken);
        var seats = await db.Seats.AsNoTracking()
            .Select(seat => new { seat.Id, seat.RoomId })
            .ToListAsync(cancellationToken);
        var onlineSeatIds = await GetOnlineSeatIdsAsync(now, cancellationToken);
        var sessions = await GetOpenSessionsAsync(cancellationToken);

        var response = rooms.Select(room =>
        {
            var roomSeats = seats.Where(seat => seat.RoomId == room.Id).ToList();
            return new
            {
                id = room.Id,
                name = room.Name,
                status = room.Status.ToString().ToLowerInvariant(),
                seatCount = roomSeats.Count,
                agentOnlineCount = roomSeats.Count(seat => onlineSeatIds.Contains(seat.Id)),
                activeUserCount = roomSeats.Count(seat => sessions.ContainsKey(seat.Id))
            };
        });

        return Results.Ok(response);
    }

    public async Task<IResult> GetSeatsAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await db.Rooms.AsNoTracking().SingleOrDefaultAsync(x => x.Id == roomId, cancellationToken);
        if (room is null)
            return Results.NotFound(new { message = "ไม่พบห้องแล็บ" });

        var now = DateTime.UtcNow;
        var seats = await db.Seats.AsNoTracking()
            .Where(seat => seat.RoomId == roomId)
            .OrderBy(seat => seat.SeatNumber)
            .ToListAsync(cancellationToken);
        var seatIds = seats.Select(seat => seat.Id).ToList();
        var agents = await db.Agents.AsNoTracking()
            .Where(agent => seatIds.Contains(agent.SeatId))
            .ToDictionaryAsync(agent => agent.SeatId, cancellationToken);
        var sessions = await GetOpenSessionsAsync(cancellationToken);

        var response = seats.Select(seat =>
        {
            var agent = agents.GetValueOrDefault(seat.Id);
            var online = AgentService.IsOnline(agent?.LastHeartbeat, now);
            var session = sessions.GetValueOrDefault(seat.Id);
            return new
            {
                id = seat.Id,
                roomId = seat.RoomId,
                number = seat.SeatNumber,
                label = SeatLabel(seat.SeatNumber),
                computerName = seat.ComputerName,
                status = GetSeatUiStatus(seat, agent, room, session is not null, now),
                agentOnline = online,
                agentRegistered = agent is not null,
                lastHeartbeat = agent?.LastHeartbeat,
                session = session is null ? null : ToSessionResponse(session)
            };
        });

        return Results.Ok(response);
    }

    public async Task<IResult> GetActivityAsync(
        Guid? roomId,
        string? date,
        string? type,
        CancellationToken cancellationToken)
    {
        var normalizedType = string.IsNullOrWhiteSpace(type) ? "all" : type.Trim().ToLowerInvariant();
        var (start, end) = BangkokDayRangeUtc(date);

        var query = ActivityQuery();
        if (roomId is Guid room)
            query = query.Where(row => row.Room.Id == room);
        if (start is not null && end is not null)
            query = query.Where(row => row.Log.CreatedAt >= start.Value && row.Log.CreatedAt < end.Value);

        query = normalizedType switch
        {
            "session" => query.Where(row =>
                row.Log.EventType == AgentEventTypes.Login || row.Log.EventType == AgentEventTypes.Logout),
            "program" => query.Where(row => row.Log.EventType == AgentEventTypes.Program),
            "website" => query.Where(row => row.Log.EventType == AgentEventTypes.Website),
            "flagged" => query.Where(row =>
                row.Log.EventType == AgentEventTypes.Suspicious || row.Log.EventType == AgentEventTypes.Website),
            _ => query
        };

        var rows = await ProjectAsync(query, MaxActivityRows, cancellationToken);
        var activity = rows.Select(ToActivityResponse);
        if (normalizedType == "flagged")
            activity = activity.Where(item => item.Suspicious);

        return Results.Ok(activity.ToArray());
    }

    public async Task<IResult> GetSeatActivityAsync(Guid seatId, int limit, CancellationToken cancellationToken)
    {
        if (!await db.Seats.AsNoTracking().AnyAsync(seat => seat.Id == seatId, cancellationToken))
            return Results.NotFound(new { message = "ไม่พบเครื่องคอมพิวเตอร์" });

        var rows = await ProjectAsync(
            ActivityQuery().Where(row => row.Seat.Id == seatId),
            Math.Clamp(limit, 1, MaxActivityRows),
            cancellationToken);

        return Results.Ok(rows.Select(ToActivityResponse).ToArray());
    }

    // --------------------------------------------------------------- writes

    public async Task<IResult> UpdateRoomStatusAsync(
        Guid actorUserId,
        Guid roomId,
        UpdateRoomStatusRequest? request,
        CancellationToken cancellationToken)
    {
        var status = ParseRoomStatus(request?.Status);
        if (status is null)
            return Results.BadRequest(new { message = "สถานะห้องต้องเป็น open, closed หรือ maintenance" });

        var room = await db.Rooms.SingleOrDefaultAsync(x => x.Id == roomId, cancellationToken);
        if (room is null)
            return Results.NotFound(new { message = "ไม่พบห้องแล็บ" });

        room.Status = status.Value;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "tracking.room_status",
            "room",
            room.Id.ToString(),
            $"{room.Name} → {status.Value.ToString().ToLowerInvariant()}",
            cancellationToken);

        return Results.Ok(new
        {
            id = room.Id,
            name = room.Name,
            status = room.Status.ToString().ToLowerInvariant()
        });
    }

    public async Task<IResult> BulkRoomActionAsync(
        Guid actorUserId,
        Guid roomId,
        RoomBulkActionRequest? request,
        CancellationToken cancellationToken)
    {
        var action = request?.Action?.Trim().ToLowerInvariant();
        if (action is not ("open" or "close" or "maintenance"))
            return Results.BadRequest(new { message = "action ต้องเป็น open, close หรือ maintenance" });

        var room = await db.Rooms.SingleOrDefaultAsync(x => x.Id == roomId, cancellationToken);
        if (room is null)
            return Results.NotFound(new { message = "ไม่พบห้องแล็บ" });

        var now = DateTime.UtcNow;
        var seats = await db.Seats.Where(seat => seat.RoomId == roomId).ToListAsync(cancellationToken);
        var seatIds = seats.Select(seat => seat.Id).ToList();
        var agents = await db.Agents
            .Where(agent => seatIds.Contains(agent.SeatId))
            .ToDictionaryAsync(agent => agent.SeatId, cancellationToken);
        var sessions = await GetOpenSessionsAsync(cancellationToken);

        room.Status = action switch
        {
            "open" => RoomStatus.Open,
            "close" => RoomStatus.Closed,
            _ => RoomStatus.Maintenance
        };

        var loggedOut = 0;
        foreach (var seat in seats)
        {
            if (action == "open")
            {
                seat.Status = AgentService.IsOnline(agents.GetValueOrDefault(seat.Id)?.LastHeartbeat, now)
                    ? SeatStatus.Available
                    : SeatStatus.Offline;
                continue;
            }

            // close/maintenance both take the room out of service; queue a logout for
            // anyone still signed in so the derived session closes immediately.
            if (sessions.TryGetValue(seat.Id, out var session) &&
                agents.TryGetValue(seat.Id, out var agent))
            {
                QueueLogout(agent, session, action == "close" ? "room_close" : "room_maintenance");
                loggedOut++;
            }
            seat.Status = SeatStatus.Offline;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "tracking.room_bulk_action",
            "room",
            room.Id.ToString(),
            $"{room.Name}: {action} ({seats.Count} เครื่อง, ออกจากระบบ {loggedOut})",
            cancellationToken);

        return Results.Ok(new
        {
            id = room.Id,
            name = room.Name,
            status = room.Status.ToString().ToLowerInvariant(),
            seatCount = seats.Count,
            loggedOut
        });
    }

    public async Task<IResult> ForceSeatLogoutAsync(
        Guid actorUserId,
        Guid seatId,
        CancellationToken cancellationToken)
    {
        var seat = await db.Seats.SingleOrDefaultAsync(x => x.Id == seatId, cancellationToken);
        if (seat is null)
            return Results.NotFound(new { message = "ไม่พบเครื่องคอมพิวเตอร์" });

        var agent = await db.Agents.SingleOrDefaultAsync(x => x.SeatId == seatId, cancellationToken);
        if (agent is null)
            return Results.Conflict(new { message = "เครื่องนี้ยังไม่ได้ติดตั้ง Agent" });

        var sessions = await GetOpenSessionsAsync(cancellationToken);
        var session = sessions.GetValueOrDefault(seatId);
        QueueLogout(agent, session, "admin");
        seat.Status = SeatStatus.Available;

        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "tracking.seat_logout",
            "seat",
            seat.Id.ToString(),
            $"{SeatLabel(seat.SeatNumber)} ({session?.Payload.Username ?? "ไม่มีผู้ใช้"})",
            cancellationToken);

        return Results.Ok(new { seatId = seat.Id, hadSession = session is not null });
    }

    // ----------------------------------------------------- agent management

    public async Task<IResult> ListAgentsAsync(Guid? roomId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = from agent in db.Agents.AsNoTracking()
                    join seat in db.Seats.AsNoTracking() on agent.SeatId equals seat.Id
                    join room in db.Rooms.AsNoTracking() on seat.RoomId equals room.Id
                    select new
                    {
                        agent.Id,
                        agent.SeatId,
                        agent.Hostname,
                        agent.LastHeartbeat,
                        agent.CreatedAt,
                        seat.SeatNumber,
                        seat.ComputerName,
                        RoomId = room.Id,
                        RoomName = room.Name
                    };
        if (roomId is Guid filterRoomId)
            query = query.Where(row => row.RoomId == filterRoomId);

        var rows = await query
            .OrderBy(row => row.RoomName)
            .ThenBy(row => row.SeatNumber)
            .ToListAsync(cancellationToken);

        return Results.Ok(rows.Select(row => new
        {
            id = row.Id,
            seatId = row.SeatId,
            roomId = row.RoomId,
            roomName = row.RoomName,
            seatNumber = row.SeatNumber,
            seatLabel = SeatLabel(row.SeatNumber),
            computerName = row.ComputerName,
            hostname = row.Hostname,
            lastHeartbeat = row.LastHeartbeat,
            online = AgentService.IsOnline(row.LastHeartbeat, now),
            createdAt = row.CreatedAt
        }).ToArray());
    }

    public async Task<IResult> CreateAgentAsync(
        Guid actorUserId,
        CreateAgentRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.SeatId == Guid.Empty)
            return Results.BadRequest(new { message = "ต้องระบุ seatId" });

        var seat = await db.Seats.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.SeatId, cancellationToken);
        if (seat is null)
            return Results.NotFound(new { message = "ไม่พบเครื่องคอมพิวเตอร์" });
        if (await db.Agents.AnyAsync(x => x.SeatId == request.SeatId, cancellationToken))
            return Results.Conflict(new { message = "เครื่องนี้มี Agent อยู่แล้ว" });

        var agent = new Agent
        {
            SeatId = seat.Id,
            ApiKey = GenerateApiKey(),
            Hostname = string.IsNullOrWhiteSpace(request.Hostname) ? seat.ComputerName : request.Hostname.Trim()
        };
        db.Agents.Add(agent);
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "tracking.agent_create",
            "agent",
            agent.Id.ToString(),
            $"seat {SeatLabel(seat.SeatNumber)}",
            cancellationToken);

        // apiKey is returned once at creation time and never listed again.
        return Results.Ok(new
        {
            id = agent.Id,
            seatId = agent.SeatId,
            hostname = agent.Hostname,
            apiKey = agent.ApiKey
        });
    }

    public static string GenerateApiKey() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

    // --------------------------------------------------------- session logic

    public async Task<Dictionary<Guid, SeatSession>> GetOpenSessionsAsync(CancellationToken cancellationToken)
    {
        var latestLogins = await (
            from log in db.AgentLogs.AsNoTracking()
            join agent in db.Agents.AsNoTracking() on log.AgentId equals agent.Id
            where log.EventType == AgentEventTypes.Login
            group log by agent.SeatId into seatGroup
            select new { SeatId = seatGroup.Key, LogId = seatGroup.Max(log => log.Id) })
            .ToListAsync(cancellationToken);
        if (latestLogins.Count == 0)
            return [];

        var latestLogouts = await (
            from log in db.AgentLogs.AsNoTracking()
            join agent in db.Agents.AsNoTracking() on log.AgentId equals agent.Id
            where log.EventType == AgentEventTypes.Logout
            group log by agent.SeatId into seatGroup
            select new { SeatId = seatGroup.Key, LogId = seatGroup.Max(log => log.Id) })
            .ToDictionaryAsync(row => row.SeatId, row => row.LogId, cancellationToken);

        var openLoginIds = latestLogins
            .Where(row => row.LogId > latestLogouts.GetValueOrDefault(row.SeatId, 0L))
            .Select(row => row.LogId)
            .ToList();
        if (openLoginIds.Count == 0)
            return [];

        var logs = await (from log in db.AgentLogs.AsNoTracking()
                          join agent in db.Agents.AsNoTracking() on log.AgentId equals agent.Id
                          where openLoginIds.Contains(log.Id)
                          select new { agent.SeatId, log.DataJson, log.CreatedAt })
            .ToListAsync(cancellationToken);

        return logs.ToDictionary(
            log => log.SeatId,
            log => new SeatSession(AgentLogPayload.Parse(log.DataJson), log.CreatedAt));
    }

    public static string GetSeatUiStatus(Seat seat, Agent? agent, Room room, bool hasSession, DateTime nowUtc)
    {
        if (room.Status == RoomStatus.Maintenance) return "maintenance";
        if (!AgentService.IsOnline(agent?.LastHeartbeat, nowUtc)) return "offline";
        if (hasSession || seat.Status == SeatStatus.Occupied) return "in_use";
        return "available";
    }

    // -------------------------------------------------------------- helpers

    public static string SeatLabel(int seatNumber) => $"คอม {seatNumber:00}";

    public static string ShortDisplayName(User user)
    {
        var lastInitial = user.LastName.Trim().FirstOrDefault();
        return lastInitial == default
            ? user.FirstName
            : $"{user.FirstName} {lastInitial}.";
    }

    public static string UserTypeLabel(UserType userType) => userType switch
    {
        UserType.Student => "นักศึกษา",
        UserType.Staff => "บุคลากร",
        UserType.External => "บุคคลภายนอก",
        UserType.Admin => "ผู้ดูแลระบบ",
        UserType.SuperAdmin => "ผู้ดูแลสูงสุด",
        _ => "ผู้ใช้งาน"
    };

    /// <summary>ISO 8601 with the Bangkok offset so the browser renders local lab time.</summary>
    public static string FormatBangkok(DateTime utc)
    {
        var kindUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var offset = BangkokTime.Zone.GetUtcOffset(kindUtc);
        return new DateTimeOffset(BangkokTime.ToLocal(kindUtc), offset)
            .ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
    }

    private void QueueLogout(Agent agent, SeatSession? session, string source)
    {
        db.AgentLogs.Add(new AgentLog
        {
            AgentId = agent.Id,
            EventType = AgentEventTypes.Logout,
            DataJson = new AgentLogPayload
            {
                UserId = session?.Payload.UserId,
                Username = session?.Payload.Username,
                DisplayName = session?.Payload.DisplayName,
                UserType = session?.Payload.UserType,
                Source = source
            }.ToJson(),
            CreatedAt = DateTime.UtcNow
        });
    }

    // Kept as entity references so filters stay translatable; ActivityRow is only
    // materialised in the final projection.
    private IQueryable<ActivityJoin> ActivityQuery() =>
        from log in db.AgentLogs.AsNoTracking()
        join agent in db.Agents.AsNoTracking() on log.AgentId equals agent.Id
        join seat in db.Seats.AsNoTracking() on agent.SeatId equals seat.Id
        join room in db.Rooms.AsNoTracking() on seat.RoomId equals room.Id
        select new ActivityJoin { Log = log, Seat = seat, Room = room };

    private static Task<List<ActivityRow>> ProjectAsync(
        IQueryable<ActivityJoin> query,
        int take,
        CancellationToken cancellationToken) =>
        query
            .OrderByDescending(row => row.Log.CreatedAt)
            .ThenByDescending(row => row.Log.Id)
            .Take(take)
            .Select(row => new ActivityRow(
                row.Log.Id,
                row.Log.EventType,
                row.Log.DataJson,
                row.Log.CreatedAt,
                row.Seat.Id,
                row.Seat.SeatNumber,
                row.Room.Id,
                row.Room.Name))
            .ToListAsync(cancellationToken);

    private async Task<HashSet<Guid>> GetOnlineSeatIdsAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var cutoff = nowUtc.AddSeconds(-AgentService.HeartbeatTimeoutSeconds);
        var seatIds = await db.Agents.AsNoTracking()
            .Where(agent => agent.LastHeartbeat != null && agent.LastHeartbeat >= cutoff)
            .Select(agent => agent.SeatId)
            .ToListAsync(cancellationToken);
        return seatIds.ToHashSet();
    }

    private static TrackingSessionResponse ToSessionResponse(SeatSession session) => new(
        ToUserResponse(session.Payload) ?? new TrackingUserResponse("ผู้ใช้งาน", null, null),
        FormatBangkok(session.StartedAtUtc));

    private static TrackingUserResponse? ToUserResponse(AgentLogPayload payload)
    {
        if (payload.UserId is null &&
            string.IsNullOrWhiteSpace(payload.Username) &&
            string.IsNullOrWhiteSpace(payload.DisplayName))
        {
            return null;
        }

        return new TrackingUserResponse(
            payload.DisplayName ?? payload.Username ?? "ผู้ใช้งาน",
            payload.Username,
            payload.UserType);
    }

    private static TrackingActivityResponse ToActivityResponse(ActivityRow row)
    {
        var payload = AgentLogPayload.Parse(row.DataJson);
        return new TrackingActivityResponse(
            row.Id.ToString(CultureInfo.InvariantCulture),
            row.SeatId,
            row.RoomId,
            row.RoomName,
            SeatLabel(row.SeatNumber),
            ToUserResponse(payload),
            FormatBangkok(row.CreatedAt),
            DescribeActivity(row.EventType, payload),
            row.EventType,
            payload.DurationMinutes,
            payload.Suspicious,
            payload.Website,
            payload.Program);
    }

    private static string DescribeActivity(string eventType, AgentLogPayload payload) => eventType switch
    {
        AgentEventTypes.Login => "เข้าสู่ระบบ",
        AgentEventTypes.Logout => payload.Source is null ? "ออกจากระบบ" : "ออกจากระบบ (โดยผู้ดูแล)",
        AgentEventTypes.Program => $"เปิด {payload.Program ?? "โปรแกรม"}",
        AgentEventTypes.Website => $"เข้าเว็บไซต์ {payload.Website ?? "-"}",
        AgentEventTypes.Suspicious => payload.Activity ?? "กิจกรรมน่าสงสัย",
        _ => payload.Activity ?? eventType
    };

    private static RoomStatus? ParseRoomStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "open" => RoomStatus.Open,
        "closed" or "close" => RoomStatus.Closed,
        "maintenance" => RoomStatus.Maintenance,
        _ => null
    };

    private static (DateTime? Start, DateTime? End) BangkokDayRangeUtc(string? date)
    {
        if (string.Equals(date?.Trim(), "all", StringComparison.OrdinalIgnoreCase))
            return (null, null);

        var day = DateOnly.TryParseExact(date?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.ToDateTime(TimeOnly.MinValue)
            : BangkokTime.ToLocal(DateTime.UtcNow).Date;

        return (BangkokTime.ToUtc(day), BangkokTime.ToUtc(day.AddDays(1)));
    }
}
