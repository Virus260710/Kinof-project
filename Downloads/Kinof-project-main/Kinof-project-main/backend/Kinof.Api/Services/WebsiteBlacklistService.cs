using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record AddWebsiteBlacklistRequest(string? Domain, string? Category, string? Reason);

public sealed class WebsiteBlacklistService(AppDbContext db, AuditLogService auditLog)
{
    private const string DefaultCategory = "manual";

    public async Task<IResult> ListAsync(CancellationToken cancellationToken)
    {
        var entries = await db.WebsiteBlacklist.AsNoTracking()
            .OrderByDescending(entry => entry.CreatedAt)
            .Select(entry => new
            {
                id = entry.Id,
                domain = entry.UrlPattern,
                category = entry.Category,
                reason = entry.Category,
                addedAt = entry.CreatedAt
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(entries);
    }

    public async Task<IResult> AddAsync(
        Guid actorUserId,
        AddWebsiteBlacklistRequest? request,
        CancellationToken cancellationToken)
    {
        var domain = Normalize(request?.Domain);
        if (string.IsNullOrWhiteSpace(domain))
            return Results.BadRequest(new { message = "กรุณากรอกโดเมนที่ต้องการบล็อก" });
        if (domain.Length > 255)
            return Results.BadRequest(new { message = "โดเมนยาวเกินไป" });
        if (await db.WebsiteBlacklist.AnyAsync(entry => entry.UrlPattern == domain, cancellationToken))
            return Results.Conflict(new { message = $"{domain} อยู่ใน Blacklist แล้ว" });

        var category = request?.Category?.Trim();
        if (string.IsNullOrWhiteSpace(category)) category = request?.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(category)) category = DefaultCategory;

        var entry = new WebsiteBlacklist
        {
            UrlPattern = domain,
            Category = category,
            CreatedBy = actorUserId
        };
        db.WebsiteBlacklist.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "tracking.blacklist_add",
            "website_blacklist",
            entry.Id.ToString(),
            domain,
            cancellationToken);

        return Results.Ok(new
        {
            id = entry.Id,
            domain = entry.UrlPattern,
            category = entry.Category,
            reason = entry.Category,
            addedAt = entry.CreatedAt
        });
    }

    public async Task<IResult> RemoveAsync(Guid actorUserId, int id, CancellationToken cancellationToken)
    {
        var entry = await db.WebsiteBlacklist.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entry is null)
            return Results.NotFound(new { message = "ไม่พบรายการใน Blacklist" });

        db.WebsiteBlacklist.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "tracking.blacklist_remove",
            "website_blacklist",
            id.ToString(),
            entry.UrlPattern,
            cancellationToken);

        return Results.Ok(new { id, domain = entry.UrlPattern });
    }

    private static string? Normalize(string? domain)
    {
        var value = domain?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(value)) return null;

        // Accept a pasted URL and keep only the host part.
        if (value.Contains("://", StringComparison.Ordinal))
            value = value[(value.IndexOf("://", StringComparison.Ordinal) + 3)..];
        var slash = value.IndexOf('/');
        if (slash >= 0) value = value[..slash];
        return value.Trim();
    }
}
