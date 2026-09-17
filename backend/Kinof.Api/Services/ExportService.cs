using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record ExportRequest(
    string? Report,
    string? Format,
    string? RoomId,
    string? StartDate,
    string? EndDate);

public sealed class ExportService(AppDbContext db, AuditLogService auditLog)
{
    public const int MaxRows = 50_000;
    public const int MaxRangeDays = 366;

    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string CsvContentType = "text/csv; charset=utf-8";

    private sealed class ActivityJoin
    {
        public required AgentLog Log { get; init; }
        public required Seat Seat { get; init; }
        public required Room Room { get; init; }
    }

    private sealed record ExportRow(
        DateTime CreatedAt,
        string EventType,
        string? DataJson,
        string RoomName,
        int SeatNumber,
        string? ComputerName);

    private sealed record ReportSpec(
        string Key,
        string Label,
        string FileSlug,
        string SheetName,
        string[] Headers,
        bool FlaggedOnly);

    private static readonly ReportSpec[] Reports =
    [
        new("log", "ประวัติเข้า-ออกระบบ", "login-logout", "เข้า-ออกระบบ",
            ["เวลา", "ห้อง", "เครื่อง", "ผู้ใช้", "ชื่อผู้ใช้", "ประเภทผู้ใช้", "กิจกรรม", "แหล่งที่มา"],
            false),
        new("prog", "โปรแกรมที่ถูกใช้งาน", "programs", "โปรแกรม",
            ["เวลา", "ห้อง", "เครื่อง", "ผู้ใช้", "ชื่อผู้ใช้", "โปรแกรม", "ระยะเวลา (นาที)", "กิจกรรม", "สถานะ"],
            false),
        new("web", "เว็บไซต์ที่เข้าชม", "websites", "เว็บไซต์",
            ["เวลา", "ห้อง", "เครื่อง", "ผู้ใช้", "ชื่อผู้ใช้", "เว็บไซต์", "กิจกรรม", "สถานะ"],
            false),
        new("flag", "กิจกรรมน่าสงสัย", "flagged", "น่าสงสัย",
            ["เวลา", "ห้อง", "เครื่อง", "ผู้ใช้", "ชื่อผู้ใช้", "ประเภทเหตุการณ์", "กิจกรรม", "โปรแกรม", "เว็บไซต์", "สถานะ"],
            true)
    ];

