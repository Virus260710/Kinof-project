using Kinof.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        await SeedSuperAdminsAsync(db, configuration, environment);

        if (!environment.IsDevelopment())
            return;

        var studentEmail = configuration["Seed:StudentEmail"] ?? "kittisak.sati@bumail.net";
        if (await db.Users.AnyAsync(x => x.UserType != UserType.SuperAdmin))
        {
            var existingStudent = await db.Users.SingleOrDefaultAsync(x => x.Username == "student");
            if (existingStudent is not null && existingStudent.Email != studentEmail)
            {
                var emailTaken = await db.Users.AnyAsync(x =>
                    x.Id != existingStudent.Id && x.Email.ToLower() == studentEmail.ToLower());
                if (!emailTaken)
                {
                    existingStudent.Email = studentEmail;
                    existingStudent.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
            await SeedDevAgentsAsync(db);
            await SeedDevKioskDevicesAsync(db);
            await SeedProgramAllowlistIfEmptyAsync(db);
            await ResetDevScoresAndEntryOtpsOnceAsync(db, environment);
            await EnsureKioskDemoBookingAsync(db);
            await SeedMonitorDemoSamplesAsync(db);
            return;
        }

        var admin = new User
        {
            Username = "admin",
            Email = "admin@kinof.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                configuration["Seed:AdminPassword"] ?? "Admin123!",
                workFactor: 12),
            FirstName = "Admin",
            LastName = "System",
            JobTitle = "ผู้ดูแลระบบ",
            UserType = UserType.Admin
        };
        var student = new User
        {
            StudentId = "6600000001",
            Username = "student",
            Email = studentEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                configuration["Seed:StudentPassword"] ?? "Student123!",
                workFactor: 12),
            FirstName = "สมหญิง",
            LastName = "ตัวอย่าง",
            UserType = UserType.Student
        };

        var labA = new Room { Name = "ห้องแล็บ 1", Building = "อาคาร IT", Capacity = 30 };
        var labB = new Room { Name = "ห้องแล็บ 2", Building = "อาคาร IT", Capacity = 25 };
        var labC = new Room { Name = "ห้องแล็บ 3", Building = "อาคาร IT", Capacity = 30 };
        var labD = new Room { Name = "ห้องแล็บ 4", Building = "อาคาร IT", Capacity = 25 };
        db.AddRange(admin, student, labA, labB, labC, labD);

        db.Seats.AddRange(
            Enumerable.Range(1, labA.Capacity)
                .Select(number => new Seat
                {
                    RoomId = labA.Id,
                    SeatNumber = number,
                    ComputerName = $"PC-LAB-1-{number:00}"
                })
                .Concat(Enumerable.Range(1, labB.Capacity)
                    .Select(number => new Seat
                    {
                        RoomId = labB.Id,
                        SeatNumber = number,
                        ComputerName = $"PC-LAB-2-{number:00}"
                    }))
                .Concat(Enumerable.Range(1, labC.Capacity)
                    .Select(number => new Seat
                    {
                        RoomId = labC.Id,
                        SeatNumber = number,
                        ComputerName = $"PC-LAB-3-{number:00}"
                    }))
                .Concat(Enumerable.Range(1, labD.Capacity)
                    .Select(number => new Seat
                    {
                        RoomId = labD.Id,
                        SeatNumber = number,
                        ComputerName = $"PC-LAB-4-{number:00}"
                    })));

        db.WebsiteBlacklist.AddRange(
            new WebsiteBlacklist { UrlPattern = "facebook.com", Category = "social", Source = "manual" },
            new WebsiteBlacklist { UrlPattern = "tiktok.com", Category = "social", Source = "manual" },
            new WebsiteBlacklist { UrlPattern = "twitter.com", Category = "social", Source = "manual" });

        db.ProgramBlacklist.AddRange(
            new ProgramBlacklist { ProcessName = "discord.exe", Category = "chat" },
            new ProgramBlacklist { ProcessName = "steam.exe", Category = "game" });

        await db.SaveChangesAsync();
        await SeedDevAgentsAsync(db);
        await SeedDevKioskDevicesAsync(db);
        await SeedProgramAllowlistIfEmptyAsync(db);
        await ResetDevScoresAndEntryOtpsOnceAsync(db, environment);
        await EnsureKioskDemoBookingAsync(db);
        await SeedMonitorDemoSamplesAsync(db);
    }

    private static async Task SeedProgramBlacklistIfEmptyAsync(AppDbContext db)
    {
        if (await db.ProgramBlacklist.AnyAsync())
            return;

        db.ProgramBlacklist.AddRange(
            new ProgramBlacklist { ProcessName = "discord.exe", Category = "chat" },
            new ProgramBlacklist { ProcessName = "steam.exe", Category = "game" });
        await db.SaveChangesAsync();
    }

    private static async Task SeedProgramAllowlistIfEmptyAsync(AppDbContext db)
    {
        if (await db.ProgramAllowlist.AnyAsync())
            return;

        db.ProgramAllowlist.AddRange(
            new ProgramAllowlist { ProcessName = "chrome.exe", DisplayName = "Google Chrome", Category = "browser" },
            new ProgramAllowlist { ProcessName = "msedge.exe", DisplayName = "Microsoft Edge", Category = "browser" },
            new ProgramAllowlist { ProcessName = "msedgewebview2.exe", DisplayName = "Edge WebView", Category = "browser" },
            new ProgramAllowlist { ProcessName = "firefox.exe", DisplayName = "Firefox", Category = "browser" },
            new ProgramAllowlist { ProcessName = "code.exe", DisplayName = "Visual Studio Code", Category = "dev" },
            new ProgramAllowlist { ProcessName = "devenv.exe", DisplayName = "Visual Studio", Category = "dev" },
            new ProgramAllowlist { ProcessName = "notepad.exe", DisplayName = "Notepad", Category = "lab" },
            new ProgramAllowlist { ProcessName = "notepad++.exe", DisplayName = "Notepad++", Category = "dev" },
            new ProgramAllowlist { ProcessName = "winword.exe", DisplayName = "Microsoft Word", Category = "office" },
            new ProgramAllowlist { ProcessName = "excel.exe", DisplayName = "Microsoft Excel", Category = "office" },
            new ProgramAllowlist { ProcessName = "powerpnt.exe", DisplayName = "Microsoft PowerPoint", Category = "office" },
            new ProgramAllowlist { ProcessName = "outlook.exe", DisplayName = "Microsoft Outlook", Category = "office" },
            new ProgramAllowlist { ProcessName = "onenote.exe", DisplayName = "OneNote", Category = "office" },
            new ProgramAllowlist { ProcessName = "teams.exe", DisplayName = "Microsoft Teams", Category = "office" },
            new ProgramAllowlist { ProcessName = "ms-teams.exe", DisplayName = "Microsoft Teams (new)", Category = "office" },
            new ProgramAllowlist { ProcessName = "acrobat.exe", DisplayName = "Adobe Acrobat", Category = "lab" },
            new ProgramAllowlist { ProcessName = "acrord32.exe", DisplayName = "Adobe Reader", Category = "lab" },
            new ProgramAllowlist { ProcessName = "python.exe", DisplayName = "Python", Category = "dev" },
            new ProgramAllowlist { ProcessName = "pythonw.exe", DisplayName = "Python (windowed)", Category = "dev" },
            new ProgramAllowlist { ProcessName = "java.exe", DisplayName = "Java", Category = "dev" },
            new ProgramAllowlist { ProcessName = "javaw.exe", DisplayName = "Java (windowed)", Category = "dev" },
            new ProgramAllowlist { ProcessName = "node.exe", DisplayName = "Node.js", Category = "dev" },
            new ProgramAllowlist { ProcessName = "cmd.exe", DisplayName = "Command Prompt", Category = "lab" },
            new ProgramAllowlist { ProcessName = "powershell.exe", DisplayName = "Windows PowerShell", Category = "lab" },
            new ProgramAllowlist { ProcessName = "pwsh.exe", DisplayName = "PowerShell", Category = "lab" },
            new ProgramAllowlist { ProcessName = "windowsterminal.exe", DisplayName = "Windows Terminal", Category = "lab" },
            new ProgramAllowlist { ProcessName = "git.exe", DisplayName = "Git", Category = "dev" },
            new ProgramAllowlist { ProcessName = "postman.exe", DisplayName = "Postman", Category = "dev" },
            new ProgramAllowlist { ProcessName = "ssms.exe", DisplayName = "SQL Server Management Studio", Category = "dev" },
            new ProgramAllowlist { ProcessName = "mysqlworkbench.exe", DisplayName = "MySQL Workbench", Category = "dev" },
            new ProgramAllowlist { ProcessName = "pycharm64.exe", DisplayName = "PyCharm", Category = "dev" },
            new ProgramAllowlist { ProcessName = "idea64.exe", DisplayName = "IntelliJ IDEA", Category = "dev" },
            new ProgramAllowlist { ProcessName = "calc.exe", DisplayName = "Calculator", Category = "lab" },
            new ProgramAllowlist { ProcessName = "mspaint.exe", DisplayName = "Paint", Category = "lab" },
            new ProgramAllowlist { ProcessName = "snippingtool.exe", DisplayName = "Snipping Tool", Category = "lab" });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Gives the first two seats of the first lab a predictable agent key so
    /// <c>scripts/simulate-agent.ps1</c> can run without provisioning through the admin UI.
    /// Development only — production agents are created via POST /api/admin/agents.
    /// </summary>
    private static async Task SeedDevAgentsAsync(AppDbContext db)
    {
        var room = await db.Rooms.OrderBy(x => x.Name).FirstOrDefaultAsync();
        if (room is null)
            return;

        var seats = await db.Seats
            .Where(seat => seat.RoomId == room.Id)
            .OrderBy(seat => seat.SeatNumber)
            .Take(2)
            .ToListAsync();

        var added = false;
        for (var index = 0; index < seats.Count; index++)
        {
            var seat = seats[index];
            if (await db.Agents.AnyAsync(agent => agent.SeatId == seat.Id))
                continue;

            db.Agents.Add(new Agent
            {
                SeatId = seat.Id,
                ApiKey = $"dev-agent-key-{index + 1}",
                Hostname = seat.ComputerName
            });
            added = true;
        }

        if (added)
            await db.SaveChangesAsync();
    }

    /// <summary>
    /// Gives each seeded lab a predictable door key so local Kiosk pages can be
    /// opened with <c>?key=dev-kiosk-key-N</c> without provisioning through the admin UI.
    /// Development only — production devices are created via POST /api/admin/kiosk-devices.
    /// </summary>
    private static async Task SeedDevKioskDevicesAsync(AppDbContext db)
    {
        var rooms = await db.Rooms.OrderBy(room => room.Name).ToListAsync();
        var added = false;
        for (var index = 0; index < rooms.Count; index++)
        {
            var room = rooms[index];
            var apiKey = $"dev-kiosk-key-{index + 1}";
            if (await db.KioskDevices.AnyAsync(device => device.ApiKey == apiKey))
                continue;

            db.KioskDevices.Add(new KioskDevice
            {
                RoomId = room.Id,
                ApiKey = apiKey,
                Label = "เครื่องประตู (dev)"
            });
            added = true;
        }

        if (added)
            await db.SaveChangesAsync();
    }

    private static async Task ResetDevScoresAndEntryOtpsOnceAsync(
        AppDbContext db,
        IHostEnvironment environment)
    {
        var marker = Path.Combine(environment.ContentRootPath, "App_Data", "dev-reset-scores-otp.done");
        if (File.Exists(marker))
            return;

        db.BehaviorPenalties.RemoveRange(db.BehaviorPenalties);
        db.EntryOtps.RemoveRange(db.EntryOtps);
        await db.SaveChangesAsync();
        Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
        await File.WriteAllTextAsync(marker, DateTime.UtcNow.ToString("O"));
    }

    private static async Task EnsureKioskDemoBookingAsync(AppDbContext db)
    {
        const string email = "noiegoh116@gmail.com";
        var user = await db.Users.FirstOrDefaultAsync(item => item.Email.ToLower() == email);
        if (user is null)
        {
            user = new User
            {
                StudentId = "6600000116",
                Username = "noiegoh116",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student123!", workFactor: 12),
                FirstName = "Noie",
                LastName = "Goh",
                UserType = UserType.Student
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var room = await db.Rooms.OrderBy(item => item.Name).FirstOrDefaultAsync();
        if (room is null)
            return;

        var start = BangkokTime.ToUtc(new DateTime(2026, 9, 17, 16, 30, 0));
        var end = BangkokTime.ToUtc(new DateTime(2026, 9, 17, 19, 0, 0));
        var exists = await db.Bookings.AnyAsync(item =>
            item.UserId == user.Id &&
            item.RoomId == room.Id &&
            item.StartTime == start &&
            item.EndTime == end &&
            (item.Status == BookingStatus.Confirmed || item.Status == BookingStatus.Pending));
        if (exists)
            return;

        db.Bookings.Add(new Booking
        {
            UserId = user.Id,
            RoomId = room.Id,
            StartTime = start,
            EndTime = end,
            Status = BookingStatus.Confirmed
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedMonitorDemoSamplesAsync(AppDbContext db)
    {
        var agent = await db.Agents.AsNoTracking().OrderBy(item => item.Hostname).FirstOrDefaultAsync();
        if (agent is not null)
        {
            var demoProgram = "kinof-demo-unknown.exe";
            var alreadyUnknown = await db.AgentLogs.AnyAsync(item =>
                item.EventType == AgentEventTypes.UnknownProgram &&
                item.DataJson != null &&
                item.DataJson.Contains(demoProgram));
            if (!alreadyUnknown)
            {
                db.AgentLogs.Add(new AgentLog
                {
                    AgentId = agent.Id,
                    EventType = AgentEventTypes.UnknownProgram,
                    DataJson = new AgentLogPayload
                    {
                        Program = demoProgram,
                        Activity = "โปรแกรมตัวอย่างสำหรับแท็บไม่รู้จัก",
                        Suspicious = false,
                        Source = "seed"
                    }.ToJson()
                });
                await db.SaveChangesAsync();
            }
        }

        const string demoTarget = "kinof-demo-flag.example";
        if (await db.BehaviorReviews.AnyAsync(item => item.Target == demoTarget))
            return;

        var student = await db.Users.AsNoTracking()
            .Where(item => item.UserType == UserType.Student)
            .OrderBy(item => item.Username)
            .FirstOrDefaultAsync();
        var room = await db.Rooms.AsNoTracking().OrderBy(item => item.Name).FirstOrDefaultAsync();
        var seat = room is null
            ? null
            : await db.Seats.AsNoTracking()
                .Where(item => item.RoomId == room.Id)
                .OrderBy(item => item.SeatNumber)
                .FirstOrDefaultAsync();

        db.BehaviorReviews.Add(new BehaviorReview
        {
            UserId = student?.Id,
            DisplayName = student is null ? "ผู้ใช้ตัวอย่าง" : $"{student.FirstName} {student.LastName}".Trim(),
            Username = student?.Username,
            RoomId = room?.Id,
            SeatId = seat?.Id,
            RoomName = room?.Name,
            SeatLabel = seat is null ? null : TrackingService.SeatLabel(seat.SeatNumber),
            Kind = "website",
            Target = demoTarget,
            Activity = $"เข้าเว็บไซต์ ({demoTarget}) — ตัวอย่างคิวแท็บน่าสงสัย",
            QueueKey = $"{student?.Id.ToString("N") ?? "anon"}:website:{demoTarget}",
            OccurrenceCount = 2
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedSuperAdminsAsync(
        AppDbContext db,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var password = configuration["Seed:SuperAdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            if (!environment.IsDevelopment())
                return;
            password = "SuperAdmin123!";
        }

        var accounts = configuration.GetSection("Seed:SuperAdmins").GetChildren().ToList();
        if (accounts.Count == 0)
        {
            accounts = null;
        }

        var defaults = new (string Username, string Email, string FirstName, string LastName)[]
        {
            ("superadmin1", "superadmin1@kinof.local", "Super", "One"),
            ("superadmin2", "superadmin2@kinof.local", "Super", "Two"),
            ("superadmin3", "superadmin3@kinof.local", "Super", "Three")
        };

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        for (var index = 0; index < 3; index++)
        {
            var username = accounts?.ElementAtOrDefault(index)?["Username"] ?? defaults[index].Username;
            var email = accounts?.ElementAtOrDefault(index)?["Email"] ?? defaults[index].Email;
            var firstName = accounts?.ElementAtOrDefault(index)?["FirstName"] ?? defaults[index].FirstName;
            var lastName = accounts?.ElementAtOrDefault(index)?["LastName"] ?? defaults[index].LastName;
            username = username.Trim().ToLowerInvariant();
            email = email.Trim().ToLowerInvariant();

            var existing = await db.Users.SingleOrDefaultAsync(x => x.Username == username);
            if (existing is not null)
            {
                if (existing.UserType != UserType.SuperAdmin)
                {
                    existing.UserType = UserType.SuperAdmin;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                continue;
            }

            if (await db.Users.AnyAsync(x => x.Email.ToLower() == email))
                continue;

            db.Users.Add(new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                FirstName = firstName,
                LastName = lastName,
                JobTitle = "ผู้ดูแลสูงสุด",
                UserType = UserType.SuperAdmin
            });
        }

        await db.SaveChangesAsync();
    }
}
