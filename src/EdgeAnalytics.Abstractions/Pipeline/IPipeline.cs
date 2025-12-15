namespace EdgeAnalytics.Abstractions.Pipeline;

public interface IPipeline
{
    string Name { get; }
    Task RunAsync(PipelineContext context, CancellationToken ct);
}
