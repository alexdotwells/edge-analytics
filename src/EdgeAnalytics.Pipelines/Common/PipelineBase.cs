using EdgeAnalytics.Abstractions.Extract;
using EdgeAnalytics.Abstractions.Load;
using EdgeAnalytics.Abstractions.Pipeline;
using EdgeAnalytics.Abstractions.Transform;

namespace EdgeAnalytics.Pipelines.Common;

public abstract class PipelineBase<TExtracted, TLoaded> : IPipeline
{
    public abstract string Name { get; }

    private readonly IExtractor<TExtracted> _extractor;
    private readonly IReadOnlyList<ITransformer<object, object>> _transformers;
    private readonly ILoader<TLoaded> _loader;

    protected PipelineBase(
        IExtractor<TExtracted> extractor,
        IEnumerable<ITransformer<object, object>> transformers,
        ILoader<TLoaded> loader)
    {
        _extractor = extractor;
        _transformers = transformers.ToList();
        _loader = loader;
    }

    public async Task RunAsync(PipelineContext context, CancellationToken ct)
    {
        object current = await _extractor.ExtractAsync(ct);

        foreach (var transformer in _transformers)
        {
            current = await transformer.TransformAsync(current, ct);
        }

        await _loader.LoadAsync((TLoaded)current, ct);
    }
}
