using System.Security.Claims;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record UserLookupResponse(Guid Id, string Name, string Email);
public sealed record AcceptInvitationRequest(bool Confirm);

public sealed class InvitationService(AppDbContext db)
{
    public async Task<IResult> SearchUsersAsync(
        Guid currentUserId,
        string? query,
        CancellationToken cancellationToken)
    {
        var search = query?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(search) || search.Length < 2)
            return Results.Ok(Array.Empty<UserLookupResponse>());

        var users = await db.Users
            .AsNoTracking()
            .Where(x => x.Id != currentUserId && x.Status == UserStatus.Active &&
                (x.Email.ToLower().Contains(search) ||
                 x.Username.ToLower().Contains(search) ||
                 x.FirstName.ToLower().Contains(search) ||
                 x.LastName.ToLower().Contains(search) ||
                 (x.FirstName + " " + x.LastName).ToLower().Contains(search)))
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Take(10)
            .Select(x => new UserLookupResponse(x.Id, x.FirstName + " " + x.LastName, x.Email))
            .ToListAsync(cancellationToken);

        return Results.Ok(users);
    }

    public async Task<IResult> GetMyInvitationsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var invitations = await db.Invitations
            .AsNoTracking()
            .Where(x => x.InviteeUserId == userId && x.Status == InvitationStatus.Pending)
            .Join(db.BookingGroups, invitation => invitation.GroupId, group => group.Id, (invitation, group) => new { invitation, group })
            .Join(db.Bookings, item => item.group.BookingId, booking => booking.Id, (item, booking) => new { item.invitation, booking })
            .Join(db.Rooms, item => item.booking.RoomId, room => room.Id, (item, room) => new { item.invitation, item.booking, room })
            .Join(db.Users, item => item.invitation.InviterUserId, user => user.Id, (item, user) => new
            {
                id = item.invitation.Id,
                inviter = user.FirstName + " " + user.LastName,
                inviterEmail = user.Email,
                startTime = item.booking.StartTime,
                endTime = item.booking.EndTime,
                room = item.room.Name,
                status = item.invitation.Status.ToString().ToLowerInvariant(),
                createdAt = item.invitation.CreatedAt
            })
            .OrderByDescending(x => x.createdAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(invitations);
    }

    public async Task<IResult> AcceptAsync(
        Guid userId,
        Guid invitationId,
        AcceptInvitationRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Confirm)
            return Results.BadRequest(new { message = "กรุณายืนยันการเข้าร่วมกลุ่มอีกครั้ง" });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var invitation = await db.Invitations.SingleOrDefaultAsync(
            x => x.Id == invitationId && x.InviteeUserId == userId,
            cancellationToken);
        if (invitation is null)
            return Results.NotFound(new { message = "ไม่พบคำเชิญนี้" });
        if (invitation.Status != InvitationStatus.Pending)
            return Results.Conflict(new { message = "คำเชิญนี้ถูกดำเนินการไปแล้ว" });

        var booking = await db.BookingGroups
            .Where(x => x.Id == invitation.GroupId)
            .Join(db.Bookings, group => group.BookingId, booking => booking.Id, (_, booking) => booking)
            .SingleOrDefaultAsync(cancellationToken);
        if (booking is null || booking.Status != BookingStatus.Confirmed)
            return Results.Conflict(new { message = "การจองของกลุ่มนี้ไม่สามารถเข้าร่วมได้แล้ว" });

        var room = await db.Rooms
            .AsNoTracking()
            .SingleAsync(x => x.Id == booking.RoomId, cancellationToken);

        var hasConflict = await HasTimeConflictAsync(userId, booking.StartTime, booking.EndTime, cancellationToken);
        if (hasConflict)
            return Results.Conflict(new { message = "คุณมีการจองหรือเข้าร่วมกลุ่มในวันและเวลานี้แล้ว" });

        invitation.Status = InvitationStatus.Accepted;
        invitation.RespondedAt = DateTime.UtcNow;
        db.GroupMembers.Add(new GroupMember { GroupId = invitation.GroupId, UserId = userId });
        db.Notifications.Add(new Notification
        {
            UserId = invitation.InviterUserId,
            InvitationId = invitation.Id,
            Message = "สมาชิกตอบรับคำเชิญเข้าร่วมกลุ่มของคุณแล้ว"
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Results.Ok(new
        {
            id = booking.Id,
            roomId = room.Id,
            room = room.Name,
            building = room.Building,
            startTime = booking.StartTime,
            endTime = booking.EndTime,
            status = "joined"
        });
    }

    public async Task<IResult> DeclineAsync(
        Guid userId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var invitation = await db.Invitations.SingleOrDefaultAsync(
            x => x.Id == invitationId && x.InviteeUserId == userId,
            cancellationToken);
        if (invitation is null)
            return Results.NotFound(new { message = "ไม่พบคำเชิญนี้" });
        if (invitation.Status != InvitationStatus.Pending)
            return Results.Conflict(new { message = "คำเชิญนี้ถูกดำเนินการไปแล้ว" });

        invitation.Status = InvitationStatus.Declined;
        invitation.RespondedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { ok = true });
    }

    internal async Task<bool> HasTimeConflictAsync(
        Guid userId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken)
    {
        var directConflict = await db.Bookings.AnyAsync(x =>
            x.UserId == userId &&
            x.Status == BookingStatus.Confirmed &&
            x.StartTime < endTime && x.EndTime > startTime,
            cancellationToken);
        if (directConflict)
            return true;

        return await db.GroupMembers
            .Where(member => member.UserId == userId)
            .Join(db.BookingGroups, member => member.GroupId, group => group.Id, (_, group) => group.BookingId)
            .Join(db.Bookings, bookingId => bookingId, booking => booking.Id, (_, booking) => booking)
            .AnyAsync(x =>
                x.Status == BookingStatus.Confirmed &&
                x.StartTime < endTime && x.EndTime > startTime,
                cancellationToken);
    }
}
