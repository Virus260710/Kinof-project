using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record CreateKioskDeviceRequest(Guid RoomId, string? Label);

/// <summary>
/// Admin provisioning for door Kiosk machines. Each device is bound to a room
/// (not a seat). The plaintext API key is returned only at creation time.
/// </summary>
public sealed class KioskDeviceService(AppDbContext db, AuditLogService auditLog)
{
    public async Task<IResult> ListAsync(Guid? roomId, CancellationToken cancellationToken)
    {
        var query = from device in db.KioskDevices.AsNoTracking()
                    join room in db.Rooms.AsNoTracking() on device.RoomId equals room.Id
                    select new { device, room };
        if (roomId is Guid filterRoomId)
            query = query.Where(row => row.device.RoomId == filterRoomId);

        var rows = await query
            .OrderBy(row => row.room.Name)
            .ThenBy(row => row.device.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(rows.Select(row => new
        {
            id = row.device.Id,
            roomId = row.device.RoomId,
            roomName = row.room.Name,
            label = row.device.Label,
            revoked = row.device.RevokedAt is not null,
            revokedAt = row.device.RevokedAt,
            lastSeenAt = row.device.LastSeenAt,
            createdAt = row.device.CreatedAt
        }).ToArray());
    }

    public async Task<IResult> CreateAsync(
        Guid actorUserId,
        CreateKioskDeviceRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.RoomId == Guid.Empty)
            return Results.BadRequest(new { message = "ต้องระบุ roomId" });

        var room = await db.Rooms.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.RoomId, cancellationToken);
        if (room is null)
            return Results.NotFound(new { message = "ไม่พบห้องแล็บ" });

        var label = string.IsNullOrWhiteSpace(request.Label) ? "เครื่องประตู" : request.Label.Trim();
        if (label.Length > 100)
            return Results.BadRequest(new { message = "ชื่ออุปกรณ์ยาวเกินไป" });

        var device = new KioskDevice
        {
            RoomId = room.Id,
            ApiKey = TrackingService.GenerateApiKey(),
            Label = label
        };
        db.KioskDevices.Add(device);
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "kiosk.device_create",
            "kiosk_device",
            device.Id.ToString(),
            $"{room.Name} · {label}",
            cancellationToken);

        return Results.Ok(new
        {
            id = device.Id,
            roomId = device.RoomId,
            roomName = room.Name,
            label = device.Label,
            apiKey = device.ApiKey,
            createdAt = device.CreatedAt
        });
    }

    public async Task<IResult> RevokeAsync(
        Guid actorUserId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var device = await db.KioskDevices
            .SingleOrDefaultAsync(x => x.Id == deviceId, cancellationToken);
        if (device is null)
            return Results.NotFound(new { message = "ไม่พบอุปกรณ์ Kiosk นี้" });
        if (device.RevokedAt is not null)
            return Results.Conflict(new { message = "เพิกถอนคีย์นี้ไปแล้ว" });

        device.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "kiosk.device_revoke",
            "kiosk_device",
            device.Id.ToString(),
            device.Label,
            cancellationToken);

        return Results.Ok(new
        {
            id = device.Id,
            revoked = true,
            revokedAt = device.RevokedAt
        });
    }
}
