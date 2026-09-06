using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed class AuditLogService(AppDbContext db)
{
    public const int RetentionDays = 90;

    public async Task WriteAsync(
        Guid actorUserId,
        string action,
        string targetType,
        string? targetId,
        string? detail,
        CancellationToken cancellationToken)
    {
        await PurgeExpiredAsync(cancellationToken);
        db.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Detail = detail,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IResult> ListAsync(
        string? action,
        DateTime? from,
        DateTime? to,
        int page,
        int limit,
        CancellationToken cancellationToken)
    {
        await PurgeExpiredAsync(cancellationToken);
        page = Math.Max(1, page);
        limit = Math.Clamp(limit <= 0 ? 50 : limit, 1, 200);
        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

        var query = db.AdminAuditLogs.AsNoTracking()
            .Where(x => x.CreatedAt >= cutoff);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(x => x.Action == action);
        if (from is not null)
            query = query.Where(x => x.CreatedAt >= from.Value);
        if (to is not null)
            query = query.Where(x => x.CreatedAt <= to.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Join(
                db.Users.AsNoTracking(),
                log => log.ActorUserId,
                user => user.Id,
                (log, user) => new
                {
                    id = log.Id,
                    action = log.Action,
                    targetType = log.TargetType,
                    targetId = log.TargetId,
                    detail = log.Detail,
                    createdAt = log.CreatedAt,
                    actor = new
                    {
                        id = user.Id,
                        username = user.Username,
                        name = user.FirstName + " " + user.LastName,
                        userType = user.UserType.ToString().ToLowerInvariant()
                    }
                })
            .ToListAsync(cancellationToken);

        return Results.Ok(new { total, page, limit, items });
    }

    private async Task PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
        await db.AdminAuditLogs
            .Where(x => x.CreatedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
