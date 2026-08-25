using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record CreateBookingRequest(
    Guid RoomId,
    DateTime StartTime,
    DateTime EndTime);

public sealed class BookingService(AppDbContext db)
{
    public async Task<IResult> GetRoomsAsync(CancellationToken cancellationToken)
    {
        var rooms = await db.Rooms
            .AsNoTracking()
            .Where(x => x.Status == RoomStatus.Open)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                id = x.Id,
                name = x.Name,
                building = x.Building,
                capacity = x.Capacity,
                status = x.Status.ToString().ToLowerInvariant()
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(rooms);
    }

    public async Task<IResult> GetAvailableRoomsAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken)
    {
        if (endTime <= startTime)
            return Results.BadRequest(new { message = "เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่มต้น" });

        var bookedRoomIds = await db.Bookings
            .AsNoTracking()
            .Where(x =>
                x.Status == BookingStatus.Confirmed &&
                x.StartTime < endTime &&
                x.EndTime > startTime)
            .Select(x => x.RoomId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var rooms = await db.Rooms
            .AsNoTracking()
            .Where(x => x.Status == RoomStatus.Open && !bookedRoomIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                id = x.Id,
                name = x.Name,
                building = x.Building,
                capacity = x.Capacity
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(rooms);
    }

    public async Task<IResult> GetMyBookingsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var bookings = await db.Bookings
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == BookingStatus.Confirmed)
            .OrderByDescending(x => x.StartTime)
            .Join(
                db.Rooms.AsNoTracking(),
                booking => booking.RoomId,
                room => room.Id,
                (booking, room) => new
                {
                    id = booking.Id,
                    roomId = room.Id,
                    room = room.Name,
                    building = room.Building,
                    startTime = booking.StartTime,
                    endTime = booking.EndTime,
                    status = booking.Status.ToString().ToLowerInvariant(),
                    createdAt = booking.CreatedAt
                })
            .ToListAsync(cancellationToken);

        return Results.Ok(bookings);
    }

    public async Task<IResult> CreateBookingAsync(
        Guid userId,
        CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EndTime <= request.StartTime)
            return Results.BadRequest(new { message = "เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่มต้น" });
        if (request.StartTime <= DateTime.UtcNow)
            return Results.BadRequest(new { message = "ไม่สามารถจองย้อนหลังได้" });

        var room = await db.Rooms.SingleOrDefaultAsync(
            x => x.Id == request.RoomId && x.Status == RoomStatus.Open,
            cancellationToken);
        if (room is null)
            return Results.NotFound(new { message = "ไม่พบห้องที่เลือก" });

        var hasConflict = await db.Bookings.AnyAsync(
            x =>
                x.RoomId == request.RoomId &&
                x.Status == BookingStatus.Confirmed &&
                x.StartTime < request.EndTime &&
                x.EndTime > request.StartTime,
            cancellationToken);
        if (hasConflict)
            return Results.Conflict(new { message = "ห้องนี้ถูกจองในช่วงเวลานี้แล้ว" });

        var booking = new Booking
        {
            UserId = userId,
            RoomId = request.RoomId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = BookingStatus.Confirmed
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            id = booking.Id,
            roomId = room.Id,
            room = room.Name,
            building = room.Building,
            startTime = booking.StartTime,
            endTime = booking.EndTime,
            status = booking.Status.ToString().ToLowerInvariant()
        });
    }
}
