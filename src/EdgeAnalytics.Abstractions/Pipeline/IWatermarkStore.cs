namespace EdgeAnalytics.Abstractions.Pipeline;

public interface IWatermarkStore
{
    Task<DateTimeOffset?> GetAsync(string pipelineName, CancellationToken ct);
    Task SaveAsync(string pipelineName, DateTimeOffset watermarkUtc, CancellationToken ct);
}
