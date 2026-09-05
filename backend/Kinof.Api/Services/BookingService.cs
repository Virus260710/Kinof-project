using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record CreateBookingRequest(
    Guid RoomId,
    DateTime StartTime,
    DateTime EndTime,
    IReadOnlyCollection<Guid>? InviteeUserIds);

public sealed class BookingService(AppDbContext db, InvitationService invitationService)
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
            .Where(x =>
                x.Status == BookingStatus.Confirmed &&
                (x.UserId == userId ||
                 db.GroupMembers.Any(member =>
                     member.UserId == userId &&
                     db.BookingGroups.Any(group =>
                         group.Id == member.GroupId && group.BookingId == x.Id))))
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

        if (await invitationService.HasTimeConflictAsync(userId, request.StartTime, request.EndTime, cancellationToken))
            return Results.Conflict(new { message = "คุณมีการจองหรือเข้าร่วมกลุ่มในวันและเวลานี้แล้ว" });

        var inviteeIds = (request.InviteeUserIds ?? Array.Empty<Guid>())
            .Where(id => id != userId)
            .Distinct()
            .Take(4)
            .ToArray();
        var invitees = await db.Users
            .Where(x => inviteeIds.Contains(x.Id) && x.Status == UserStatus.Active)
            .ToListAsync(cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
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

        if (invitees.Count > 0)
        {
            var group = new BookingGroup { BookingId = booking.Id, OwnerUserId = userId };
            db.BookingGroups.Add(group);
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = userId });
            foreach (var invitee in invitees)
            {
                var invitation = new Invitation
                {
                    GroupId = group.Id,
                    InviterUserId = userId,
                    InviteeUserId = invitee.Id
                };
                db.Invitations.Add(invitation);
                db.Notifications.Add(new Notification
                {
                    UserId = invitee.Id,
                    InvitationId = invitation.Id,
                    Message = $"คุณได้รับคำเชิญให้เข้าร่วมกลุ่มจองห้อง {room.Name} ในช่วงเวลาที่เลือก"
                });
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);

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
