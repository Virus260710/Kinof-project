using System.Security.Claims;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed class NavBadgeService(AppDbContext db)
{
    public async Task<IResult> GetAsync(ClaimsPrincipal user, Guid userId, CancellationToken cancellationToken)
    {
        if (StaffAuth.IsStaff(user))
        {
            var helpCenter = await db.ProblemReports.AsNoTracking()
                .CountAsync(item => item.Status != ProblemReportStatus.Resolved, cancellationToken);
            var monitor = await db.BehaviorReviews.AsNoTracking()
                .CountAsync(item => item.Status == BehaviorReviewStatus.Pending, cancellationToken);
            return Results.Ok(new
            {
                invite = 0,
                monitor,
                helpcenter = helpCenter
            });
        }

        var invite = await db.Invitations.AsNoTracking()
            .CountAsync(
                item => item.InviteeUserId == userId && item.Status == InvitationStatus.Pending,
                cancellationToken);
        return Results.Ok(new
        {
            invite,
            monitor = 0,
            helpcenter = 0
        });
    }
}
