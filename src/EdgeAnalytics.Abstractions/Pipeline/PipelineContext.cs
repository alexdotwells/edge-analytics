using EdgeAnalytics.Domain.Common;

namespace EdgeAnalytics.Abstractions.Pipeline;

public sealed record PipelineContext
(
    Guid RunId,
    string PipelineName,
    Sport Sport,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? WindowStartUtc,
    DateTimeOffset? WindowEndUtc,
    bool IsBackfill
)
{
    public static PipelineContext Create(
        string pipelineName,
        Sport sport,
        DateTimeOffset? windowStartUtc = null,
        DateTimeOffset? windowEndUtc = null,
        bool isBackfill = false)
        => new(
            Guid.NewGuid(),
            pipelineName,
            sport,
            DateTimeOffset.UtcNow,
            windowStartUtc,
            windowEndUtc,
            isBackfill
        );
}
