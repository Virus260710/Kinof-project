using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record AddProgramAllowlistRequest(string? ProcessName, string? DisplayName, string? Category);

public sealed class ProgramAllowlistService(AppDbContext db, AuditLogService auditLog)
{
    private const string DefaultCategory = "lab";

    public async Task<IResult> ListAsync(CancellationToken cancellationToken)
    {
        var entries = await db.ProgramAllowlist.AsNoTracking()
            .OrderBy(entry => entry.DisplayName ?? entry.ProcessName)
            .Select(entry => new
            {
                id = entry.Id,
                processName = entry.ProcessName,
                displayName = entry.DisplayName,
                category = entry.Category,
                addedAt = entry.CreatedAt
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(entries);
    }

    public async Task<IResult> AddAsync(
        Guid actorUserId,
        AddProgramAllowlistRequest? request,
        CancellationToken cancellationToken)
    {
        var processName = ProgramBlacklistService.Normalize(request?.ProcessName);
        if (string.IsNullOrWhiteSpace(processName))
            return Results.BadRequest(new { message = "กรุณากรอกชื่อโปรแกรมที่อนุญาต เช่น chrome.exe" });
        if (processName.Length > 255)
            return Results.BadRequest(new { message = "ชื่อโปรแกรมยาวเกินไป" });
        if (await db.ProgramAllowlist.AnyAsync(entry => entry.ProcessName == processName, cancellationToken))
            return Results.Conflict(new { message = $"{processName} อยู่ในรายการอนุญาตแล้ว" });

        var category = string.IsNullOrWhiteSpace(request?.Category) ? DefaultCategory : request.Category.Trim();
        var displayName = string.IsNullOrWhiteSpace(request?.DisplayName) ? null : request.DisplayName.Trim();
        if (displayName is { Length: > 120 }) displayName = displayName[..120];

        var entry = new ProgramAllowlist
        {
            ProcessName = processName,
            DisplayName = displayName,
            Category = category,
            CreatedBy = actorUserId
        };
        db.ProgramAllowlist.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "tracking.program_allowlist_add",
            "program_allowlist",
            entry.Id.ToString(),
            processName,
            cancellationToken);

        return Results.Ok(new
        {
            id = entry.Id,
            processName = entry.ProcessName,
            displayName = entry.DisplayName,
            category = entry.Category,
            addedAt = entry.CreatedAt
        });
    }

    public async Task<IResult> RemoveAsync(Guid actorUserId, int id, CancellationToken cancellationToken)
    {
        var entry = await db.ProgramAllowlist.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entry is null)
            return Results.NotFound(new { message = "ไม่พบรายการในรายการอนุญาตโปรแกรม" });

        db.ProgramAllowlist.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "tracking.program_allowlist_remove",
            "program_allowlist",
            id.ToString(),
            entry.ProcessName,
            cancellationToken);

        return Results.Ok(new { id, processName = entry.ProcessName });
    }
}
