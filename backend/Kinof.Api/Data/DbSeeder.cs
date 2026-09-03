using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            return;

        var studentEmail = configuration["Seed:StudentEmail"] ?? "kittisak.sati@bumail.net";
        if (await db.Users.AnyAsync())
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
}
