using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record CreateBookingRequest(
    Guid RoomId,
    DateTime StartTime,
    DateTime EndTime,
    IReadOnlyCollection<Guid>? InviteeUserIds);

public sealed class BookingService(
    AppDbContext db,
    InvitationService invitationService,
    ScheduleService scheduleService,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<BookingService> logger)
{
    private static readonly BookingStatus[] RoomBlockingStatuses =
        [BookingStatus.Confirmed, BookingStatus.Pending];

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
                RoomBlockingStatuses.Contains(x.Status) &&
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

        var available = new List<object>();
        foreach (var room in rooms)
        {
            if (await scheduleService.RoomHasScheduleOverlapAsync(room.id, startTime, endTime, cancellationToken))
                continue;
            available.Add(room);
        }

        return Results.Ok(available);
    }

    public async Task<IResult> GetMyBookingsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var bookings = await db.Bookings
            .AsNoTracking()
            .Where(x =>
                (x.Status == BookingStatus.Confirmed ||
                 (x.Status == BookingStatus.Pending && x.UserId == userId)) &&
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

    public async Task<IResult> GetBookingGroupStatusAsync(
        Guid userId,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var booking = await db.Bookings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == bookingId && x.UserId == userId, cancellationToken);
        if (booking is null)
            return Results.NotFound(new { message = "ไม่พบการจองนี้" });

        var group = await db.BookingGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.BookingId == bookingId, cancellationToken);
        if (group is null)
        {
            return Results.Ok(new
            {
                bookingId,
                status = booking.Status.ToString().ToLowerInvariant(),
                members = Array.Empty<object>(),
                allAccepted = true,
                hasDeclined = false,
                canConfirm = booking.Status == BookingStatus.Pending
            });
        }

        var members = await db.Invitations
            .AsNoTracking()
            .Where(x => x.GroupId == group.Id)
            .Join(
                db.Users.AsNoTracking(),
                invitation => invitation.InviteeUserId,
                user => user.Id,
                (invitation, user) => new
                {
                    id = user.Id,
                    name = user.FirstName + " " + user.LastName,
                    email = user.Email,
                    status = invitation.Status.ToString().ToLowerInvariant()
                })
            .ToListAsync(cancellationToken);

        var hasDeclined = members.Any(x => x.status == "declined");
        var allAccepted = members.Count > 0 && members.All(x => x.status == "accepted");

        return Results.Ok(new
        {
            bookingId,
            status = booking.Status.ToString().ToLowerInvariant(),
            members,
            allAccepted,
            hasDeclined,
            canConfirm = booking.Status == BookingStatus.Pending && allAccepted && !hasDeclined
        });
    }

    public async Task<IResult> ConfirmBookingAsync(
        Guid userId,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.SingleOrDefaultAsync(
            x => x.Id == bookingId && x.UserId == userId,
            cancellationToken);
        if (booking is null)
            return Results.NotFound(new { message = "ไม่พบการจองนี้" });
        if (booking.Status != BookingStatus.Pending)
            return Results.Conflict(new { message = "การจองนี้ยืนยันแล้วหรือไม่สามารถยืนยันได้" });

        var group = await db.BookingGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.BookingId == bookingId, cancellationToken);
        if (group is not null)
        {
            var invitations = await db.Invitations
                .Where(x => x.GroupId == group.Id)
                .ToListAsync(cancellationToken);
            if (invitations.Any(x => x.Status == InvitationStatus.Declined))
                return Results.Conflict(new { message = "มีสมาชิกปฏิเสธคำเชิญ กรุณายกเลิกและจองใหม่" });
            if (invitations.Any(x => x.Status == InvitationStatus.Pending))
                return Results.Conflict(new { message = "รอให้สมาชิกทุกคนตอบรับคำเชิญก่อนยืนยันการจอง" });
        }

        booking.Status = BookingStatus.Confirmed;
        await db.SaveChangesAsync(cancellationToken);

        var room = await db.Rooms.AsNoTracking().SingleAsync(x => x.Id == booking.RoomId, cancellationToken);
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

    public async Task<IResult> CancelPendingBookingAsync(
        Guid userId,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.SingleOrDefaultAsync(
            x => x.Id == bookingId && x.UserId == userId,
            cancellationToken);
        if (booking is null)
            return Results.NotFound(new { message = "ไม่พบการจองนี้" });
        if (booking.Status != BookingStatus.Pending)
            return Results.Conflict(new { message = "ยกเลิกได้เฉพาะการจองที่รอยืนยันเท่านั้น" });

        booking.Status = BookingStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { ok = true });
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
                RoomBlockingStatuses.Contains(x.Status) &&
                x.StartTime < request.EndTime &&
                x.EndTime > request.StartTime,
            cancellationToken);
        if (hasConflict)
            return Results.Conflict(new { message = "ห้องนี้ถูกจองในช่วงเวลานี้แล้ว" });

        if (await scheduleService.RoomHasScheduleOverlapAsync(request.RoomId, request.StartTime, request.EndTime, cancellationToken))
            return Results.Conflict(new { message = "ห้องนี้มีตารางเรียนในช่วงเวลานี้" });

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

        if (inviteeIds.Length > 0 && invitees.Count == 0)
            return Results.BadRequest(new { message = "ไม่พบผู้ใช้ที่เชิญในระบบ" });

        var hasInvitees = invitees.Count > 0;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var booking = new Booking
        {
            UserId = userId,
            RoomId = request.RoomId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = hasInvitees ? BookingStatus.Pending : BookingStatus.Confirmed
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);

        var invitationsCreated = 0;
        if (hasInvitees)
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
            invitationsCreated = invitees.Count;
        }

        if (inviteeIds.Length > invitees.Count)
        {
            logger.LogWarning(
                "Booking {BookingId}: {Requested} invitee ids requested but only {Created} active users matched",
                booking.Id,
                inviteeIds.Length,
                invitees.Count);
        }

        await transaction.CommitAsync(cancellationToken);

        if (invitationsCreated > 0)
            await SendGroupInvitationEmailsAsync(userId, invitees, room.Name, booking, cancellationToken);

        return Results.Ok(new
        {
            id = booking.Id,
            roomId = room.Id,
            room = room.Name,
            building = room.Building,
            startTime = booking.StartTime,
            endTime = booking.EndTime,
            status = booking.Status.ToString().ToLowerInvariant(),
            invitationsRequested = inviteeIds.Length,
            invitationsCreated,
            invitationsSkipped = Math.Max(0, inviteeIds.Length - invitationsCreated),
            awaitingMemberConfirmation = hasInvitees
        });
    }

    private async Task SendGroupInvitationEmailsAsync(
        Guid userId,
        IReadOnlyList<User> invitees,
        string roomName,
        Booking booking,
        CancellationToken cancellationToken)
    {
        var inviter = await db.Users
            .AsNoTracking()
            .SingleAsync(x => x.Id == userId, cancellationToken);
        var inviterName = $"{inviter.FirstName} {inviter.LastName}".Trim();
        var appLink = (configuration["Frontend:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');

        foreach (var invitee in invitees)
        {
            try
            {
                var delivery = await emailSender.SendGroupInvitationEmailAsync(
                    invitee.Email,
                    invitee.FirstName,
                    inviterName,
                    roomName,
                    booking.StartTime,
                    booking.EndTime,
                    appLink,
                    cancellationToken);
                logger.LogInformation(
                    "Group invitation email for {InviteeEmail} delivery mode: {Mode}",
                    invitee.Email,
                    delivery.Mode);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Failed to send group invitation email to {InviteeEmail}",
                    invitee.Email);
            }
        }
    }
}
