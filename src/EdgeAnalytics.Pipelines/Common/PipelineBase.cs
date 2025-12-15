using EdgeAnalytics.Abstractions.Extract;
using EdgeAnalytics.Abstractions.Load;
using EdgeAnalytics.Abstractions.Pipeline;
using EdgeAnalytics.Abstractions.Transform;
using Microsoft.Extensions.Logging;

namespace EdgeAnalytics.Pipelines.Common;

public abstract class PipelineBase<TExtracted, TLoaded> : IPipeline
{
    public abstract string Name { get; }

    private readonly IExtractor<TExtracted>? _extractor;
    private readonly IContextualExtractor<TExtracted>? _contextualExtractor;
    private readonly IReadOnlyList<ITransformer<object, object>> _transformers;
    private readonly ILoader<TLoaded> _loader;
    private readonly ILogger _logger;

    protected PipelineBase(
        IExtractor<TExtracted>? extractor,
        IContextualExtractor<TExtracted>? contextualExtractor,
        IEnumerable<ITransformer<object, object>> transformers,
        ILoader<TLoaded> loader,
        ILogger logger)
    {
        _extractor = extractor;
        _contextualExtractor = contextualExtractor;
        _transformers = transformers.ToList();
        _loader = loader;
        _logger = logger;
    }

    public async Task RunAsync(PipelineContext context, CancellationToken ct)
    {
        _logger.LogInformation("Pipeline execution started");

        object current;

        if (_contextualExtractor is not null)
        {
            _logger.LogInformation("Using contextual extractor");
            current = await _contextualExtractor.ExtractAsync(context, ct);
        }
        else if (_extractor is not null)
        {
            _logger.LogInformation("Using legacy extractor");
            current = await _extractor.ExtractAsync(ct);
        }
        else
        {
            throw new InvalidOperationException(
                $"No extractor registered for pipeline '{Name}'.");
        }

        foreach (var transformer in _transformers)
        {
            _logger.LogInformation(
                "Applying transformer {Transformer}",
                transformer.GetType().Name);

            current = await transformer.TransformAsync(current, ct);
        }

        await _loader.LoadAsync((TLoaded)current, ct);

        _logger.LogInformation("Pipeline execution finished");
    }
    
}
