using EdgeAnalytics.Abstractions.Pipeline;
using System.Text.Json;

namespace EdgeAnalytics.Infrastructure.Pipeline;

public sealed class FileWatermarkStore : IWatermarkStore
{
    private readonly string _path = Path.Combine(AppContext.BaseDirectory, "watermarks.json");

    public async Task<DateTimeOffset?> GetAsync(string pipelineName, CancellationToken ct)
    {
        if (!File.Exists(_path)) return null;

        var json = await File.ReadAllTextAsync(_path, ct);
        var dict = JsonSerializer.Deserialize<Dictionary<string, DateTimeOffset>>(json)
                   ?? new();

        return dict.TryGetValue(pipelineName, out var value) ? value : null;
    }

    public async Task SaveAsync(string pipelineName, DateTimeOffset watermarkUtc, CancellationToken ct)
    {
        Dictionary<string, DateTimeOffset> dict = new();

        if (File.Exists(_path))
        {
            var json = await File.ReadAllTextAsync(_path, ct);
            dict = JsonSerializer.Deserialize<Dictionary<string, DateTimeOffset>>(json) ?? new();
        }

        dict[pipelineName] = watermarkUtc;

        var output = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_path, output, ct);
    }
}
