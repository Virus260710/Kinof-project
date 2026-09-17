using Kinof.Api.Services;

namespace Kinof.Api;

public static class KioskEndpoints
{
    public const string ApiKeyHeader = "X-Kiosk-Key";

    /// <summary>
    /// Kiosk machines stand in front of the lab door and have no user session, so this
    /// group stays out of the JWT pipeline. Door devices authenticate with
    /// <see cref="ApiKeyHeader"/> bound to a room (not a seat). Face or the emergency
    /// entry OTP is the person's credential. Success only means they may enter the room —
    /// seats are not assigned here.
    /// </summary>
    public static void MapKioskEndpoints(this WebApplication app)
    {
        var kiosk = app.MapGroup("/api/kiosk");

        kiosk.MapGet("/rooms/{roomId:guid}", (
            Guid roomId,
            HttpRequest httpRequest,
            KioskService service,
            CancellationToken cancellationToken) =>
            service.GetRoomAsync(ReadKey(httpRequest), roomId, cancellationToken));

        kiosk.MapPost("/entry/verify-otp", (
            KioskVerifyOtpRequest? request,
            HttpRequest httpRequest,
            KioskService service,
            CancellationToken cancellationToken) =>
            service.VerifyEntryOtpAsync(ReadKey(httpRequest), request, cancellationToken));

        kiosk.MapPost("/entry/verify-face", (
            KioskVerifyFaceRequest? request,
            HttpRequest httpRequest,
            KioskService service,
            CancellationToken cancellationToken) =>
            service.VerifyFaceAsync(ReadKey(httpRequest), request, cancellationToken));
    }

    private static string? ReadKey(HttpRequest request) =>
        request.Headers.TryGetValue(ApiKeyHeader, out var values) ? values.ToString() : null;
}
