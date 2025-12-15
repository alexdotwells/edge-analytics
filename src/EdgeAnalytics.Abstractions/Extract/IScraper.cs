namespace EdgeAnalytics.Abstractions.Extract;

public interface IScraper
{
    Task<string> FetchAsync(
        Uri uri,
        CancellationToken ct);
}
