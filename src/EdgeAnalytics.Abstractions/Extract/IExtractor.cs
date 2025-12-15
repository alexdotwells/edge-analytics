namespace EdgeAnalytics.Abstractions.Extract;

public interface IExtractor<T>
{
    Task<T> ExtractAsync(CancellationToken ct);
}
