using EdgeAnalytics.Abstractions.Pipeline;

namespace EdgeAnalytics.Abstractions.Extract;

public interface IContextualExtractor<T>
{
    Task<T> ExtractAsync(PipelineContext context, CancellationToken ct);
}
