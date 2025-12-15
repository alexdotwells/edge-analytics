using EdgeAnalytics.Abstractions.Pipeline;
using EdgeAnalytics.Domain.Common;
using Microsoft.Extensions.Logging;

namespace EdgeAnalytics.Application;

public sealed class PipelineRunner
{
    private readonly IEnumerable<IPipeline> _pipelines;
    private readonly IWatermarkStore _watermarks;
    private readonly ILogger<PipelineRunner> _logger;

    public PipelineRunner(
        IEnumerable<IPipeline> pipelines,
        IWatermarkStore watermarks,
        ILogger<PipelineRunner> logger)
    {
        _pipelines = pipelines;
        _watermarks = watermarks;
        _logger = logger;
    }

    public async Task RunAsync(string pipelineName, Sport sport, CancellationToken ct)
    {
        var pipeline = _pipelines.Single(p => p.Name == pipelineName);

        var last = await _watermarks.GetAsync(pipelineName, ct);
        var now = DateTimeOffset.UtcNow;

        var context = PipelineContext.Create(
            pipelineName: pipelineName,
            sport: sport,
            windowStartUtc: last,
            windowEndUtc: now,
            isBackfill: false
        );

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["RunId"] = context.RunId,
            ["Pipeline"] = context.PipelineName,
            ["Sport"] = context.Sport,
            ["WindowStartUtc"] = context.WindowStartUtc,
            ["WindowEndUtc"] = context.WindowEndUtc
        }))
        {
            _logger.LogInformation("Pipeline run started");
            await pipeline.RunAsync(context, ct);
            await _watermarks.SaveAsync(pipelineName, now, ct);
            _logger.LogInformation("Pipeline run completed");
        }
    }
}
