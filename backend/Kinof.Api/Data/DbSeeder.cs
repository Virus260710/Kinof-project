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
                existingStudent.Email = studentEmail;
                existingStudent.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
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
            new WebsiteBlacklist { UrlPattern = "facebook.com", Category = "social" },
            new WebsiteBlacklist { UrlPattern = "tiktok.com", Category = "social" },
            new WebsiteBlacklist { UrlPattern = "twitter.com", Category = "social" });

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
