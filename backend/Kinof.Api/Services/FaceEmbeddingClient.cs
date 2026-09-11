using System.Net.Http.Json;
using System.Text.Json;

namespace Kinof.Api.Services;

public sealed class FaceServiceOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8001";
    public int TimeoutSeconds { get; set; } = 30;
}

public interface IFaceEmbeddingClient
{
    Task<float[]> CreateEmbeddingAsync(
        byte[] image,
        string contentType,
        CancellationToken cancellationToken);
}

public sealed class FaceEmbeddingClient(
    HttpClient httpClient,
    ILogger<FaceEmbeddingClient> logger) : IFaceEmbeddingClient
{
    public async Task<float[]> CreateEmbeddingAsync(
        byte[] image,
        string contentType,
        CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(image);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(imageContent, "image", "face.jpg");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync(
                "/api/v1/embeddings",
                content,
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new FaceServiceException(
                "Face Service ใช้เวลาประมวลผลนานเกินไป กรุณาลองใหม่",
                StatusCodes.Status503ServiceUnavailable);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Unable to connect to Face Service");
            throw new FaceServiceException(
                "ไม่สามารถเชื่อมต่อ Face Service ได้ กรุณาตรวจสอบว่า service ทำงานอยู่",
                StatusCodes.Status503ServiceUnavailable);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var detail = await ReadErrorDetailAsync(response, cancellationToken);
                var numericStatusCode = (int)response.StatusCode;
                var statusCode = numericStatusCode is >= 400 and < 500
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status503ServiceUnavailable;
                throw new FaceServiceException(detail, statusCode);
            }

            EmbeddingResponse? result;
            try
            {
                result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(
                    cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                result = null;
            }
            if (result?.Embedding is not { Length: 512 } ||
                result.Embedding.Any(value => !float.IsFinite(value)))
            {
                logger.LogError("Face Service returned an invalid embedding");
                throw new FaceServiceException(
                    "Face Service ส่งผลลัพธ์ไม่ถูกต้อง",
                    StatusCodes.Status503ServiceUnavailable);
            }

            return Normalize(result.Embedding);
        }
    }

    private static float[] Normalize(float[] embedding)
    {
        var magnitude = Math.Sqrt(embedding.Sum(value => (double)value * value));
        if (magnitude <= 0)
            throw new FaceServiceException(
                "Face Service ส่งผลลัพธ์ไม่ถูกต้อง",
                StatusCodes.Status503ServiceUnavailable);

        return embedding.Select(value => (float)(value / magnitude)).ToArray();
    }

    private static async Task<string> ReadErrorDetailAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: cancellationToken);
            if (payload.TryGetProperty("detail", out var detail) &&
                detail.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(detail.GetString()))
            {
                return detail.GetString()!;
            }
        }
        catch (JsonException)
        {
            // Use a stable message when the service does not return its documented JSON shape.
        }

        return "Face Service ไม่สามารถประมวลผลภาพได้ กรุณาลองใหม่";
    }

    private sealed record EmbeddingResponse(float[] Embedding);
}

public sealed class FaceServiceException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
