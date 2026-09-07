using Kinof.Api.Services;

namespace Kinof.Api;

public static class KioskEndpoints
{
    /// <summary>
    /// Kiosk machines stand in front of the lab door and have no user session, so this
    /// group stays out of the authorization pipeline. The entry OTP or the face itself is
    /// the credential, protected by <see cref="KioskAttemptLimiter"/>.
    /// </summary>
    public static void MapKioskEndpoints(this WebApplication app)
    {
        var kiosk = app.MapGroup("/api/kiosk");

        kiosk.MapGet("/rooms/{roomId:guid}", (
            Guid roomId,
            KioskService service,
            CancellationToken cancellationToken) =>
            service.GetRoomAsync(roomId, cancellationToken));

        kiosk.MapPost("/entry/verify-otp", (
            KioskVerifyOtpRequest? request,
            KioskService service,
            CancellationToken cancellationToken) =>
            service.VerifyEntryOtpAsync(request, cancellationToken));

        kiosk.MapPost("/entry/verify-face", (
            KioskVerifyFaceRequest? request,
            KioskService service,
            CancellationToken cancellationToken) =>
            service.VerifyFaceAsync(request, cancellationToken));
    }
}
