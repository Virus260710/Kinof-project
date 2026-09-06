using System.Security.Claims;
using Kinof.Api.Data;
using Kinof.Api.Services;

namespace Kinof.Api;

public static class AdminEndpoints
{
    public static void MapAdminAndScheduleEndpoints(this WebApplication app)
    {
        app.MapGet("/api/schedule/me", (
            DateTime? date,
            ClaimsPrincipal user,
            ScheduleService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            return userId is null
                ? Task.FromResult(Results.Unauthorized())
                : service.GetMineAsync(userId.Value, date, cancellationToken);
        }).RequireAuthorization();

        var admin = app.MapGroup("/api/admin").RequireAuthorization();

        admin.MapGet("/users", (
            ClaimsPrincipal user,
            AdminUserService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsSuperAdmin(user)
                ? service.ListAsync(cancellationToken)
                : Task.FromResult(Results.Forbid()));

        admin.MapPost("/users", (
            CreateAdminRequest request,
            ClaimsPrincipal user,
            AdminUserService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsSuperAdmin(user)) return Task.FromResult(Results.Forbid());
            return service.CreateAsync(userId.Value, request, cancellationToken);
        });

        admin.MapPut("/users/{adminId:guid}", (
            Guid adminId,
            UpdateAdminRequest request,
            ClaimsPrincipal user,
            AdminUserService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsSuperAdmin(user)) return Task.FromResult(Results.Forbid());
            return service.UpdateAsync(userId.Value, adminId, request, cancellationToken);
        });

        admin.MapPost("/users/{adminId:guid}/disable", (
            Guid adminId,
            ClaimsPrincipal user,
            AdminUserService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsSuperAdmin(user)) return Task.FromResult(Results.Forbid());
            return service.SetStatusAsync(userId.Value, adminId, UserStatus.Disabled, cancellationToken);
        });

        admin.MapPost("/users/{adminId:guid}/enable", (
            Guid adminId,
            ClaimsPrincipal user,
            AdminUserService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsSuperAdmin(user)) return Task.FromResult(Results.Forbid());
            return service.SetStatusAsync(userId.Value, adminId, UserStatus.Active, cancellationToken);
        });

        admin.MapPost("/users/{adminId:guid}/resend-invite", (
            Guid adminId,
            ClaimsPrincipal user,
            AdminUserService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsSuperAdmin(user)) return Task.FromResult(Results.Forbid());
            return service.ResendInviteAsync(userId.Value, adminId, cancellationToken);
        });

        admin.MapGet("/audit-logs", (
            string? action,
            DateTime? from,
            DateTime? to,
            int? page,
            int? limit,
            ClaimsPrincipal user,
            AuditLogService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsSuperAdmin(user)
                ? service.ListAsync(action, from, to, page ?? 1, limit ?? 50, cancellationToken)
                : Task.FromResult(Results.Forbid()));

        admin.MapGet("/rooms", (
            ClaimsPrincipal user,
            RoomAdminService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.ListAsync(cancellationToken)
                : Task.FromResult(Results.Forbid()));

        admin.MapPost("/rooms", (
            UpsertRoomRequest request,
            ClaimsPrincipal user,
            RoomAdminService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.CreateAsync(userId.Value, request, cancellationToken);
        });

        admin.MapPut("/rooms/{roomId:guid}", (
            Guid roomId,
            UpsertRoomRequest request,
            ClaimsPrincipal user,
            RoomAdminService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.UpdateAsync(userId.Value, roomId, request, cancellationToken);
        });

        admin.MapDelete("/rooms/{roomId:guid}", (
            Guid roomId,
            ClaimsPrincipal user,
            RoomAdminService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.DeleteAsync(userId.Value, roomId, cancellationToken);
        });

        admin.MapGet("/schedules", (
            bool? active,
            ClaimsPrincipal user,
            ScheduleService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.ListAsync(active, cancellationToken)
                : Task.FromResult(Results.Forbid()));

        admin.MapGet("/schedules/template", (ClaimsPrincipal user, ScheduleService service) =>
            StaffAuth.IsStaff(user) ? service.DownloadTemplate() : Results.Forbid());

        admin.MapGet("/schedules/{scheduleId:guid}", (
            Guid scheduleId,
            ClaimsPrincipal user,
            ScheduleService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.GetAsync(scheduleId, cancellationToken)
                : Task.FromResult(Results.Forbid()));

        admin.MapPost("/schedules", (
            UpsertScheduleRequest request,
            ClaimsPrincipal user,
            ScheduleService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.CreateAsync(userId.Value, request, cancellationToken);
        });

        admin.MapPut("/schedules/{scheduleId:guid}", (
            Guid scheduleId,
            UpsertScheduleRequest request,
            ClaimsPrincipal user,
            ScheduleService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.UpdateAsync(userId.Value, scheduleId, request, cancellationToken);
        });

        admin.MapDelete("/schedules/{scheduleId:guid}", (
            Guid scheduleId,
            ClaimsPrincipal user,
            ScheduleService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.SoftDeleteAsync(userId.Value, scheduleId, cancellationToken);
        });

        admin.MapPost("/schedules/{scheduleId:guid}/enrollments", (
            Guid scheduleId,
            AddEnrollmentRequest request,
            ClaimsPrincipal user,
            ScheduleService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.AddStudentAsync(userId.Value, scheduleId, request, cancellationToken);
        });

        admin.MapDelete("/schedules/{scheduleId:guid}/enrollments/{recordId:guid}", (
            Guid scheduleId,
            Guid recordId,
            string? type,
            ClaimsPrincipal user,
            ScheduleService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.RemoveStudentAsync(userId.Value, scheduleId, recordId, type ?? "enrolled", cancellationToken);
        });

        admin.MapPost("/schedules/import/preview", async (
            HttpRequest request,
            ClaimsPrincipal user,
            ScheduleService service,
            CancellationToken cancellationToken) =>
        {
            if (!StaffAuth.IsStaff(user)) return Results.Forbid();
            var file = request.HasFormContentType ? request.Form.Files["file"] : null;
            return await service.PreviewImportAsync(file, cancellationToken);
        }).DisableAntiforgery();

        admin.MapPost("/schedules/import/confirm", async (
            HttpRequest request,
            ClaimsPrincipal user,
            ScheduleService service,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthService.GetUserId(user);
            if (userId is null) return Results.Unauthorized();
            if (!StaffAuth.IsStaff(user)) return Results.Forbid();
            var file = request.HasFormContentType ? request.Form.Files["file"] : null;
            return await service.ConfirmImportAsync(userId.Value, file, cancellationToken);
        }).DisableAntiforgery();
    }
}
