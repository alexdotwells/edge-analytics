using EdgeAnalytics.Abstractions.Transform;

namespace EdgeAnalytics.Infrastructure.Transform.Normalize;

public sealed class UppercaseTransformer : ITransformer<object, object>
{
    public Task<object> TransformAsync(object input, CancellationToken ct)
    {
        var value = input.ToString() ?? string.Empty;
        return Task.FromResult<object>(value.ToUpperInvariant());
    }
}
