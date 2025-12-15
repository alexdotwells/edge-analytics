namespace EdgeAnalytics.Abstractions.Transform;

public interface ITransformer<TIn, TOut>
{
    Task<TOut> TransformAsync(TIn input, CancellationToken ct);
}
