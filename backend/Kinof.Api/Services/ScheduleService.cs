using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record UpsertScheduleRequest(
    Guid RoomId,
    string CourseCode,
    string? CourseName,
    string? Section,
    string? InstructorName,
    int DayOfWeek,
    string StartTime,
    string EndTime,
    string AcademicYear,
    string Semester);

public sealed record AddEnrollmentRequest(string StudentId);

public sealed class ScheduleService(AppDbContext db, AuditLogService auditLog)
{
    private static readonly string[] ThaiDays =
        ["วันอาทิตย์", "วันจันทร์", "วันอังคาร", "วันพุธ", "วันพฤหัสบดี", "วันศุกร์", "วันเสาร์"];

    public async Task<IResult> ListAsync(bool? active, CancellationToken cancellationToken)
    {
        var query = db.Schedules.AsNoTracking().AsQueryable();
        if (active is not null)
            query = query.Where(x => x.IsActive == active);

        var rows = await query
            .Join(db.Rooms.AsNoTracking(), schedule => schedule.RoomId, room => room.Id, (schedule, room) => new { schedule, room })
            .OrderBy(x => x.schedule.DayOfWeek)
            .ThenBy(x => x.schedule.StartTime)
            .ThenBy(x => x.schedule.CourseCode)
            .Select(x => new
            {
                id = x.schedule.Id,
                roomId = x.room.Id,
                room = x.room.Name,
                courseCode = x.schedule.CourseCode,
                courseName = x.schedule.CourseName,
                section = x.schedule.Section,
                instructorName = x.schedule.InstructorName,
                dayOfWeek = x.schedule.DayOfWeek,
                startTime = x.schedule.StartTime,
                endTime = x.schedule.EndTime,
                academicYear = x.schedule.AcademicYear,
                semester = x.schedule.Semester,
                isActive = x.schedule.IsActive,
                enrolledCount = db.ScheduleEnrollments.Count(e => e.ScheduleId == x.schedule.Id),
                pendingCount = db.ScheduleEnrollmentPendings.Count(e => e.ScheduleId == x.schedule.Id),
                capacity = x.room.Capacity
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(rows.Select(x => new
        {
            x.id,
            x.roomId,
            x.room,
            x.courseCode,
            x.courseName,
            x.section,
            x.instructorName,
            x.dayOfWeek,
            dayLabel = DayLabel(x.dayOfWeek),
            startTime = x.startTime.ToString("HH:mm"),
            endTime = x.endTime.ToString("HH:mm"),
            x.academicYear,
            x.semester,
            term = FormatTerm(x.semester, x.academicYear),
            x.isActive,
            x.enrolledCount,
            x.pendingCount,
            x.capacity
        }));
    }

    public async Task<IResult> GetAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        var schedule = await db.Schedules.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == scheduleId, cancellationToken);
        if (schedule is null)
            return Results.NotFound(new { message = "ไม่พบตารางเรียน" });

        var room = await db.Rooms.AsNoTracking()
            .SingleAsync(x => x.Id == schedule.RoomId, cancellationToken);
        var enrolled = await db.ScheduleEnrollments.AsNoTracking()
            .Where(x => x.ScheduleId == scheduleId)
            .Join(db.Users.AsNoTracking(), e => e.UserId, u => u.Id, (e, u) => new
            {
                id = e.Id,
                type = "enrolled",
                userId = (Guid?)u.Id,
                studentId = (string?)u.StudentId,
                name = (string?)(u.FirstName + " " + u.LastName),
                email = (string?)u.Email
            })
            .ToListAsync(cancellationToken);
        var pending = await db.ScheduleEnrollmentPendings.AsNoTracking()
            .Where(x => x.ScheduleId == scheduleId)
            .Select(x => new
            {
                id = x.Id,
                type = "pending",
                userId = (Guid?)null,
                studentId = x.StudentId,
                name = (string?)null,
                email = (string?)null
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            id = schedule.Id,
            roomId = room.Id,
            room = room.Name,
            courseCode = schedule.CourseCode,
            courseName = schedule.CourseName,
            section = schedule.Section,
            instructorName = schedule.InstructorName,
            dayOfWeek = schedule.DayOfWeek,
            dayLabel = DayLabel(schedule.DayOfWeek),
            startTime = schedule.StartTime.ToString("HH:mm"),
            endTime = schedule.EndTime.ToString("HH:mm"),
            academicYear = schedule.AcademicYear,
            semester = schedule.Semester,
            term = FormatTerm(schedule.Semester, schedule.AcademicYear),
            isActive = schedule.IsActive,
            capacity = room.Capacity,
            students = enrolled.Concat(pending).ToList()
        });
    }

