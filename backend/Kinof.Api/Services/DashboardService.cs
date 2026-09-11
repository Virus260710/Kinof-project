using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record DashboardRoomResponse(
    Guid Id,
    string Name,
    string Status,
    int SeatCount,
    int TodayBookingCount,
    bool HasClassNow);

public sealed record DashboardResponse(
    int TodayBookingCount,
    int PendingProblemReports,
    int RoomsOpen,
    int RoomsTotal,
    int SeatsOpen,
    int SeatsTotal,
    IReadOnlyCollection<DashboardRoomResponse> Rooms);

public sealed class DashboardService(AppDbContext db)
{
    public async Task<IResult> GetAsync(CancellationToken cancellationToken)
    {
        var nowLocal = BangkokTime.ToLocal(DateTime.UtcNow);
        var todayStartLocal = nowLocal.Date;
        var tomorrowStartLocal = todayStartLocal.AddDays(1);
        var todayStartUtc = BangkokTime.ToUtc(todayStartLocal);
        var tomorrowStartUtc = BangkokTime.ToUtc(tomorrowStartLocal);
        var dayOfWeek = (int)nowLocal.DayOfWeek;
        var timeNow = TimeOnly.FromDateTime(nowLocal);

        var rooms = await db.Rooms
            .AsNoTracking()
            .OrderBy(room => room.Name)
            .ToListAsync(cancellationToken);

        var seatCounts = await db.Seats
            .AsNoTracking()
            .GroupBy(seat => seat.RoomId)
            .Select(group => new { RoomId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.RoomId, row => row.Count, cancellationToken);

        var bookingCounts = await db.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.Status == BookingStatus.Confirmed &&
                booking.StartTime < tomorrowStartUtc &&
                booking.EndTime > todayStartUtc)
            .GroupBy(booking => booking.RoomId)
            .Select(group => new { RoomId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.RoomId, row => row.Count, cancellationToken);

        var roomIdsWithClassNow = await db.Schedules
            .AsNoTracking()
            .Where(schedule =>
                schedule.IsActive &&
                schedule.DayOfWeek == dayOfWeek &&
                schedule.StartTime <= timeNow &&
                schedule.EndTime > timeNow)
            .Select(schedule => schedule.RoomId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var roomsWithClassNow = roomIdsWithClassNow.ToHashSet();

        var pendingProblemReports = await db.ProblemReports
            .AsNoTracking()
            .CountAsync(report => report.Status != ProblemReportStatus.Resolved, cancellationToken);

        var responseRooms = rooms.Select(room => new DashboardRoomResponse(
            room.Id,
            room.Name,
            room.Status.ToString().ToLowerInvariant(),
            seatCounts.GetValueOrDefault(room.Id),
            bookingCounts.GetValueOrDefault(room.Id),
            roomsWithClassNow.Contains(room.Id)))
            .ToArray();

        return Results.Ok(new DashboardResponse(
            bookingCounts.Values.Sum(),
            pendingProblemReports,
            rooms.Count(room => room.Status == RoomStatus.Open),
            rooms.Count,
            rooms.Where(room => room.Status == RoomStatus.Open)
                .Sum(room => seatCounts.GetValueOrDefault(room.Id)),
            seatCounts.Values.Sum(),
            responseRooms));
    }
}
