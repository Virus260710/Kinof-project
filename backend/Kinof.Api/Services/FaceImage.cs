namespace Kinof.Api.Services;

/// <summary>
/// Validation for the base64 data URLs the browser sends for face work: enrollment
/// (<see cref="AuthService.RegisterFaceAsync"/>) and Kiosk face entry
/// (<see cref="KioskService.VerifyFaceAsync"/>) accept exactly the same payload shape.
/// </summary>
public static class FaceImage
{
    private const int MinBytes = 1_024;
    private const int MaxBytes = 5 * 1_024 * 1_024;

    /// <summary>Base64 inflates by ~4/3, so this caps the request before decoding.</summary>
    private const int MaxBase64Length = 7_000_000;

    private static readonly Dictionary<string, string> SupportedPrefixes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["data:image/jpeg;base64,"] = "image/jpeg",
            ["data:image/png;base64,"] = "image/png",
            ["data:image/webp;base64,"] = "image/webp"
        };

    public static bool TryDecode(
        string? imageBase64,
        out byte[] image,
        out string contentType,
        out string validationMessage)
    {
        image = [];
        contentType = "";
        validationMessage = "";

        if (string.IsNullOrWhiteSpace(imageBase64) || imageBase64.Length > MaxBase64Length)
        {
            validationMessage = "ภาพใบหน้าไม่ถูกต้องหรือมีขนาดใหญ่เกิน 5 MB";
            return false;
        }

        var prefix = SupportedPrefixes.Keys.FirstOrDefault(
            candidate => imageBase64.StartsWith(candidate, StringComparison.OrdinalIgnoreCase));
        if (prefix is null)
        {
            validationMessage = "รองรับเฉพาะภาพ JPEG, PNG หรือ WebP";
            return false;
        }

        try
        {
            image = Convert.FromBase64String(imageBase64[prefix.Length..]);
        }
        catch (FormatException)
        {
            validationMessage = "ข้อมูลภาพใบหน้าไม่ถูกต้อง";
            return false;
        }

        if (image.Length is < MinBytes or > MaxBytes)
        {
            validationMessage = "ภาพใบหน้าไม่ถูกต้องหรือมีขนาดใหญ่เกิน 5 MB";
            return false;
        }

        contentType = SupportedPrefixes[prefix];
        return true;
    }
}
