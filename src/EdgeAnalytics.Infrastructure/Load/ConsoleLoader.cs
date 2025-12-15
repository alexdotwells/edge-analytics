using EdgeAnalytics.Abstractions.Load;

namespace EdgeAnalytics.Infrastructure.Load;

public sealed class ConsoleLoader : ILoader<string>
{
    public Task LoadAsync(string data, CancellationToken ct)
    {
        Console.WriteLine(data);
        return Task.CompletedTask;
    }
}
