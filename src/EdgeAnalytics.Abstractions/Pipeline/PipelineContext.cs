using EdgeAnalytics.Domain.Common;

namespace EdgeAnalytics.Abstractions.Pipeline;

public sealed record PipelineContext
(
    Guid RunId,
    string PipelineName,
    Sport Sport,
    DateTimeOffset StartedAtUtc
)
{
    public static PipelineContext Create(string pipelineName, Sport sport)
        => new(
            Guid.NewGuid(),
            pipelineName,
            sport,
            DateTimeOffset.UtcNow
        );
}
