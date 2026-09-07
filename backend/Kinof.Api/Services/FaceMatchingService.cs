using System.Text.Json;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record FaceMatch(Guid UserId, double Score, bool Matched);

/// <summary>
/// 1:N face identification inside the API. Embeddings are 512-d unit vectors stored as
/// JSON by <see cref="AuthService.RegisterFaceAsync"/>, so a linear cosine scan over the
/// enrolled users costs one dot product each — fast enough for a single lab building and
/// it keeps the face service stateless (embeddings only).
/// </summary>
public sealed class FaceMatchingService(AppDbContext db, ILogger<FaceMatchingService> logger)
{
    /// <summary>docs/AUTH_ADAPTIVE.md — 0.5 ขึ้นไปผ่านทันที, ต่ำกว่านั้นไปทาง Entry OTP.</summary>
    public const double MatchThreshold = 0.5;

    public const int EmbeddingLength = 512;

    /// <summary>
    /// Returns the closest enrolled user, or <c>null</c> when nobody in the system has a
    /// usable embedding yet.
    /// </summary>
    public async Task<FaceMatch?> IdentifyAsync(float[] probe, CancellationToken cancellationToken)
    {
        if (probe.Length != EmbeddingLength)
            return null;

        var candidates = await db.FaceEmbeddings
            .AsNoTracking()
            .Join(
                db.Users.AsNoTracking().Where(user =>
                    user.Status == UserStatus.Active && user.FaceEnrolled),
                embedding => embedding.UserId,
                user => user.Id,
                (embedding, user) => new { UserId = user.Id, embedding.Embedding })
            .ToListAsync(cancellationToken);

        FaceMatch? best = null;
        foreach (var candidate in candidates)
        {
            if (!TryReadEmbedding(candidate.Embedding, out var stored))
            {
                logger.LogWarning(
                    "Skipping unusable face embedding for user {UserId}",
                    candidate.UserId);
                continue;
            }

            var score = CosineSimilarity(probe, stored);
            if (best is null || score > best.Score)
                best = new FaceMatch(candidate.UserId, score, score >= MatchThreshold);
        }

        return best;
    }

    public static double CosineSimilarity(float[] left, float[] right)
    {
        double dot = 0;
        double leftSquares = 0;
        double rightSquares = 0;
        for (var index = 0; index < left.Length; index++)
        {
            dot += (double)left[index] * right[index];
            leftSquares += (double)left[index] * left[index];
            rightSquares += (double)right[index] * right[index];
        }

        var magnitude = Math.Sqrt(leftSquares) * Math.Sqrt(rightSquares);
        return magnitude <= 0 ? 0 : dot / magnitude;
    }

    private static bool TryReadEmbedding(string json, out float[] embedding)
    {
        embedding = [];
        try
        {
            var values = JsonSerializer.Deserialize<float[]>(json);
            if (values is not { Length: EmbeddingLength } ||
                values.Any(value => !float.IsFinite(value)))
            {
                return false;
            }

            embedding = values;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
