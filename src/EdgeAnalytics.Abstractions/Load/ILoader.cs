namespace EdgeAnalytics.Abstractions.Load;

public interface ILoader<T>
{
    Task LoadAsync(T data, CancellationToken ct);
}
