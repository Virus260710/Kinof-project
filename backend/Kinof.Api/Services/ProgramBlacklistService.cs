using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record AddProgramBlacklistRequest(string? ProcessName, string? Category, string? Reason);

public sealed class ProgramBlacklistService(AppDbContext db, AuditLogService auditLog)
{
    private const string DefaultCategory = "manual";

    public async Task<IResult> ListAsync(CancellationToken cancellationToken)
    {
        var entries = await db.ProgramBlacklist.AsNoTracking()
            .OrderByDescending(entry => entry.CreatedAt)
            .Select(entry => new
            {
                id = entry.Id,
                processName = entry.ProcessName,
                category = entry.Category,
                reason = entry.Category,
                addedAt = entry.CreatedAt
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(entries);
    }

    public async Task<IResult> AddAsync(
        Guid actorUserId,
        AddProgramBlacklistRequest? request,
        CancellationToken cancellationToken)
    {
        var processName = Normalize(request?.ProcessName);
        if (string.IsNullOrWhiteSpace(processName))
            return Results.BadRequest(new { message = "กรุณากรอกชื่อโปรแกรมที่ต้องการบล็อก เช่น discord.exe" });
        if (processName.Length > 255)
            return Results.BadRequest(new { message = "ชื่อโปรแกรมยาวเกินไป" });
        if (await db.ProgramBlacklist.AnyAsync(entry => entry.ProcessName == processName, cancellationToken))
            return Results.Conflict(new { message = $"{processName} อยู่ในรายการบล็อกแล้ว" });

        var category = request?.Category?.Trim();
        if (string.IsNullOrWhiteSpace(category)) category = request?.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(category)) category = DefaultCategory;

        var entry = new ProgramBlacklist
        {
            ProcessName = processName,
            Category = category,
            CreatedBy = actorUserId
        };
        db.ProgramBlacklist.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "tracking.program_blacklist_add",
            "program_blacklist",
            entry.Id.ToString(),
            processName,
            cancellationToken);

        return Results.Ok(new
        {
            id = entry.Id,
            processName = entry.ProcessName,
            category = entry.Category,
            reason = entry.Category,
            addedAt = entry.CreatedAt
        });
    }

    public async Task<IResult> RemoveAsync(Guid actorUserId, int id, CancellationToken cancellationToken)
    {
        var entry = await db.ProgramBlacklist.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entry is null)
            return Results.NotFound(new { message = "ไม่พบรายการในรายการบล็อกโปรแกรม" });

        db.ProgramBlacklist.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "tracking.program_blacklist_remove",
            "program_blacklist",
            id.ToString(),
            entry.ProcessName,
            cancellationToken);

        return Results.Ok(new { id, processName = entry.ProcessName });
    }

    public static string? Normalize(string? processName)
    {
        var value = processName?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(value)) return null;

        value = value.Replace('/', '\\');
        var slash = value.LastIndexOf('\\');
        if (slash >= 0) value = value[(slash + 1)..];
        if (!value.EndsWith(".exe", StringComparison.Ordinal) && !value.Contains('.'))
            value += ".exe";
        return value.Trim();
    }
}