    public async Task<IResult> CreateAsync(
        Guid actorUserId,
        UpsertScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var parsed = ValidateUpsert(request);
        if (parsed.Error is not null)
            return parsed.Error;

        var room = await db.Rooms.SingleOrDefaultAsync(x => x.Id == request.RoomId, cancellationToken);
        if (room is null)
            return Results.NotFound(new { message = "ไม่พบห้องที่เลือก" });
        if (await HasScheduleOverlapAsync(
                request.RoomId,
                parsed.Day,
                parsed.Start,
                parsed.End,
                null,
                cancellationToken))
        {
            return Results.Conflict(new { message = "ตารางเรียนห้องนี้ทับช่วงเวลาที่มีอยู่แล้ว" });
        }

        var schedule = new Schedule
        {
            RoomId = request.RoomId,
            CourseCode = parsed.CourseCode,
            CourseName = parsed.CourseName,
            Section = parsed.Section,
            InstructorName = parsed.InstructorName,
            DayOfWeek = parsed.Day,
            StartTime = parsed.Start,
            EndTime = parsed.End,
            AcademicYear = parsed.AcademicYear,
            Semester = parsed.Semester,
            IsActive = true
        };
        db.Schedules.Add(schedule);
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "schedule.create",
            "schedule",
            schedule.Id.ToString(),
            $"{schedule.CourseCode} {FormatTerm(schedule.Semester, schedule.AcademicYear)}",
            cancellationToken);
        return await GetAsync(schedule.Id, cancellationToken);
    }

    public async Task<IResult> UpdateAsync(
        Guid actorUserId,
        Guid scheduleId,
        UpsertScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var schedule = await db.Schedules.SingleOrDefaultAsync(x => x.Id == scheduleId, cancellationToken);
        if (schedule is null)
            return Results.NotFound(new { message = "ไม่พบตารางเรียน" });

        var parsed = ValidateUpsert(request);
        if (parsed.Error is not null)
            return parsed.Error;
        if (!await db.Rooms.AnyAsync(x => x.Id == request.RoomId, cancellationToken))
            return Results.NotFound(new { message = "ไม่พบห้องที่เลือก" });
        if (await HasScheduleOverlapAsync(
                request.RoomId,
                parsed.Day,
                parsed.Start,
                parsed.End,
                scheduleId,
                cancellationToken))
        {
            return Results.Conflict(new { message = "ตารางเรียนห้องนี้ทับช่วงเวลาที่มีอยู่แล้ว" });
        }

        schedule.RoomId = request.RoomId;
        schedule.CourseCode = parsed.CourseCode;
        schedule.CourseName = parsed.CourseName;
        schedule.Section = parsed.Section;
        schedule.InstructorName = parsed.InstructorName;
        schedule.DayOfWeek = parsed.Day;
        schedule.StartTime = parsed.Start;
        schedule.EndTime = parsed.End;
        schedule.AcademicYear = parsed.AcademicYear;
        schedule.Semester = parsed.Semester;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(actorUserId, "schedule.update", "schedule", schedule.Id.ToString(), schedule.CourseCode, cancellationToken);
        return await GetAsync(scheduleId, cancellationToken);
    }

    public async Task<IResult> SoftDeleteAsync(
        Guid actorUserId,
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        var schedule = await db.Schedules.SingleOrDefaultAsync(x => x.Id == scheduleId, cancellationToken);
        if (schedule is null)
            return Results.NotFound(new { message = "ไม่พบตารางเรียน" });

        schedule.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(actorUserId, "schedule.delete", "schedule", schedule.Id.ToString(), schedule.CourseCode, cancellationToken);
        return Results.Ok(new { ok = true, isActive = false });
    }

    public async Task<IResult> AddStudentAsync(
        Guid actorUserId,
        Guid scheduleId,
        AddEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var schedule = await db.Schedules.SingleOrDefaultAsync(x => x.Id == scheduleId, cancellationToken);
        if (schedule is null)
            return Results.NotFound(new { message = "ไม่พบตารางเรียน" });
        if (!schedule.IsActive)
            return Results.BadRequest(new { message = "ตารางเรียนนี้ถูกปิดแล้ว" });

        var studentId = StudentIds.Normalize(request.StudentId);
        if (!StudentIds.IsValid(studentId))
            return Results.BadRequest(new { message = "รหัสนักศึกษาต้องเป็นตัวเลข 10 หลัก" });

        var room = await db.Rooms.SingleAsync(x => x.Id == schedule.RoomId, cancellationToken);
        var enrolled = await db.ScheduleEnrollments.CountAsync(x => x.ScheduleId == scheduleId, cancellationToken);
        var pending = await db.ScheduleEnrollmentPendings.CountAsync(x => x.ScheduleId == scheduleId, cancellationToken);
        if (enrolled + pending >= room.Capacity)
            return Results.Conflict(new { message = "จำนวนนักศึกษาเต็มความจุห้องแล้ว" });

        var user = await db.Users.SingleOrDefaultAsync(
            x => x.StudentId == studentId && x.UserType == UserType.Student,
            cancellationToken);
        if (user is not null)
        {
            if (await db.ScheduleEnrollments.AnyAsync(
                    x => x.ScheduleId == scheduleId && x.UserId == user.Id,
                    cancellationToken))
            {
                return Results.Conflict(new { message = "นักศึกษานี้อยู่ในคาบนี้แล้ว" });
            }

            db.ScheduleEnrollments.Add(new ScheduleEnrollment { ScheduleId = scheduleId, UserId = user.Id });
        }
        else
        {
            if (await db.ScheduleEnrollmentPendings.AnyAsync(
                    x => x.ScheduleId == scheduleId && x.StudentId == studentId,
                    cancellationToken))
            {
                return Results.Conflict(new { message = "รหัสนักศึกษานี้อยู่ในรายการรอลงทะเบียนแล้ว" });
            }

            db.ScheduleEnrollmentPendings.Add(new ScheduleEnrollmentPending
            {
                ScheduleId = scheduleId,
                StudentId = studentId!
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "schedule.enrollment_add",
            "schedule",
            scheduleId.ToString(),
            studentId,
            cancellationToken);
        return await GetAsync(scheduleId, cancellationToken);
    }

    public async Task<IResult> RemoveStudentAsync(
        Guid actorUserId,
        Guid scheduleId,
        Guid recordId,
        string type,
        CancellationToken cancellationToken)
    {
        if (type == "pending")
        {
            var pending = await db.ScheduleEnrollmentPendings.SingleOrDefaultAsync(
                x => x.Id == recordId && x.ScheduleId == scheduleId,
                cancellationToken);
            if (pending is null)
                return Results.NotFound(new { message = "ไม่พบรายการรอลงทะเบียน" });
            db.ScheduleEnrollmentPendings.Remove(pending);
            await db.SaveChangesAsync(cancellationToken);
            await auditLog.WriteAsync(
                actorUserId,
                "schedule.enrollment_remove",
                "schedule",
                scheduleId.ToString(),
                pending.StudentId,
                cancellationToken);
        }
        else
        {
            var enrollment = await db.ScheduleEnrollments.SingleOrDefaultAsync(
                x => x.Id == recordId && x.ScheduleId == scheduleId,
                cancellationToken);
            if (enrollment is null)
                return Results.NotFound(new { message = "ไม่พบรายชื่อนักศึกษาในคาบนี้" });
            db.ScheduleEnrollments.Remove(enrollment);
            await db.SaveChangesAsync(cancellationToken);
            await auditLog.WriteAsync(
                actorUserId,
                "schedule.enrollment_remove",
                "schedule",
                scheduleId.ToString(),
                enrollment.UserId.ToString(),
                cancellationToken);
        }

        return await GetAsync(scheduleId, cancellationToken);
    }

    public async Task<IResult> GetMineAsync(Guid userId, DateTime? date, CancellationToken cancellationToken)
    {
        var query = db.ScheduleEnrollments.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Join(db.Schedules.AsNoTracking().Where(s => s.IsActive), e => e.ScheduleId, s => s.Id, (e, s) => s)
            .Join(db.Rooms.AsNoTracking(), s => s.RoomId, r => r.Id, (s, r) => new { s, r });

        if (date is not null)
        {
            var local = BangkokTime.ToLocal(date.Value);
            var day = (int)local.DayOfWeek;
            query = query.Where(x => x.s.DayOfWeek == day);
        }

        var items = await query
            .OrderBy(x => x.s.DayOfWeek)
            .ThenBy(x => x.s.StartTime)
            .Select(x => new
            {
                id = x.s.Id,
                roomId = x.r.Id,
                room = x.r.Name,
                courseCode = x.s.CourseCode,
                courseName = x.s.CourseName,
                section = x.s.Section,
                instructorName = x.s.InstructorName,
                dayOfWeek = x.s.DayOfWeek,
                startTime = x.s.StartTime,
                endTime = x.s.EndTime,
                academicYear = x.s.AcademicYear,
                semester = x.s.Semester
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(items.Select(x => new
        {
            x.id,
            x.roomId,
            x.room,
            x.courseCode,
            x.courseName,
            x.section,
            x.instructorName,
            x.dayOfWeek,
            dayLabel = DayLabel(x.dayOfWeek),
            startTime = x.startTime.ToString("HH:mm"),
            endTime = x.endTime.ToString("HH:mm"),
            x.academicYear,
            x.semester,
            term = FormatTerm(x.semester, x.academicYear)
        }));
    }

    public async Task LinkPendingForStudentAsync(User user, CancellationToken cancellationToken)
    {
        if (user.UserType != UserType.Student || !StudentIds.IsValid(user.StudentId))
            return;

        var pending = await db.ScheduleEnrollmentPendings
            .Where(x => x.StudentId == user.StudentId)
            .ToListAsync(cancellationToken);
        foreach (var item in pending)
        {
            var exists = await db.ScheduleEnrollments.AnyAsync(
                x => x.ScheduleId == item.ScheduleId && x.UserId == user.Id,
                cancellationToken);
            if (!exists)
                db.ScheduleEnrollments.Add(new ScheduleEnrollment { ScheduleId = item.ScheduleId, UserId = user.Id });
            db.ScheduleEnrollmentPendings.Remove(item);
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RoomHasScheduleOverlapAsync(
        Guid roomId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var localStart = BangkokTime.ToLocal(startUtc);
        var localEnd = BangkokTime.ToLocal(endUtc);
        var day = (int)localStart.DayOfWeek;
        var start = TimeOnly.FromDateTime(localStart);
        var end = TimeOnly.FromDateTime(localEnd);
        return await HasScheduleOverlapAsync(roomId, day, start, end, null, cancellationToken);
    }

    public IResult DownloadTemplate() =>
        Results.File(
            ScheduleImportParser.CreateTemplate(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "kinof-schedule-template.xlsx");

    public async Task<IResult> PreviewImportAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        var parsed = await ParseImportFileAsync(file, cancellationToken);
        if (parsed.Error is not null)
            return parsed.Error;
        return Results.Ok(parsed.Preview);
    }

    public async Task<IResult> ConfirmImportAsync(
        Guid actorUserId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        var parsed = await ParseImportFileAsync(file, cancellationToken);
        if (parsed.Error is not null)
            return parsed.Error;
        if (!parsed.CanConfirm)
            return Results.BadRequest(new { message = "ไฟล์ยังมีข้อผิดพลาด กรุณาแก้แล้วลองใหม่อีกครั้ง", preview = parsed.Preview });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (parsed.WillDeactivatePreviousTerm)
        {
            var old = await db.Schedules.Where(x => x.IsActive).ToListAsync(cancellationToken);
            foreach (var item in old)
                item.IsActive = false;
        }

        var created = new Dictionary<string, Schedule>();
        foreach (var row in parsed.ScheduleRows)
        {
            var schedule = new Schedule
            {
                RoomId = parsed.Rooms[row.RoomName.ToLowerInvariant()].Id,
                CourseCode = row.SubjectCode,
                CourseName = row.SubjectName,
                Section = row.Section,
                InstructorName = row.InstructorName,
                DayOfWeek = row.DayOfWeek,
                StartTime = row.StartTime,
                EndTime = row.EndTime,
                AcademicYear = row.AcademicYear,
                Semester = row.Semester,
                IsActive = true
            };
            db.Schedules.Add(schedule);
            created[row.CompositeKey] = schedule;
        }
        await db.SaveChangesAsync(cancellationToken);

        var usersByStudentId = await db.Users
            .Where(x => x.UserType == UserType.Student && x.StudentId != null)
            .ToListAsync(cancellationToken);
        var userLookup = usersByStudentId
            .Where(x => x.StudentId is not null)
            .GroupBy(x => x.StudentId!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var enrolled = 0;
        var pending = 0;
        foreach (var row in parsed.EnrollmentRows)
        {
            if (!created.TryGetValue(row.CompositeKey, out var schedule))
                continue;
            if (userLookup.TryGetValue(row.StudentId, out var user))
            {
                db.ScheduleEnrollments.Add(new ScheduleEnrollment { ScheduleId = schedule.Id, UserId = user.Id });
                enrolled++;
            }
            else
            {
                db.ScheduleEnrollmentPendings.Add(new ScheduleEnrollmentPending
                {
                    ScheduleId = schedule.Id,
                    StudentId = row.StudentId
                });
                pending++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "schedule.import",
            "schedule",
            parsed.Term,
            $"schedules={created.Count}, enrolled={enrolled}, pending={pending}",
            cancellationToken);

        return Results.Ok(new
        {
            ok = true,
            term = parsed.Term,
            deactivatedPreviousTerm = parsed.WillDeactivatePreviousTerm,
            schedules = created.Count,
            enrolled,
            pending
        });
    }

    private async Task<ImportParseResult> ParseImportFileAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return ImportParseResult.Fail(Results.BadRequest(new { message = "กรุณาอัปโหลดไฟล์ Excel" }));
        if (file.Length > 5 * 1024 * 1024)
            return ImportParseResult.Fail(Results.BadRequest(new { message = "ไฟล์ต้องไม่เกิน 5 MB" }));

        await using var stream = file.OpenReadStream();
        List<ParsedScheduleRow> scheduleRows;
        List<ParsedEnrollmentRow> enrollmentRows;
        List<ImportIssue> issues;
        try
        {
            (scheduleRows, enrollmentRows, issues) = ScheduleImportParser.Parse(stream);
        }
        catch
        {
            return ImportParseResult.Fail(Results.BadRequest(new { message = "อ่านไฟล์ Excel ไม่ได้ กรุณาใช้แม่แบบของระบบ" }));
        }

        var rooms = await db.Rooms.ToListAsync(cancellationToken);
        var roomLookup = rooms.ToDictionary(x => x.Name.Trim().ToLowerInvariant(), x => x);
        var terms = scheduleRows.Select(x => FormatTerm(x.Semester, x.AcademicYear)).Distinct().ToList();
        if (terms.Count > 1)
            issues.Add(new ImportIssue(0, "term", "ไฟล์ต้องเป็นเทอมเดียวเท่านั้น", "error"));

        var term = terms.FirstOrDefault() ?? "";
        var active = await db.Schedules.AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
        var activeTerm = active
            .Select(x => FormatTerm(x.Semester, x.AcademicYear))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        var willDeactivate = !string.IsNullOrWhiteSpace(term) &&
            !string.IsNullOrWhiteSpace(activeTerm) &&
            !string.Equals(term, activeTerm, StringComparison.Ordinal);

        foreach (var row in scheduleRows)
        {
            if (!roomLookup.ContainsKey(row.RoomName.ToLowerInvariant()))
                issues.Add(new ImportIssue(row.Row, "room_name", $"ไม่พบห้อง {row.RoomName}", "error"));
        }

        var byKey = scheduleRows.GroupBy(x => x.CompositeKey).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var group in byKey.Where(x => x.Value.Count > 1))
        {
            foreach (var row in group.Value)
                issues.Add(new ImportIssue(row.Row, "schedule", "คาบซ้ำในไฟล์", "error"));
        }

        var overlapSource = willDeactivate
            ? new List<Schedule>()
            : active;
        foreach (var row in scheduleRows)
        {
            if (!roomLookup.TryGetValue(row.RoomName.ToLowerInvariant(), out var room))
                continue;
            var overlapExisting = overlapSource.Any(x =>
                x.RoomId == room.Id &&
                x.DayOfWeek == row.DayOfWeek &&
                ScheduleTimes.Overlaps(x.StartTime, x.EndTime, row.StartTime, row.EndTime));
            var overlapFile = scheduleRows.Any(other =>
                other.Row != row.Row &&
                other.RoomName.Equals(row.RoomName, StringComparison.OrdinalIgnoreCase) &&
                other.DayOfWeek == row.DayOfWeek &&
                ScheduleTimes.Overlaps(other.StartTime, other.EndTime, row.StartTime, row.EndTime));
            if (overlapExisting || overlapFile)
                issues.Add(new ImportIssue(row.Row, "time", "ช่วงเวลานี้ทับกับคาบอื่นในห้องเดียวกัน", "error"));
        }

        var enrollmentCounts = enrollmentRows
            .GroupBy(x => x.CompositeKey)
            .ToDictionary(g => g.Key, g => g.Select(x => x.StudentId).Distinct().Count());
        foreach (var row in scheduleRows)
        {
            if (!roomLookup.TryGetValue(row.RoomName.ToLowerInvariant(), out var room))
                continue;
            var count = enrollmentCounts.GetValueOrDefault(row.CompositeKey);
            if (count > room.Capacity)
                issues.Add(new ImportIssue(row.Row, "capacity", $"จำนวนนักศึกษา {count} เกินความจุห้อง {room.Capacity}", "error"));
        }

        var users = await db.Users.AsNoTracking()
            .Where(x => x.UserType == UserType.Student && x.StudentId != null)
            .Select(x => new { x.StudentId, x.FirstName, x.LastName })
            .ToListAsync(cancellationToken);
        var userLookup = users
            .Where(x => x.StudentId is not null)
            .GroupBy(x => x.StudentId!)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var row in enrollmentRows)
        {
            if (!byKey.ContainsKey(row.CompositeKey))
                issues.Add(new ImportIssue(row.Row, "schedule", "ไม่พบคาบที่ตรงกับรายชื่อนี้", "error"));
        }

        var duplicateStudents = enrollmentRows
            .GroupBy(x => (x.CompositeKey, x.StudentId))
            .Where(g => g.Count() > 1);
        foreach (var group in duplicateStudents)
        {
            foreach (var row in group)
                issues.Add(new ImportIssue(row.Row, "student_id", "รหัสนักศึกษาซ้ำในคาบเดียวกัน", "error"));
        }

        var errorCount = issues.Count(x => x.Level == "error");
        var warningCount = issues.Count(x => x.Level == "warning");
        var schedulePreview = scheduleRows.Select(row =>
        {
            var rowIssues = issues.Where(x => x.Row == row.Row && x.Level == "error").Select(x => x.Message).ToList();
            return new
            {
                row = row.Row,
                subjectCode = row.SubjectCode,
                subjectName = row.SubjectName,
                section = row.Section,
                instructorName = row.InstructorName,
                dayOfWeek = row.DayOfWeek,
                dayKey = row.DayKey,
                startTime = row.StartTime.ToString("HH:mm"),
                endTime = row.EndTime.ToString("HH:mm"),
                academicYear = row.AcademicYear,
                semester = row.Semester,
                roomName = row.RoomName,
                status = rowIssues.Count > 0 ? "error" : "ok",
                messages = rowIssues
            };
        }).ToList();
        var enrollmentPreview = enrollmentRows.Select(row =>
        {
            var rowIssues = issues.Where(x => x.Row == row.Row && x.Level == "error").Select(x => x.Message).ToList();
            var linked = userLookup.GetValueOrDefault(row.StudentId);
            var status = rowIssues.Count > 0 ? "error" : linked is null ? "pending" : "linked";
            if (linked is null && rowIssues.Count == 0)
                issues.Add(new ImportIssue(row.Row, "student_id", "ยังไม่มีบัญชี — จะเก็บเป็นรอลงทะเบียน", "warning"));
            return new
            {
                row = row.Row,
                studentId = row.StudentId,
                subjectCode = row.SubjectCode,
                section = row.Section,
                roomName = row.RoomName,
                status,
                linkedName = linked is null ? null : $"{linked.FirstName} {linked.LastName}",
                messages = rowIssues.Concat(linked is null && rowIssues.Count == 0
                    ? ["ยังไม่มีบัญชี — จะเก็บเป็นรอลงทะเบียน"]
                    : Array.Empty<string>()).ToList()
            };
        }).ToList();

        warningCount = issues.Count(x => x.Level == "warning");
        var preview = new
        {
            term,
            previousTerm = activeTerm,
            willDeactivatePreviousTerm = willDeactivate,
            errorCount,
            warningCount,
            canConfirm = errorCount == 0 && scheduleRows.Count > 0,
            issues,
            schedules = schedulePreview,
            enrollments = enrollmentPreview
        };

        return new ImportParseResult(
            null,
            preview,
            errorCount == 0 && scheduleRows.Count > 0,
            willDeactivate,
            term,
            scheduleRows,
            enrollmentRows,
            roomLookup);
    }

    private async Task<bool> HasScheduleOverlapAsync(
        Guid roomId,
        int dayOfWeek,
        TimeOnly start,
        TimeOnly end,
        Guid? exceptScheduleId,
        CancellationToken cancellationToken)
    {
        var existing = await db.Schedules
            .Where(x => x.IsActive && x.RoomId == roomId && x.DayOfWeek == dayOfWeek)
            .Where(x => exceptScheduleId == null || x.Id != exceptScheduleId)
            .ToListAsync(cancellationToken);
        return existing.Any(x => ScheduleTimes.Overlaps(x.StartTime, x.EndTime, start, end));
    }

    private static ParsedUpsert ValidateUpsert(UpsertScheduleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CourseCode))
            return ParsedUpsert.Fail("กรุณากรอกรหัสวิชา");
        if (request.DayOfWeek is < 0 or > 6)
            return ParsedUpsert.Fail("วันต้องอยู่ระหว่างอาทิตย์ถึงเสาร์");
        if (!ScheduleTimes.TryParse(request.StartTime, out var start) ||
            !ScheduleTimes.TryParse(request.EndTime, out var end))
        {
            return ParsedUpsert.Fail("รูปแบบเวลาไม่ถูกต้อง");
        }
        if (end <= start)
            return ParsedUpsert.Fail("เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่มต้น");
        var semester = request.Semester?.Trim();
        if (semester is not "1" and not "2")
            return ParsedUpsert.Fail("เทอมต้องเป็น 1 หรือ 2");
        if (string.IsNullOrWhiteSpace(request.AcademicYear))
            return ParsedUpsert.Fail("กรุณากรอกปีการศึกษา");

        return new ParsedUpsert(
            null,
            request.CourseCode.Trim(),
            string.IsNullOrWhiteSpace(request.CourseName) ? null : request.CourseName.Trim(),
            string.IsNullOrWhiteSpace(request.Section) ? "1" : request.Section.Trim(),
            string.IsNullOrWhiteSpace(request.InstructorName) ? null : request.InstructorName.Trim(),
            request.DayOfWeek,
            start,
            end,
            request.AcademicYear.Trim(),
            semester);
    }

    private static string DayLabel(int dayOfWeek) =>
        dayOfWeek is >= 0 and <= 6 ? ThaiDays[dayOfWeek] : "";

    private static string FormatTerm(string? semester, string? academicYear) =>
        string.IsNullOrWhiteSpace(semester) || string.IsNullOrWhiteSpace(academicYear)
            ? ""
            : $"{semester}/{academicYear}";

    private sealed record ParsedUpsert(
        IResult? Error,
        string CourseCode,
        string? CourseName,
        string Section,
        string? InstructorName,
        int Day,
        TimeOnly Start,
        TimeOnly End,
        string AcademicYear,
        string Semester)
    {
        public static ParsedUpsert Fail(string message) =>
            new(Results.BadRequest(new { message }), "", null, "1", null, 0, default, default, "", "1");
    }

    private sealed record ImportParseResult(
        IResult? Error,
        object? Preview,
        bool CanConfirm,
        bool WillDeactivatePreviousTerm,
        string Term,
        List<ParsedScheduleRow> ScheduleRows,
        List<ParsedEnrollmentRow> EnrollmentRows,
        Dictionary<string, Room> Rooms)
    {
        public static ImportParseResult Fail(IResult error) =>
            new(error, null, false, false, "", [], [], new Dictionary<string, Room>());
    }
}
