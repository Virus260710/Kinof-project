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
    string? Building)
{
    public const string GrantedMessage =
        "ผ่านการตรวจสิทธิ์เข้าห้องแล้ว กรุณาเดินเข้าไปเลือกเครื่องที่ว่าง แล้วเข้าสู่ระบบบัญชี KINOF ที่เครื่องนั้น — จุดสแกนประตูกับการล็อกอินเครื่องเป็นคนละด่าน ไม่ต้องใช้เลขเครื่องที่ระบบสุ่มให้";

    public static EntryDecision Deny(string message) =>
        new(false, message, null, null, null, null, null, null);

    public static EntryDecision Denied(string message, User user, Room room) =>
        new(false, message, user.Id, TrackingService.ShortDisplayName(user), user.Username,
            room.Id, room.Name, room.Building);

    public static EntryDecision Grant(User user, Room room) =>
        new(true, GrantedMessage, user.Id, TrackingService.ShortDisplayName(user), user.Username,
            room.Id, room.Name, room.Building);
}

/// <summary>
/// Kiosk door check only: schedule or booking. Does not pick a seat, does not mark
/// Occupied, and does not bind the person to a computer — machine login is a later step.
/// </summary>
public sealed class EntryService(AppDbContext db)
{
    public async Task<EntryDecision> AuthorizeRoomEntryAsync(
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
        {
            var who = TrackingService.ShortDisplayName(user);
            return await DenyAsync(
                user,
                room,
                authMethod,
                $"ระบบจดจำว่าเป็น {who} แต่บัญชีนี้ยังไม่มีตารางเรียนหรือการจอง {room.Name} ในช่วงเวลานี้",
                cancellationToken);
        }

        db.AccessLogs.Add(new AccessLog
        {
            UserId = user.Id,
            RoomId = room.Id,
            AuthMethod = authMethod,
            AuthResult = AuthResult.Granted,
            CreatedAt = nowUtc
        });
        await db.SaveChangesAsync(cancellationToken);

        return EntryDecision.Grant(user, room);
    }

    public async Task<bool> HasDoorEntitlementAsync(
        Guid userId,
        Guid roomId,
        DateTime nowUtc,
        CancellationToken cancellationToken) =>
        await HasActiveScheduleAsync(userId, roomId, nowUtc, cancellationToken) ||
        await HasActiveBookingAsync(userId, roomId, nowUtc, cancellationToken);

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
}
