using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record UpsertRoomRequest(string Name, string? Building, int Capacity, string? Status);

public sealed class RoomAdminService(AppDbContext db, AuditLogService auditLog)
{
    public async Task<IResult> ListAsync(CancellationToken cancellationToken)
    {
        var rooms = await db.Rooms.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                id = x.Id,
                name = x.Name,
                building = x.Building,
                capacity = x.Capacity,
                status = x.Status.ToString().ToLowerInvariant(),
                createdAt = x.CreatedAt,
                seatCount = db.Seats.Count(seat => seat.RoomId == x.Id)
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(rooms);
    }

    public async Task<IResult> CreateAsync(
        Guid actorUserId,
        UpsertRoomRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "กรุณากรอกชื่อห้อง" });
        if (request.Capacity < 1 || request.Capacity > 200)
            return Results.BadRequest(new { message = "ความจุต้องอยู่ระหว่าง 1–200" });
        if (await db.Rooms.AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken))
            return Results.Conflict(new { message = "มีห้องชื่อนี้อยู่แล้ว" });

        var room = new Room
        {
            Name = name,
            Building = string.IsNullOrWhiteSpace(request.Building) ? null : request.Building.Trim(),
            Capacity = request.Capacity,
            Status = ParseStatus(request.Status)
        };
        db.Rooms.Add(room);
        AddSeats(room, 1, request.Capacity);
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(actorUserId, "room.create", "room", room.Id.ToString(), room.Name, cancellationToken);
        return Results.Ok(ToResponse(room));
    }

    public async Task<IResult> UpdateAsync(
        Guid actorUserId,
        Guid roomId,
        UpsertRoomRequest request,
        CancellationToken cancellationToken)
    {
        var room = await db.Rooms.SingleOrDefaultAsync(x => x.Id == roomId, cancellationToken);
        if (room is null)
            return Results.NotFound(new { message = "ไม่พบห้องแล็บ" });

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { message = "กรุณากรอกชื่อห้อง" });
        if (request.Capacity < 1 || request.Capacity > 200)
            return Results.BadRequest(new { message = "ความจุต้องอยู่ระหว่าง 1–200" });
        if (await db.Rooms.AnyAsync(
                x => x.Id != roomId && x.Name.ToLower() == name.ToLower(),
                cancellationToken))
        {
            return Results.Conflict(new { message = "มีห้องชื่อนี้อยู่แล้ว" });
        }

        var oldCapacity = room.Capacity;
        room.Name = name;
        room.Building = string.IsNullOrWhiteSpace(request.Building) ? null : request.Building.Trim();
        room.Status = ParseStatus(request.Status);

        if (request.Capacity != oldCapacity)
        {
            var seats = await db.Seats
                .Where(x => x.RoomId == room.Id)
                .OrderBy(x => x.SeatNumber)
                .ToListAsync(cancellationToken);
            if (request.Capacity > oldCapacity)
            {
                var nextNumber = seats.Count == 0 ? 1 : seats.Max(x => x.SeatNumber) + 1;
                AddSeats(room, nextNumber, request.Capacity - seats.Count);
            }
            else
            {
                var toRemove = seats
                    .OrderByDescending(x => x.SeatNumber)
                    .Take(oldCapacity - request.Capacity)
                    .ToList();
                if (toRemove.Any(x => x.Status == SeatStatus.Occupied))
                    return Results.Conflict(new { message = "ลดความจุไม่ได้ เพราะที่นั่งที่จะตัดกำลังถูกใช้งาน" });
                db.Seats.RemoveRange(toRemove);
            }

            room.Capacity = request.Capacity;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            request.Capacity != oldCapacity ? "room.capacity_change" : "room.update",
            "room",
            room.Id.ToString(),
            request.Capacity != oldCapacity ? $"{room.Name}: {oldCapacity} → {request.Capacity}" : room.Name,
            cancellationToken);
        return Results.Ok(ToResponse(room));
    }

    public async Task<IResult> DeleteAsync(
        Guid actorUserId,
        Guid roomId,
        CancellationToken cancellationToken)
    {
        var room = await db.Rooms.SingleOrDefaultAsync(x => x.Id == roomId, cancellationToken);
        if (room is null)
            return Results.NotFound(new { message = "ไม่พบห้องแล็บ" });

        var now = DateTime.UtcNow;
        var hasActiveSchedule = await db.Schedules.AnyAsync(
            x => x.RoomId == roomId && x.IsActive,
            cancellationToken);
        var hasFutureBooking = await db.Bookings.AnyAsync(
            x => x.RoomId == roomId &&
                 x.Status == BookingStatus.Confirmed &&
                 x.EndTime > now,
            cancellationToken);

        if (hasActiveSchedule || hasFutureBooking)
        {
            room.Status = RoomStatus.Closed;
            await db.SaveChangesAsync(cancellationToken);
            await auditLog.WriteAsync(
                actorUserId,
                "room.update",
                "room",
                room.Id.ToString(),
                $"{room.Name} closed (still has schedule or booking)",
                cancellationToken);
            return Results.Ok(new
            {
                action = "closed",
                message = "ยังมีตารางเรียนหรือการจองอยู่ จึงเปลี่ยนสถานะเป็นปิดแทนการลบ",
                room = ToResponse(room)
            });
        }

        db.Rooms.Remove(room);
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(actorUserId, "room.delete", "room", roomId.ToString(), room.Name, cancellationToken);
        return Results.Ok(new { action = "deleted", id = roomId });
    }

    private void AddSeats(Room room, int startNumber, int count)
    {
        var prefix = SanitizeComputerPrefix(room.Name);
        db.Seats.AddRange(Enumerable.Range(startNumber, count).Select(number => new Seat
        {
            RoomId = room.Id,
            SeatNumber = number,
            ComputerName = $"{prefix}-{number:00}"
        }));
    }

    private static RoomStatus ParseStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "closed" => RoomStatus.Closed,
        "maintenance" => RoomStatus.Maintenance,
        _ => RoomStatus.Open
    };

    private static string SanitizeComputerPrefix(string name)
    {
        var cleaned = new string(name.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "PC-LAB" : $"PC-{cleaned.ToUpperInvariant()}";
    }

    private static object ToResponse(Room room) => new
    {
        id = room.Id,
        name = room.Name,
        building = room.Building,
        capacity = room.Capacity,
        status = room.Status.ToString().ToLowerInvariant()
    };
}
