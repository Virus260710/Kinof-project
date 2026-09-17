using System.Text.Json;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record FaceMatch(Guid UserId, double Score, bool Matched, double SecondScore = 0);

public sealed record FaceScore(Guid UserId, double Score);

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

    /// <summary>
    /// Best match must beat the runner-up by this much so lookalike enrolled accounts
    /// are not treated as a unique identification.
    /// </summary>
    public const double MinScoreGap = 0.08;

    public const int EmbeddingLength = 512;

    /// <summary>
    /// Returns the closest enrolled user, or <c>null</c> when nobody in the system has a
    /// usable embedding yet.
    /// </summary>
    public async Task<IReadOnlyList<FaceScore>> RankAsync(
        float[] probe,
        CancellationToken cancellationToken)
    {
        if (probe.Length != EmbeddingLength)
            return [];

        var candidates = await db.FaceEmbeddings
            .AsNoTracking()
            .Join(
                db.Users.AsNoTracking().Where(user =>
                    user.Status == UserStatus.Active && user.FaceEnrolled),
                embedding => embedding.UserId,
                user => user.Id,
                (embedding, user) => new { UserId = user.Id, embedding.Embedding })
            .ToListAsync(cancellationToken);

        var ranked = new List<FaceScore>(candidates.Count);
        foreach (var candidate in candidates)
        {
            if (!TryReadEmbedding(candidate.Embedding, out var stored))
            {
                logger.LogWarning(
                    "Skipping unusable face embedding for user {UserId}",
                    candidate.UserId);
                continue;
            }

            ranked.Add(new FaceScore(candidate.UserId, CosineSimilarity(probe, stored)));
        }

        return ranked
            .OrderByDescending(item => item.Score)
            .ToList();
    }

    public async Task<FaceMatch?> IdentifyAsync(float[] probe, CancellationToken cancellationToken)
    {
        var ranked = await RankAsync(probe, cancellationToken);
        if (ranked.Count == 0)
            return null;

        var best = ranked[0];
        var secondScore = ranked.Count > 1 ? ranked[1].Score : 0;
        var unique = best.Score >= MatchThreshold && best.Score - secondScore >= MinScoreGap;
        return new FaceMatch(best.UserId, best.Score, unique, secondScore);
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
            if (values is { Length: EmbeddingLength } &&
                values.All(value => float.IsFinite(value)))
            {
                embedding = values;
                return true;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
