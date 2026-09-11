using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record EntryDecision(
    bool Granted,
    string? Message,
    Guid? UserId,
    string? DisplayName,
    string? Username,
    Guid? RoomId,
    string? RoomName,
    string? Building,
    Guid? SeatId,
    int? SeatNumber,
    string? SeatLabel,
    string? ComputerName)
{
    public static EntryDecision Deny(string message) =>
        new(false, message, null, null, null, null, null, null, null, null, null, null);

    public static EntryDecision Denied(string message, User user, Room room) =>
        new(false, message, user.Id, TrackingService.ShortDisplayName(user), user.Username,
            room.Id, room.Name, room.Building, null, null, null, null);

    public static EntryDecision Grant(User user, Room room, Seat seat) =>
        new(true, null, user.Id, TrackingService.ShortDisplayName(user), user.Username,
            room.Id, room.Name, room.Building, seat.Id, seat.SeatNumber,
            TrackingService.SeatLabel(seat.SeatNumber), seat.ComputerName);
}

/// <summary>
/// Shared entry pipeline for every Kiosk authentication path: entitlement check
/// (schedule or booking) → seat assignment → access log. Phase 3B calls this after an
/// entry OTP verifies; the Phase 3C face path will call the same method with
/// <see cref="AuthMethod.Face"/>.
/// </summary>
public sealed class EntryService(AppDbContext db)
{
    public async Task<EntryDecision> AuthorizeAndAssignSeatAsync(
        Guid userId,
        Guid roomId,
        AuthMethod authMethod,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
            return EntryDecision.Deny("ไม่พบบัญชีผู้ใช้นี้");

        var room = await db.Rooms.SingleOrDefaultAsync(x => x.Id == roomId, cancellationToken);
        if (room is null)
            return EntryDecision.Deny("ไม่พบห้องแล็บนี้");

        if (room.Status != RoomStatus.Open)
            return await DenyAsync(user, room, authMethod, "ห้องนี้ปิดหรืออยู่ระหว่างปรับปรุง", cancellationToken);

        if (user.Status != UserStatus.Active)
            return await DenyAsync(user, room, authMethod, "บัญชีนี้ถูกระงับการใช้งาน", cancellationToken);

        var nowUtc = DateTime.UtcNow;
        var entitled =
            await HasActiveScheduleAsync(user.Id, room.Id, nowUtc, cancellationToken) ||
            await HasActiveBookingAsync(user.Id, room.Id, nowUtc, cancellationToken);
        if (!entitled)
            return await DenyAsync(user, room, authMethod, "ไม่มีตารางเรียนหรือการจองห้องนี้ในช่วงเวลานี้", cancellationToken);

        var seat = await db.Seats
            .Where(x => x.RoomId == room.Id && x.Status == SeatStatus.Available)
            .OrderBy(x => x.SeatNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (seat is null)
            return await DenyAsync(user, room, authMethod, "ไม่มีที่นั่งว่างในห้องนี้", cancellationToken);

        seat.Status = SeatStatus.Occupied;
        db.AccessLogs.Add(new AccessLog
        {
            UserId = user.Id,
            RoomId = room.Id,
            SeatId = seat.Id,
            AuthMethod = authMethod,
            AuthResult = AuthResult.Granted,
            CreatedAt = nowUtc
        });
        await AddAgentLoginLogAsync(user, seat, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return EntryDecision.Grant(user, room, seat);
    }

    /// <summary>กลุ่ม 1 — คาบที่กำลังเรียนอยู่ในห้องนี้ตามเวลากรุงเทพ</summary>
    private async Task<bool> HasActiveScheduleAsync(
        Guid userId,
        Guid roomId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var local = BangkokTime.ToLocal(nowUtc);
        var day = (int)local.DayOfWeek;
        var timeOfDay = TimeOnly.FromDateTime(local);

        // TimeOnly comparisons are filtered in memory to match how ScheduleService
        // handles SQLite's text-based time storage.
        var slots = await db.ScheduleEnrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.UserId == userId)
            .Join(
                db.Schedules.AsNoTracking().Where(schedule =>
                    schedule.IsActive &&
                    schedule.RoomId == roomId &&
                    schedule.DayOfWeek == day),
                enrollment => enrollment.ScheduleId,
                schedule => schedule.Id,
                (_, schedule) => new { schedule.StartTime, schedule.EndTime })
            .ToListAsync(cancellationToken);

        return slots.Any(slot => slot.StartTime <= timeOfDay && slot.EndTime > timeOfDay);
    }

    /// <summary>กลุ่ม 2/3 — เจ้าของการจอง หรือสมาชิกกลุ่มที่ตอบรับคำเชิญแล้ว</summary>
    private Task<bool> HasActiveBookingAsync(
        Guid userId,
        Guid roomId,
        DateTime nowUtc,
        CancellationToken cancellationToken) =>
        db.Bookings
            .AsNoTracking()
            .AnyAsync(booking =>
                booking.RoomId == roomId &&
                booking.Status == BookingStatus.Confirmed &&
                booking.StartTime <= nowUtc &&
                booking.EndTime > nowUtc &&
                (booking.UserId == userId ||
                 db.GroupMembers.Any(member =>
                     member.UserId == userId &&
                     db.BookingGroups.Any(group =>
                         group.Id == member.GroupId && group.BookingId == booking.Id))),
                cancellationToken);

    private async Task<EntryDecision> DenyAsync(
        User user,
        Room room,
        AuthMethod authMethod,
        string reason,
        CancellationToken cancellationToken)
    {
        db.AccessLogs.Add(new AccessLog
        {
            UserId = user.Id,
            RoomId = room.Id,
            AuthMethod = authMethod,
            AuthResult = AuthResult.Denied,
            DenyReason = reason
        });
        await db.SaveChangesAsync(cancellationToken);
        return EntryDecision.Denied(reason, user, room);
    }

    /// <summary>
    /// Seats that already have a tracking agent get a login event so the admin monitor
    /// shows the session immediately, before the agent itself reports in.
    /// </summary>
    private async Task AddAgentLoginLogAsync(User user, Seat seat, CancellationToken cancellationToken)
    {
        var agentId = await db.Agents
            .AsNoTracking()
            .Where(x => x.SeatId == seat.Id)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (agentId is not Guid id)
            return;

        db.AgentLogs.Add(new AgentLog
        {
            AgentId = id,
            EventType = AgentEventTypes.Login,
            DataJson = new AgentLogPayload
            {
                UserId = user.Id,
                Username = user.Username,
                DisplayName = TrackingService.ShortDisplayName(user),
                UserType = TrackingService.UserTypeLabel(user.UserType),
                Source = "kiosk"
            }.ToJson()
        });
    }
}
