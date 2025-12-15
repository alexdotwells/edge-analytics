namespace EdgeAnalytics.Abstractions.Pipeline;

public interface IPipeline
{
    string Name { get; }
    Task RunAsync(CancellationToken ct);
}
