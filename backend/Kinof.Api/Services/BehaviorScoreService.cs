using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record BlockFlaggedRequest(
    Guid? UserId,
    string? DisplayName,
    string? Username,
    Guid? RoomId,
    Guid? SeatId,
    string? RoomName,
    string? SeatLabel,
    string? EventType,
    string? Website,
    string? Program,
    string? Activity);

public sealed class BehaviorScoreService(
    AppDbContext db,
    WebsiteBlacklistService websites,
    ProgramBlacklistService programs,
    AuditLogService auditLog,
    ILogger<BehaviorScoreService> logger)
{
    public const int MaxScore = 100;
    public const int MinScoreToBook = 50;
    public const int NoShowPoints = 5;
    public const int FlaggedPoints = 5;

    public const string SourceNoShow = "no_show";
    public const string SourceFlagged = "flagged";

    public async Task<IResult> GetMineAsync(Guid userId, CancellationToken cancellationToken)
    {
        await SettleNoShowsAsync(cancellationToken);
        var monthStart = CurrentScoreMonthStartUtc();
        var penalties = await db.BehaviorPenalties.AsNoTracking()
            .Where(item => item.UserId == userId && item.CreatedAt >= monthStart)
            .OrderByDescending(item => item.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        var deducted = await MonthDeductedAsync(userId, monthStart, cancellationToken);
        var nextResetLocal = BangkokTime.ToLocal(monthStart).AddMonths(1);

        return Results.Ok(new
        {
            score = Remaining(deducted),
            maxScore = MaxScore,
            minScoreToBook = MinScoreToBook,
            canBook = Remaining(deducted) >= MinScoreToBook,
            resetsAt = TrackingService.FormatBangkok(BangkokTime.ToUtc(nextResetLocal)),
            penalties = penalties.Select(item => new
            {
                id = item.Id,
                at = TrackingService.FormatBangkok(item.CreatedAt),
                points = item.Points,
                reason = item.Reason,
                source = item.Source
            })
        });
    }

    public async Task<int> RemainingAsync(Guid userId, CancellationToken cancellationToken)
    {
        var deducted = await MonthDeductedAsync(userId, CurrentScoreMonthStartUtc(), cancellationToken);
        return Remaining(deducted);
    }

    public async Task QueueFlaggedAsync(
        Guid? userId,
        string? displayName,
        string? username,
        Guid? roomId,
        Guid? seatId,
        string? roomName,
        string? seatLabel,
        string eventType,
        AgentLogPayload payload,
        DateTime atUtc,
        CancellationToken cancellationToken)
    {
        var (kind, target) = KindAndTarget(eventType, payload);
        if (kind is "unknown" || eventType == AgentEventTypes.UnknownProgram)
            return;
        if (await AlreadyBlockedAsync(kind, target, cancellationToken))
            return;

        var queueKey = QueueKey(userId, kind, target);
        if (await db.BehaviorReviews.AnyAsync(
                item => item.QueueKey == queueKey && item.Status == BehaviorReviewStatus.Cleared,
                cancellationToken))
            return;

        var pending = await db.BehaviorReviews
            .SingleOrDefaultAsync(
                item => item.Status == BehaviorReviewStatus.Pending && item.QueueKey == queueKey,
                cancellationToken);
        if (pending is not null)
        {
            pending.OccurrenceCount += 1;
            pending.LastSeenAt = atUtc;
            pending.Activity = Truncate(FlagReason(eventType, payload), 500);
            if (string.IsNullOrWhiteSpace(pending.DisplayName) && !string.IsNullOrWhiteSpace(displayName))
                pending.DisplayName = displayName;
            if (pending.RoomId is null && roomId is Guid)
            {
                pending.RoomId = roomId;
                pending.SeatId = seatId;
                pending.RoomName = roomName;
                pending.SeatLabel = seatLabel;
            }
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        db.BehaviorReviews.Add(new BehaviorReview
        {
            UserId = userId,
            DisplayName = TruncateOptional(displayName, 120),
            Username = TruncateOptional(username, 50),
            RoomId = roomId,
            SeatId = seatId,
            RoomName = TruncateOptional(roomName, 100),
            SeatLabel = TruncateOptional(seatLabel, 40),
            Kind = kind,
            Target = Truncate(target, 255),
            Activity = Truncate(FlagReason(eventType, payload), 500),
            QueueKey = Truncate(queueKey, 320),
            OccurrenceCount = 1,
            FirstSeenAt = atUtc,
            LastSeenAt = atUtc,
            Status = BehaviorReviewStatus.Pending
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            foreach (var entry in db.ChangeTracker.Entries<BehaviorReview>())
            {
                if (entry.State == EntityState.Added)
                    entry.State = EntityState.Detached;
            }
            logger.LogDebug("Skipped duplicate behavior review {QueueKey}", queueKey);
        }
    }

    public async Task<IResult> BlockFromActivityAsync(
        Guid actorUserId,
        BlockFlaggedRequest? request,
        CancellationToken cancellationToken)
    {
        var review = await EnsurePendingReviewAsync(request, cancellationToken);
        if (review is null)
            return Results.BadRequest(new { message = "ไม่พบเว็บหรือโปรแกรมที่ต้องการบล็อก" });
        return await PenalizeReviewAsync(actorUserId, review.Id, cancellationToken);
    }

    public async Task<IResult> ClearFromActivityAsync(
        Guid actorUserId,
        BlockFlaggedRequest? request,
        CancellationToken cancellationToken)
    {
        var review = await EnsurePendingReviewAsync(request, cancellationToken);
        if (review is null)
            return Results.BadRequest(new { message = "ไม่พบรายการต้องสงสัย" });
        return await ClearReviewAsync(actorUserId, review.Id, cancellationToken);
    }

    public async Task<IResult> ListReviewsAsync(Guid? roomId, CancellationToken cancellationToken)
    {
        var query = db.BehaviorReviews.AsNoTracking()
            .Where(item => item.Status == BehaviorReviewStatus.Pending);
        if (roomId is Guid room)
            query = query.Where(item => item.RoomId == room);

        var rows = await query
            .OrderByDescending(item => item.LastSeenAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var clearedKeys = await db.BehaviorReviews.AsNoTracking()
            .Where(item => item.Status == BehaviorReviewStatus.Cleared)
            .Select(item => item.QueueKey)
            .Distinct()
            .ToListAsync(cancellationToken);
        var handledKeys = await db.BehaviorReviews.AsNoTracking()
            .Where(item => item.Status != BehaviorReviewStatus.Pending)
            .Select(item => item.QueueKey)
            .Distinct()
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            items = rows.Select(item => new
            {
                id = item.Id,
                userId = item.UserId,
                user = item.UserId is null && string.IsNullOrWhiteSpace(item.DisplayName)
                    ? null
                    : new { displayName = item.DisplayName ?? item.Username ?? "ผู้ใช้งาน", username = item.Username },
                roomId = item.RoomId,
                seatId = item.SeatId,
                roomName = item.RoomName,
                seatLabel = item.SeatLabel,
                kind = item.Kind,
                target = item.Target,
                activity = item.Activity,
                occurrenceCount = item.OccurrenceCount,
                firstSeenAt = TrackingService.FormatBangkok(item.FirstSeenAt),
                lastSeenAt = TrackingService.FormatBangkok(item.LastSeenAt),
                status = "pending",
                queueKey = item.QueueKey
            }),
            clearedKeys,
            handledKeys
        });
    }

    public async Task<IResult> ClearReviewAsync(Guid actorUserId, Guid reviewId, CancellationToken cancellationToken)
    {
        var review = await db.BehaviorReviews.SingleOrDefaultAsync(item => item.Id == reviewId, cancellationToken);
        if (review is null)
            return Results.NotFound(new { message = "ไม่พบรายการต้องสงสัย" });
        if (review.Status == BehaviorReviewStatus.Cleared)
            return Results.Ok(new { id = review.Id, status = "cleared", already = true });
        if (review.Status == BehaviorReviewStatus.Penalized)
        {
            db.BehaviorReviews.Add(CopyAsCleared(review, actorUserId));
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { id = review.Id, status = "cleared" });
        }
        if (review.Status != BehaviorReviewStatus.Pending)
            return Results.Conflict(new { message = "รายการนี้ตรวจแล้ว" });

        review.Status = BehaviorReviewStatus.Cleared;
        review.ReviewedBy = actorUserId;
        review.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "behavior.review_clear",
            "behavior_review",
            review.Id.ToString(),
            $"{review.Kind}:{review.Target}",
            cancellationToken);
        return Results.Ok(new { id = review.Id, status = "cleared" });
    }

    public async Task<IResult> PenalizeReviewAsync(Guid actorUserId, Guid reviewId, CancellationToken cancellationToken)
    {
        var review = await db.BehaviorReviews.SingleOrDefaultAsync(item => item.Id == reviewId, cancellationToken);
        if (review is null)
            return Results.NotFound(new { message = "ไม่พบรายการต้องสงสัย" });
        if (review.Status == BehaviorReviewStatus.Penalized)
        {
            await EnsureBlacklistedAsync(actorUserId, review, cancellationToken);
            return await BlockResultAsync(review, already: true, cancellationToken);
        }
        if (review.Status is not BehaviorReviewStatus.Pending and not BehaviorReviewStatus.Cleared)
            return Results.Conflict(new { message = "รายการนี้ตรวจแล้ว" });

        await EnsureBlacklistedAsync(actorUserId, review, cancellationToken);
        string? blocked = review.Kind is "website" or "program" ? review.Target : null;

        var deducted = false;
        if (review.UserId is Guid userId)
        {
            var reason = review.Kind switch
            {
                "website" => $"บล็อกเว็บไซต์ ({review.Target})",
                "program" => $"บล็อกโปรแกรม ({review.Target})",
                _ => review.Activity
            };
            await TryAddAsync(
                userId,
                FlaggedPoints,
                reason,
                SourceFlagged,
                FlagSourceKey(userId, review.Kind, review.Target),
                cancellationToken);
            deducted = await db.BehaviorPenalties.AsNoTracking()
                .AnyAsync(item => item.SourceKey == FlagSourceKey(userId, review.Kind, review.Target), cancellationToken);
        }

        review.Status = BehaviorReviewStatus.Penalized;
        review.ReviewedBy = actorUserId;
        review.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "behavior.review_penalize",
            "behavior_review",
            review.Id.ToString(),
            $"{review.Kind}:{review.Target} deducted={deducted}",
            cancellationToken);

        return await BlockResultAsync(review, already: false, cancellationToken, blocked, deducted);
    }

    public Task<int> CountPendingAsync(CancellationToken cancellationToken) =>
        db.BehaviorReviews.AsNoTracking()
            .CountAsync(item => item.Status == BehaviorReviewStatus.Pending, cancellationToken);

    public async Task SettleNoShowsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var pending = await db.Bookings
            .Where(booking => booking.Status == BookingStatus.Pending && booking.EndTime < now)
            .ToListAsync(cancellationToken);
        foreach (var booking in pending)
            booking.Status = BookingStatus.Cancelled;
        if (pending.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        var ended = await db.Bookings
            .Where(booking => booking.Status == BookingStatus.Confirmed && booking.EndTime < now)
            .Select(booking => new { booking.Id, booking.UserId, booking.RoomId, booking.StartTime, booking.EndTime })
            .ToListAsync(cancellationToken);
        if (ended.Count == 0)
            return;

        foreach (var booking in ended)
        {
            var attendeeIds = await AttendeeIdsAsync(booking.Id, booking.UserId, cancellationToken);
            var shown = await WhoShowedAsync(booking.RoomId, attendeeIds, booking.StartTime, booking.EndTime, cancellationToken);
            foreach (var userId in attendeeIds)
            {
                if (shown.Contains(userId))
                    continue;
                await TryAddAsync(
                    userId,
                    NoShowPoints,
                    "ไม่มาใช้ห้องแล็บตามวัน-เวลาที่จองไว้",
                    SourceNoShow,
                    $"noshow:{booking.Id:N}:{userId:N}",
                    cancellationToken);
            }

            var row = await db.Bookings.SingleAsync(item => item.Id == booking.Id, cancellationToken);
            row.Status = shown.Contains(booking.UserId) ? BookingStatus.Completed : BookingStatus.Expired;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<Guid>> AttendeeIdsAsync(Guid bookingId, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var ids = new HashSet<Guid> { ownerUserId };
        var members = await (
            from groupRow in db.BookingGroups.AsNoTracking()
            join member in db.GroupMembers.AsNoTracking() on groupRow.Id equals member.GroupId
            where groupRow.BookingId == bookingId
            select member.UserId
        ).ToListAsync(cancellationToken);
        foreach (var id in members)
            ids.Add(id);
        return ids.ToList();
    }

    private async Task<HashSet<Guid>> WhoShowedAsync(
        Guid roomId,
        IReadOnlyCollection<Guid> userIds,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var shown = new HashSet<Guid>();
        if (userIds.Count == 0)
            return shown;

        var fromDoor = await db.AccessLogs.AsNoTracking()
            .Where(log =>
                log.RoomId == roomId &&
                log.AuthResult == AuthResult.Granted &&
                log.CreatedAt >= startUtc &&
                log.CreatedAt < endUtc &&
                userIds.Contains(log.UserId))
            .Select(log => log.UserId)
            .ToListAsync(cancellationToken);
        foreach (var id in fromDoor)
            shown.Add(id);

        if (shown.Count == userIds.Count)
            return shown;

        var loginJson = await (
            from log in db.AgentLogs.AsNoTracking()
            join agent in db.Agents.AsNoTracking() on log.AgentId equals agent.Id
            join seat in db.Seats.AsNoTracking() on agent.SeatId equals seat.Id
            where seat.RoomId == roomId &&
                  log.EventType == AgentEventTypes.Login &&
                  log.CreatedAt >= startUtc &&
                  log.CreatedAt < endUtc
            select log.DataJson
        ).ToListAsync(cancellationToken);

        foreach (var json in loginJson)
        {
            var userId = AgentLogPayload.Parse(json).UserId;
            if (userId is Guid id && userIds.Contains(id))
                shown.Add(id);
        }

        return shown;
    }

    private async Task TryAddAsync(
        Guid userId,
        int points,
        string reason,
        string source,
        string sourceKey,
        CancellationToken cancellationToken)
    {
        if (await db.BehaviorPenalties.AnyAsync(item => item.SourceKey == sourceKey, cancellationToken))
            return;

        db.BehaviorPenalties.Add(new BehaviorPenalty
        {
            UserId = userId,
            Points = points,
            Reason = reason,
            Source = source,
            SourceKey = sourceKey,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            foreach (var entry in db.ChangeTracker.Entries<BehaviorPenalty>())
            {
                if (entry.State == EntityState.Added)
                    entry.State = EntityState.Detached;
            }
            logger.LogDebug("Skipped duplicate behavior penalty {SourceKey}", sourceKey);
        }
    }

    private async Task<BehaviorReview?> EnsurePendingReviewAsync(
        BlockFlaggedRequest? request,
        CancellationToken cancellationToken)
    {
        var eventType = request?.EventType?.Trim().ToLowerInvariant() switch
        {
            "program" => AgentEventTypes.Program,
            "website" => AgentEventTypes.Website,
            _ => AgentEventTypes.Suspicious
        };
        var payload = new AgentLogPayload
        {
            UserId = request?.UserId,
            DisplayName = request?.DisplayName,
            Username = request?.Username,
            Website = request?.Website,
            Program = request?.Program,
            Activity = request?.Activity,
            Suspicious = true
        };
        var (kind, target) = KindAndTarget(eventType, payload);
        if (kind is "website" or "program" && target is "unknown" or "")
            return null;

        var queueKey = QueueKey(request?.UserId, kind, target);
        var existing = await db.BehaviorReviews
            .Where(item => item.QueueKey == queueKey)
            .OrderByDescending(item => item.LastSeenAt)
            .ToListAsync(cancellationToken);
        var pending = existing.FirstOrDefault(item => item.Status == BehaviorReviewStatus.Pending);
        if (pending is not null)
            return pending;
        var handled = existing.FirstOrDefault(item => item.Status != BehaviorReviewStatus.Pending);
        if (handled is not null)
            return handled;

        await QueueFlaggedAsync(
            request?.UserId,
            request?.DisplayName,
            request?.Username,
            request?.RoomId,
            request?.SeatId,
            request?.RoomName,
            request?.SeatLabel,
            eventType,
            payload,
            DateTime.UtcNow,
            cancellationToken);

        return await db.BehaviorReviews.SingleOrDefaultAsync(
            item => item.Status == BehaviorReviewStatus.Pending && item.QueueKey == queueKey,
            cancellationToken);
    }

    private async Task EnsureBlacklistedAsync(
        Guid actorUserId,
        BehaviorReview review,
        CancellationToken cancellationToken)
    {
        if (review.Kind == "website")
        {
            await websites.AddAsync(
                actorUserId,
                new AddWebsiteBlacklistRequest(review.Target, "review", null),
                cancellationToken);
        }
        else if (review.Kind == "program")
        {
            await programs.AddAsync(
                actorUserId,
                new AddProgramBlacklistRequest(review.Target, "review", null),
                cancellationToken);
        }
    }

    private static BehaviorReview CopyAsCleared(BehaviorReview review, Guid actorUserId) => new()
    {
        UserId = review.UserId,
        DisplayName = review.DisplayName,
        Username = review.Username,
        RoomId = review.RoomId,
        SeatId = review.SeatId,
        RoomName = review.RoomName,
        SeatLabel = review.SeatLabel,
        Kind = review.Kind,
        Target = review.Target,
        Activity = review.Activity,
        QueueKey = review.QueueKey,
        OccurrenceCount = review.OccurrenceCount,
        FirstSeenAt = review.FirstSeenAt,
        LastSeenAt = DateTime.UtcNow,
        Status = BehaviorReviewStatus.Cleared,
        ReviewedBy = actorUserId,
        ReviewedAt = DateTime.UtcNow
    };

    private async Task<bool> AlreadyBlockedAsync(string kind, string target, CancellationToken cancellationToken)
    {
        if (kind == "website")
            return await db.WebsiteBlacklist.AnyAsync(item => item.UrlPattern == target, cancellationToken);
        if (kind == "program")
            return await db.ProgramBlacklist.AnyAsync(item => item.ProcessName == target, cancellationToken);
        return false;
    }

    private static (string Kind, string Target) KindAndTarget(string eventType, AgentLogPayload payload)
    {
        if (eventType == AgentEventTypes.Program)
        {
            var program = ProgramBlacklistService.Normalize(payload.Program) ?? payload.Program?.Trim() ?? "unknown";
            return ("program", program);
        }
        if (eventType == AgentEventTypes.Website)
        {
            var website = WebsiteBlacklistService.Normalize(payload.Website)
                ?? payload.Website?.Trim().ToLowerInvariant()
                ?? "unknown";
            return ("website", website);
        }
        if (!string.IsNullOrWhiteSpace(payload.Program))
        {
            var program = ProgramBlacklistService.Normalize(payload.Program) ?? payload.Program.Trim();
            return ("program", program);
        }
        if (!string.IsNullOrWhiteSpace(payload.Website))
        {
            var website = WebsiteBlacklistService.Normalize(payload.Website)
                ?? payload.Website.Trim().ToLowerInvariant();
            return ("website", website);
        }
        return ("suspicious", Truncate(payload.Activity ?? "กิจกรรมน่าสงสัย", 255));
    }

    private async Task<IResult> BlockResultAsync(
        BehaviorReview review,
        bool already,
        CancellationToken cancellationToken,
        string? blocked = null,
        bool? deducted = null)
    {
        if (blocked is null && review.Kind is "website" or "program")
            blocked = review.Target;

        var deductedValue = deducted ?? false;
        int? score = null;
        if (review.UserId is Guid userId)
        {
            var total = await MonthDeductedAsync(userId, CurrentScoreMonthStartUtc(), cancellationToken);
            score = Remaining(total);
            if (deducted is null)
            {
                deductedValue = await db.BehaviorPenalties.AsNoTracking()
                    .AnyAsync(item => item.SourceKey == FlagSourceKey(userId, review.Kind, review.Target), cancellationToken);
            }
        }

        return Results.Ok(new
        {
            id = review.Id,
            status = "penalized",
            already,
            blocked,
            deducted = already ? false : deductedValue,
            points = already ? 0 : deductedValue ? FlaggedPoints : 0,
            score,
            userName = review.DisplayName ?? review.Username
        });
    }

    public static DateTime CurrentScoreMonthStartUtc()
    {
        var local = BangkokTime.ToLocal(DateTime.UtcNow);
        return BangkokTime.ToUtc(new DateTime(local.Year, local.Month, 1));
    }

    public static int Remaining(int deducted) => Math.Max(0, MaxScore - deducted);

    private async Task<int> MonthDeductedAsync(
        Guid userId,
        DateTime monthStartUtc,
        CancellationToken cancellationToken) =>
        await db.BehaviorPenalties.AsNoTracking()
            .Where(item => item.UserId == userId && item.CreatedAt >= monthStartUtc)
            .SumAsync(item => (int?)item.Points, cancellationToken) ?? 0;

    private static string FlagSourceKey(Guid userId, string kind, string target) =>
        Truncate($"flag:{userId:N}:{kind}:{target.ToLowerInvariant()}", 200);

    private static string QueueKey(Guid? userId, string kind, string target) =>
        $"{userId?.ToString("N") ?? "anon"}:{kind}:{target.ToLowerInvariant()}";

    private static string FlagReason(string eventType, AgentLogPayload payload)
    {
        if (eventType == AgentEventTypes.Program)
            return $"ใช้โปรแกรม ({payload.Program ?? "โปรแกรม"})";
        if (eventType == AgentEventTypes.Website)
            return $"เข้าเว็บไซต์ ({payload.Website ?? "เว็บไซต์"})";
        return string.IsNullOrWhiteSpace(payload.Activity) ? "กิจกรรมน่าสงสัย" : payload.Activity;
    }

    private static string Truncate(string value, int max)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static string? TruncateOptional(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? value : Truncate(value, max);
}
