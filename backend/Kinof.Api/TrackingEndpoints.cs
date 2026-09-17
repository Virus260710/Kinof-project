using System.Security.Claims;
using Kinof.Api.Services;

namespace Kinof.Api;

public static class TrackingEndpoints
{
    public static void MapTrackingEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin").RequireAuthorization();

        admin.MapGet("/agents", (
            Guid? roomId,
            ClaimsPrincipal user,
            TrackingService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.ListAgentsAsync(roomId, cancellationToken)
                : Task.FromResult(Results.Forbid()));

        admin.MapPost("/agents", (
            CreateAgentRequest request,
            ClaimsPrincipal user,
            TrackingService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.CreateAgentAsync(actorId.Value, request, cancellationToken);
        });

        var tracking = admin.MapGroup("/tracking");

        tracking.MapGet("/summary", (
            ClaimsPrincipal user,
            TrackingService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.GetSummaryAsync(cancellationToken)
                : Task.FromResult(Results.Forbid()));

        admin.MapGet("/behavior/reviews", (
            Guid? roomId,
            ClaimsPrincipal user,
            BehaviorScoreService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.ListReviewsAsync(roomId, cancellationToken)
                : Task.FromResult(Results.Forbid()));

        admin.MapPost("/behavior/reviews/{reviewId:guid}/clear", (
            Guid reviewId,
            ClaimsPrincipal user,
            BehaviorScoreService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.ClearReviewAsync(actorId.Value, reviewId, cancellationToken);
        });

        admin.MapPost("/behavior/reviews/{reviewId:guid}/penalize", (
            Guid reviewId,
            ClaimsPrincipal user,
            BehaviorScoreService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.PenalizeReviewAsync(actorId.Value, reviewId, cancellationToken);
        });

        admin.MapPost("/behavior/block", (
            BlockFlaggedRequest request,
            ClaimsPrincipal user,
            BehaviorScoreService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.BlockFromActivityAsync(actorId.Value, request, cancellationToken);
        });

        admin.MapPost("/behavior/clear", (
            BlockFlaggedRequest request,
            ClaimsPrincipal user,
            BehaviorScoreService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.ClearFromActivityAsync(actorId.Value, request, cancellationToken);
        });

        tracking.MapGet("/rooms", (
            ClaimsPrincipal user,
            TrackingService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.GetRoomsAsync(cancellationToken)
                : Task.FromResult(Results.Forbid()));

        tracking.MapGet("/seats", (
            Guid roomId,
            ClaimsPrincipal user,
            TrackingService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.GetSeatsAsync(roomId, cancellationToken)
                : Task.FromResult(Results.Forbid()));

        tracking.MapGet("/activity", (
            Guid? roomId,
            string? date,
            string? type,
            ClaimsPrincipal user,
            TrackingService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.GetActivityAsync(roomId, date, type, cancellationToken)
                : Task.FromResult(Results.Forbid()));

        tracking.MapGet("/seats/{seatId:guid}/activity", (
            Guid seatId,
            int? limit,
            ClaimsPrincipal user,
            TrackingService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.GetSeatActivityAsync(seatId, limit ?? 50, cancellationToken)
                : Task.FromResult(Results.Forbid()));

        tracking.MapPut("/rooms/{roomId:guid}/status", (
            Guid roomId,
            UpdateRoomStatusRequest request,
            ClaimsPrincipal user,
            TrackingService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.UpdateRoomStatusAsync(actorId.Value, roomId, request, cancellationToken);
        });

        tracking.MapPost("/rooms/{roomId:guid}/bulk-action", (
            Guid roomId,
            RoomBulkActionRequest request,
            ClaimsPrincipal user,
            TrackingService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.BulkRoomActionAsync(actorId.Value, roomId, request, cancellationToken);
        });

        tracking.MapPost("/seats/{seatId:guid}/logout", (
            Guid seatId,
            ClaimsPrincipal user,
            TrackingService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.ForceSeatLogoutAsync(actorId.Value, seatId, cancellationToken);
        });

        tracking.MapGet("/website-blacklist", (
            ClaimsPrincipal user,
            WebsiteBlacklistService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.ListAsync(cancellationToken)
                : Task.FromResult(Results.Forbid()));

        tracking.MapPost("/website-blacklist", (
            AddWebsiteBlacklistRequest request,
            ClaimsPrincipal user,
            WebsiteBlacklistService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.AddAsync(actorId.Value, request, cancellationToken);
        });

        tracking.MapDelete("/website-blacklist/{id:int}", (
            int id,
            ClaimsPrincipal user,
            WebsiteBlacklistService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.RemoveAsync(actorId.Value, id, cancellationToken);
        });

        tracking.MapGet("/program-blacklist", (
            ClaimsPrincipal user,
            ProgramBlacklistService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.ListAsync(cancellationToken)
                : Task.FromResult(Results.Forbid()));

        tracking.MapPost("/program-blacklist", (
            AddProgramBlacklistRequest request,
            ClaimsPrincipal user,
            ProgramBlacklistService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.AddAsync(actorId.Value, request, cancellationToken);
        });

        tracking.MapDelete("/program-blacklist/{id:int}", (
            int id,
            ClaimsPrincipal user,
            ProgramBlacklistService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.RemoveAsync(actorId.Value, id, cancellationToken);
        });

        tracking.MapGet("/website-blacklist/categories", (
            ClaimsPrincipal user,
            Ut1WebsiteCategoryService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.ListCategoriesAsync(cancellationToken)
                : Task.FromResult(Results.Forbid()));

        tracking.MapPost("/website-blacklist/import", (
            ImportUt1CategoryRequest request,
            ClaimsPrincipal user,
            Ut1WebsiteCategoryService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.ImportAsync(actorId.Value, request, cancellationToken);
        });

        tracking.MapDelete("/website-blacklist/categories/{category}", (
            string category,
            ClaimsPrincipal user,
            Ut1WebsiteCategoryService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.RemoveCategoryAsync(actorId.Value, category, cancellationToken);
        });

        tracking.MapGet("/program-allowlist", (
            ClaimsPrincipal user,
            ProgramAllowlistService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.ListAsync(cancellationToken)
                : Task.FromResult(Results.Forbid()));

        tracking.MapPost("/program-allowlist", (
            AddProgramAllowlistRequest request,
            ClaimsPrincipal user,
            ProgramAllowlistService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.AddAsync(actorId.Value, request, cancellationToken);
        });

        tracking.MapDelete("/program-allowlist/{id:int}", (
            int id,
            ClaimsPrincipal user,
            ProgramAllowlistService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthService.GetUserId(user);
            if (actorId is null) return Task.FromResult(Results.Unauthorized());
            if (!StaffAuth.IsStaff(user)) return Task.FromResult(Results.Forbid());
            return service.RemoveAsync(actorId.Value, id, cancellationToken);
        });

        tracking.MapGet("/unknown-programs", (
            Guid? roomId,
            string? date,
            ClaimsPrincipal user,
            TrackingService service,
            CancellationToken cancellationToken) =>
            StaffAuth.IsStaff(user)
                ? service.GetUnknownProgramsAsync(roomId, date, cancellationToken)
                : Task.FromResult(Results.Forbid()));
    }
}