    public async Task<IResult> ExportAsync(
        Guid actorUserId,
        ExportRequest? request,
        CancellationToken cancellationToken)
    {
        var spec = Reports.FirstOrDefault(item =>
            string.Equals(item.Key, request?.Report?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (spec is null)
            return Results.BadRequest(new { message = "ประเภทรายงานต้องเป็น log, prog, web หรือ flag" });

        var format = ParseFormat(request?.Format);
        if (format is null)
            return Results.BadRequest(new { message = "รูปแบบไฟล์ต้องเป็น Excel หรือ CSV" });

        if (!TryParseBangkokRange(request?.StartDate, request?.EndDate, out var startUtc, out var endUtc, out var rangeError))
            return rangeError!;

        Guid? roomId = null;
        string roomLabel = "ทุกห้อง";
        var roomRaw = request?.RoomId?.Trim();
        if (!string.IsNullOrWhiteSpace(roomRaw) &&
            !string.Equals(roomRaw, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(roomRaw, out var parsedRoom))
                return Results.BadRequest(new { message = "รหัสห้องไม่ถูกต้อง" });

            var room = await db.Rooms.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == parsedRoom, cancellationToken);
            if (room is null)
                return Results.NotFound(new { message = "ไม่พบห้องแล็บ" });

            roomId = room.Id;
            roomLabel = room.Name;
        }

        var rows = await QueryRowsAsync(spec, roomId, startUtc, endUtc, cancellationToken);
        var startDate = DateOnly.FromDateTime(BangkokTime.ToLocal(startUtc));
        var endDate = DateOnly.FromDateTime(BangkokTime.ToLocal(endUtc.AddTicks(-1)));
        var startStamp = startDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var endStamp = endDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var startLabel = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endLabel = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var fileName = $"kinof-{spec.FileSlug}-{startStamp}-{endStamp}.{format}";
        var bytes = format == "csv"
            ? BuildCsv(spec, rows)
            : BuildExcel(spec, rows);

        await auditLog.WriteAsync(
            actorUserId,
            "tracking.export",
            "export",
            spec.Key,
            $"{spec.Label} · {roomLabel} · {startLabel}–{endLabel} · {format.ToUpperInvariant()} · {rows.Count} แถว",
            cancellationToken);

        return Results.File(bytes, format == "csv" ? CsvContentType : ExcelContentType, fileName);
    }

    private async Task<List<ExportRow>> QueryRowsAsync(
        ReportSpec spec,
        Guid? roomId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var query =
            from log in db.AgentLogs.AsNoTracking()
            join agent in db.Agents.AsNoTracking() on log.AgentId equals agent.Id
            join seat in db.Seats.AsNoTracking() on agent.SeatId equals seat.Id
            join room in db.Rooms.AsNoTracking() on seat.RoomId equals room.Id
            where log.CreatedAt >= startUtc && log.CreatedAt < endUtc
            select new ActivityJoin { Log = log, Seat = seat, Room = room };

        if (roomId is Guid filterRoom)
            query = query.Where(row => row.Room.Id == filterRoom);

        query = spec.Key switch
        {
            "log" => query.Where(row =>
                row.Log.EventType == AgentEventTypes.Login || row.Log.EventType == AgentEventTypes.Logout),
            "prog" => query.Where(row => row.Log.EventType == AgentEventTypes.Program),
            "web" => query.Where(row => row.Log.EventType == AgentEventTypes.Website),
            "flag" => query.Where(row =>
                row.Log.EventType == AgentEventTypes.Suspicious ||
                row.Log.EventType == AgentEventTypes.Website ||
                row.Log.EventType == AgentEventTypes.Program),
            _ => query.Where(_ => false)
        };

        var fetched = await query
            .OrderBy(row => row.Log.CreatedAt)
            .ThenBy(row => row.Log.Id)
            .Take(MaxRows)
            .Select(row => new ExportRow(
                row.Log.CreatedAt,
                row.Log.EventType,
                row.Log.DataJson,
                row.Room.Name,
                row.Seat.SeatNumber,
                row.Seat.ComputerName))
            .ToListAsync(cancellationToken);

        if (!spec.FlaggedOnly)
            return fetched;

        return fetched
            .Where(row =>
            {
                if (row.EventType == AgentEventTypes.Suspicious)
                    return true;
                return AgentLogPayload.Parse(row.DataJson).Suspicious;
            })
            .ToList();
    }

    private static byte[] BuildExcel(ReportSpec spec, IReadOnlyList<ExportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(spec.SheetName);
        for (var column = 0; column < spec.Headers.Length; column++)
            sheet.Cell(1, column + 1).Value = spec.Headers[column];
        sheet.Row(1).Style.Font.Bold = true;

        for (var index = 0; index < rows.Count; index++)
        {
            var values = ToCells(spec, rows[index]);
            for (var column = 0; column < values.Length; column++)
                sheet.Cell(index + 2, column + 1).Value = values[column];
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildCsv(ReportSpec spec, IReadOnlyList<ExportRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", spec.Headers.Select(CsvEscape)));
        foreach (var row in rows)
            builder.AppendLine(string.Join(",", ToCells(spec, row).Select(CsvEscape)));

        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(builder.ToString());
        var bytes = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);
        return bytes;
    }

    private static string[] ToCells(ReportSpec spec, ExportRow row)
    {
        var payload = AgentLogPayload.Parse(row.DataJson);
        var time = FormatTime(row.CreatedAt);
        var room = row.RoomName;
        var machine = MachineLabel(row.SeatNumber, row.ComputerName);
        var user = payload.DisplayName ?? payload.Username ?? "";
        var username = payload.Username ?? "";
        var userType = payload.UserType ?? "";
        var activity = TrackingService.DescribeActivity(row.EventType, payload);
        var status = payload.Suspicious || row.EventType == AgentEventTypes.Suspicious ? "น่าสงสัย" : "ปกติ";

        return spec.Key switch
        {
            "log" => [time, room, machine, user, username, userType, activity, payload.Source ?? ""],
            "prog" =>
            [
                time, room, machine, user, username,
                payload.Program ?? "",
                payload.DurationMinutes?.ToString(CultureInfo.InvariantCulture) ?? "",
                activity, status
            ],
            "web" => [time, room, machine, user, username, payload.Website ?? "", activity, status],
            "flag" =>
            [
                time, room, machine, user, username,
                EventTypeLabel(row.EventType),
                activity,
                payload.Program ?? "",
                payload.Website ?? "",
                status
            ],
            _ => []
        };
    }

    private static string MachineLabel(int seatNumber, string? computerName)
    {
        var label = TrackingService.SeatLabel(seatNumber);
        return string.IsNullOrWhiteSpace(computerName) ? label : $"{label} ({computerName})";
    }

    private static string FormatTime(DateTime utc) =>
        BangkokTime.ToLocal(DateTime.SpecifyKind(utc, DateTimeKind.Utc))
            .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private static string EventTypeLabel(string eventType) => eventType switch
    {
        AgentEventTypes.Login => "เข้าสู่ระบบ",
        AgentEventTypes.Logout => "ออกจากระบบ",
        AgentEventTypes.Program => "โปรแกรม",
        AgentEventTypes.Website => "เว็บไซต์",
        AgentEventTypes.Suspicious => "น่าสงสัย",
        _ => eventType
    };

    private static string? ParseFormat(string? format) => format?.Trim().ToLowerInvariant() switch
    {
        "excel" or "xlsx" => "xlsx",
        "csv" => "csv",
        _ => null
    };

    private static bool TryParseBangkokRange(
        string? startDate,
        string? endDate,
        out DateTime startUtc,
        out DateTime endUtc,
        out IResult? error)
    {
        startUtc = default;
        endUtc = default;
        error = null;

        if (!DateOnly.TryParseExact(startDate?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
        {
            error = Results.BadRequest(new { message = "วันที่เริ่มต้นต้องเป็นรูปแบบ YYYY-MM-DD" });
            return false;
        }

        if (!DateOnly.TryParseExact(endDate?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            error = Results.BadRequest(new { message = "วันที่สิ้นสุดต้องเป็นรูปแบบ YYYY-MM-DD" });
            return false;
        }

        if (end < start)
        {
            error = Results.BadRequest(new { message = "วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มต้น" });
            return false;
        }

        var days = end.DayNumber - start.DayNumber + 1;
        if (days > MaxRangeDays)
        {
            error = Results.BadRequest(new { message = $"ช่วงวันที่ต้องไม่เกิน {MaxRangeDays} วัน" });
            return false;
        }

        startUtc = BangkokTime.ToUtc(start.ToDateTime(TimeOnly.MinValue));
        endUtc = BangkokTime.ToUtc(end.AddDays(1).ToDateTime(TimeOnly.MinValue));
        return true;
    }

    private static string CsvEscape(string value)
    {
        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
            return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
