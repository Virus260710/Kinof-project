using ClosedXML.Excel;
using Kinof.Api.Data;

namespace Kinof.Api.Services;

public sealed record ImportIssue(int Row, string Field, string Message, string Level);
public sealed record ParsedScheduleRow(
    int Row,
    string SubjectCode,
    string? SubjectName,
    string Section,
    string? InstructorName,
    int DayOfWeek,
    string DayKey,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string AcademicYear,
    string Semester,
    string RoomName,
    string CompositeKey);
public sealed record ParsedEnrollmentRow(
    int Row,
    string SubjectCode,
    string Section,
    int DayOfWeek,
    TimeOnly StartTime,
    string RoomName,
    string StudentId,
    string CompositeKey);

public sealed class ScheduleImportParser
{
    private static readonly Dictionary<string, int> DayMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SUN"] = 0, ["SUNDAY"] = 0, ["0"] = 0,
        ["MON"] = 1, ["MONDAY"] = 1, ["1"] = 1,
        ["TUE"] = 2, ["TUESDAY"] = 2, ["2"] = 2,
        ["WED"] = 3, ["WEDNESDAY"] = 3, ["3"] = 3,
        ["THU"] = 4, ["THURSDAY"] = 4, ["4"] = 4,
        ["FRI"] = 5, ["FRIDAY"] = 5, ["5"] = 5,
        ["SAT"] = 6, ["SATURDAY"] = 6, ["6"] = 6
    };

    public static byte[] CreateTemplate()
    {
        using var workbook = new XLWorkbook();
        var schedules = workbook.Worksheets.Add("schedules");
        schedules.Cell(1, 1).Value = "แม่แบบตารางเรียน KINOF";
        schedules.Cell(2, 1).Value = "แถวที่ 3 คือชื่อคอลัมน์ที่ระบบอ่าน — อย่าแก้คีย์ภาษาอังกฤษ วันใช้ MON/TUE/... เวลาใช้ HH:mm";
        var scheduleHeaders = new[]
        {
            "subject_code", "subject_name", "section", "instructor_name", "day_of_week",
            "start_time", "end_time", "academic_year", "semester", "room_name"
        };
        for (var i = 0; i < scheduleHeaders.Length; i++)
            schedules.Cell(3, i + 1).Value = scheduleHeaders[i];
        schedules.Row(3).Style.Font.Bold = true;
        schedules.Columns().AdjustToContents();

        var enrollments = workbook.Worksheets.Add("enrollments");
        enrollments.Cell(1, 1).Value = "แม่แบบรายชื่อนักศึกษาในตารางเรียน KINOF";
        enrollments.Cell(2, 1).Value = "ผูกกับชีต schedules ด้วยวิชา/ตอน/วัน/เวลาเริ่ม/ห้อง — รหัสนักศึกษา 10 หลัก";
        var enrollmentHeaders = new[]
        {
            "subject_code", "section", "day_of_week", "start_time", "room_name", "student_id"
        };
        for (var i = 0; i < enrollmentHeaders.Length; i++)
            enrollments.Cell(3, i + 1).Value = enrollmentHeaders[i];
        enrollments.Row(3).Style.Font.Bold = true;
        enrollments.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static (List<ParsedScheduleRow> Schedules, List<ParsedEnrollmentRow> Enrollments, List<ImportIssue> Issues)
        Parse(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var issues = new List<ImportIssue>();
        var schedules = ParseSchedules(workbook, issues);
        var enrollments = ParseEnrollments(workbook, issues);
        return (schedules, enrollments, issues);
    }

    public static string CompositeKey(
        string subjectCode,
        string section,
        int dayOfWeek,
        TimeOnly startTime,
        string roomName) =>
        $"{subjectCode.Trim().ToUpperInvariant()}|{section.Trim().ToUpperInvariant()}|{dayOfWeek}|{startTime:HH\\:mm}|{roomName.Trim().ToLowerInvariant()}";

    private static List<ParsedScheduleRow> ParseSchedules(XLWorkbook workbook, List<ImportIssue> issues)
    {
        if (!TryGetWorksheet(workbook, "schedules", out var sheet))
        {
            issues.Add(new ImportIssue(0, "file", "ไม่พบชีต schedules", "error"));
            return [];
        }

        var headers = ReadHeaders(sheet);
        var required = new[]
        {
            "subject_code", "day_of_week", "start_time", "end_time", "academic_year", "semester", "room_name"
        };
        foreach (var key in required)
        {
            if (!headers.ContainsKey(key))
                issues.Add(new ImportIssue(3, key, $"ชีต schedules ไม่มีคอลัมน์ {key}", "error"));
        }

        var rows = new List<ParsedScheduleRow>();
        if (required.Any(key => !headers.ContainsKey(key)))
            return rows;

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 3;
        for (var row = 4; row <= lastRow; row++)
        {
            if (IsEmpty(sheet, row, headers.Values.Max()))
                continue;

            var subjectCode = Cell(sheet, row, headers, "subject_code");
            var subjectName = Cell(sheet, row, headers, "subject_name");
            var section = Cell(sheet, row, headers, "section");
            var instructor = Cell(sheet, row, headers, "instructor_name");
            var dayRaw = Cell(sheet, row, headers, "day_of_week");
            var startRaw = TimeCell(sheet, row, headers, "start_time");
            var endRaw = TimeCell(sheet, row, headers, "end_time");
            var year = Cell(sheet, row, headers, "academic_year");
            var semester = Cell(sheet, row, headers, "semester");
            var roomName = Cell(sheet, row, headers, "room_name");

            var ok = true;
            if (string.IsNullOrWhiteSpace(subjectCode))
            {
                issues.Add(new ImportIssue(row, "subject_code", "กรุณากรอกรหัสวิชา", "error"));
                ok = false;
            }
            if (string.IsNullOrWhiteSpace(section))
                section = "1";
            if (!TryParseDay(dayRaw, out var day, out var dayKey))
            {
                issues.Add(new ImportIssue(row, "day_of_week", "วันต้องเป็น MON/TUE/WED/THU/FRI/SAT/SUN", "error"));
                ok = false;
            }
            if (!ScheduleTimes.TryParse(startRaw, out var start))
            {
                issues.Add(new ImportIssue(row, "start_time", "รูปแบบเวลาเริ่มต้นไม่ถูกต้อง", "error"));
                ok = false;
            }
            if (!ScheduleTimes.TryParse(endRaw, out var end))
            {
                issues.Add(new ImportIssue(row, "end_time", "รูปแบบเวลาสิ้นสุดไม่ถูกต้อง", "error"));
                ok = false;
            }
            if (ok && end <= start)
            {
                issues.Add(new ImportIssue(row, "end_time", "เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่มต้น", "error"));
                ok = false;
            }
            if (string.IsNullOrWhiteSpace(year))
            {
                issues.Add(new ImportIssue(row, "academic_year", "กรุณากรอกปีการศึกษา", "error"));
                ok = false;
            }
            if (semester is not "1" and not "2")
            {
                issues.Add(new ImportIssue(row, "semester", "เทอมต้องเป็น 1 หรือ 2", "error"));
                ok = false;
            }
            if (string.IsNullOrWhiteSpace(roomName))
            {
                issues.Add(new ImportIssue(row, "room_name", "กรุณากรอกชื่อห้อง", "error"));
                ok = false;
            }

            if (!ok)
                continue;

            rows.Add(new ParsedScheduleRow(
                row,
                subjectCode.Trim(),
                string.IsNullOrWhiteSpace(subjectName) ? null : subjectName.Trim(),
                section.Trim(),
                string.IsNullOrWhiteSpace(instructor) ? null : instructor.Trim(),
                day,
                dayKey,
                start,
                end,
                year.Trim(),
                semester,
                roomName.Trim(),
                CompositeKey(subjectCode, section, day, start, roomName)));
        }

        return rows;
    }

    private static List<ParsedEnrollmentRow> ParseEnrollments(XLWorkbook workbook, List<ImportIssue> issues)
    {
        if (!TryGetWorksheet(workbook, "enrollments", out var sheet))
        {
            issues.Add(new ImportIssue(0, "file", "ไม่พบชีต enrollments", "error"));
            return [];
        }

        var headers = ReadHeaders(sheet);
        var required = new[] { "subject_code", "day_of_week", "start_time", "room_name", "student_id" };
        foreach (var key in required)
        {
            if (!headers.ContainsKey(key))
                issues.Add(new ImportIssue(3, key, $"ชีต enrollments ไม่มีคอลัมน์ {key}", "error"));
        }
        if (required.Any(key => !headers.ContainsKey(key)))
            return [];

        var rows = new List<ParsedEnrollmentRow>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 3;
        for (var row = 4; row <= lastRow; row++)
        {
            if (IsEmpty(sheet, row, headers.Values.Max()))
                continue;

            var subjectCode = Cell(sheet, row, headers, "subject_code");
            var section = Cell(sheet, row, headers, "section");
            var dayRaw = Cell(sheet, row, headers, "day_of_week");
            var startRaw = TimeCell(sheet, row, headers, "start_time");
            var roomName = Cell(sheet, row, headers, "room_name");
            var studentId = Cell(sheet, row, headers, "student_id");
            var ok = true;
            if (string.IsNullOrWhiteSpace(subjectCode))
            {
                issues.Add(new ImportIssue(row, "subject_code", "กรุณากรอกรหัสวิชา", "error"));
                ok = false;
            }
            if (string.IsNullOrWhiteSpace(section))
                section = "1";
            if (!TryParseDay(dayRaw, out var day, out _))
            {
                issues.Add(new ImportIssue(row, "day_of_week", "วันต้องเป็น MON/TUE/WED/THU/FRI/SAT/SUN", "error"));
                ok = false;
            }
            if (!ScheduleTimes.TryParse(startRaw, out var start))
            {
                issues.Add(new ImportIssue(row, "start_time", "รูปแบบเวลาเริ่มต้นไม่ถูกต้อง", "error"));
                ok = false;
            }
            if (string.IsNullOrWhiteSpace(roomName))
            {
                issues.Add(new ImportIssue(row, "room_name", "กรุณากรอกชื่อห้อง", "error"));
                ok = false;
            }
            if (!StudentIds.IsValid(studentId))
            {
                issues.Add(new ImportIssue(row, "student_id", "รหัสนักศึกษาต้องเป็นตัวเลข 10 หลัก", "error"));
                ok = false;
            }
            if (!ok)
                continue;

            rows.Add(new ParsedEnrollmentRow(
                row,
                subjectCode.Trim(),
                section.Trim(),
                day,
                start,
                roomName.Trim(),
                studentId.Trim(),
                CompositeKey(subjectCode, section, day, start, roomName)));
        }

        return rows;
    }

    private static bool TryGetWorksheet(XLWorkbook workbook, string name, out IXLWorksheet sheet)
    {
        sheet = workbook.Worksheets.FirstOrDefault(x =>
            x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))!;
        return sheet is not null;
    }

    private static Dictionary<string, int> ReadHeaders(IXLWorksheet sheet)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = sheet.Row(3).LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (var col = 1; col <= lastCol; col++)
        {
            var key = sheet.Cell(3, col).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(key) && !headers.ContainsKey(key))
                headers[key] = col;
        }
        return headers;
    }

    private static string Cell(IXLWorksheet sheet, int row, Dictionary<string, int> headers, string key) =>
        headers.TryGetValue(key, out var col) ? sheet.Cell(row, col).GetString().Trim() : "";

    private static string TimeCell(IXLWorksheet sheet, int row, Dictionary<string, int> headers, string key)
    {
        if (!headers.TryGetValue(key, out var col))
            return "";
        var cell = sheet.Cell(row, col);
        if (cell.DataType == XLDataType.DateTime)
            return TimeOnly.FromDateTime(cell.GetDateTime()).ToString("HH:mm");
        if (cell.DataType == XLDataType.TimeSpan)
            return TimeOnly.FromTimeSpan(cell.GetTimeSpan()).ToString("HH:mm");
        if (cell.DataType == XLDataType.Number && cell.GetDouble() is >= 0 and < 1)
            return TimeOnly.FromTimeSpan(TimeSpan.FromDays(cell.GetDouble())).ToString("HH:mm");
        return cell.GetString().Trim();
    }

    private static bool TryParseDay(string raw, out int day, out string dayKey)
    {
        day = 0;
        dayKey = raw.Trim().ToUpperInvariant();
        return DayMap.TryGetValue(dayKey, out day);
    }

    private static bool IsEmpty(IXLWorksheet sheet, int row, int lastCol)
    {
        for (var col = 1; col <= lastCol; col++)
        {
            if (!string.IsNullOrWhiteSpace(sheet.Cell(row, col).GetString()))
                return false;
        }
        return true;
    }
}
