using EdgeAnalytics.Abstractions.Pipeline;
using EdgeAnalytics.Application.Execution;
using EdgeAnalytics.Domain.Common;
using Microsoft.Extensions.Logging;

namespace EdgeAnalytics.Application;

public sealed class PipelineRunner
{
    private readonly IEnumerable<IPipeline> _pipelines;
    private readonly IWatermarkStore _watermarks;
    private readonly PipelineRetryPolicyFactory _retryPolicyFactory;
    private readonly ILogger<PipelineRunner> _logger;

    public PipelineRunner(
        IEnumerable<IPipeline> pipelines,
        IWatermarkStore watermarks,
        PipelineRetryPolicyFactory retryPolicyFactory,
        ILogger<PipelineRunner> logger)
    {
        _pipelines = pipelines;
        _watermarks = watermarks;
        _retryPolicyFactory = retryPolicyFactory;
        _logger = logger;
    }

    public async Task RunAsync(string pipelineName, Sport sport, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var pipeline = ResolvePipeline(pipelineName);

        var context = await BuildContextAsync(pipelineName, sport, ct);

        ValidateWindow(context);

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["RunId"] = context.RunId,
            ["Pipeline"] = context.PipelineName,
            ["Sport"] = context.Sport.ToString(),
            ["WindowStartUtc"] = context.WindowStartUtc,
            ["WindowEndUtc"] = context.WindowEndUtc,
            ["IsBackfill"] = context.IsBackfill
        }))
        {
            _logger.LogInformation(
                "Pipeline run started. Window {Start} → {End}",
                context.WindowStartUtc,
                context.WindowEndUtc);

            var policy = _retryPolicyFactory.Create();

            try
            {
                PipelineMetrics.Runs.Add(1);

                // Cancellation-aware Polly execution
                await policy.ExecuteAsync(
                    async token => await pipeline.RunAsync(context, token),
                    ct);

                // Save watermark only after successful execution
                await _watermarks.SaveAsync(
                    pipelineName,
                    context.WindowEndUtc!.Value,
                    ct);

                _logger.LogInformation("Pipeline run completed");
            }
            catch (PipelineFailureException ex)
            {
                PipelineMetrics.Failures.Add(1);
                _logger.LogError(
                    ex,
                    "Pipeline failed with {FailureType} failure",
                    ex.FailureType);

                throw;
            }
        }
    }

    private IPipeline ResolvePipeline(string pipelineName)
    {
        var pipeline = _pipelines.SingleOrDefault(p => p.Name == pipelineName);

        if (pipeline is null)
        {
            throw new InvalidOperationException(
                $"No pipeline registered with name '{pipelineName}'. " +
                $"Registered pipelines: {string.Join(", ", _pipelines.Select(p => p.Name))}");
        }

        return pipeline;
    }

    private async Task<PipelineContext> BuildContextAsync(
        string pipelineName,
        Sport sport,
        CancellationToken ct)
    {
        var last = await _watermarks.GetAsync(pipelineName, ct);
        var now = DateTimeOffset.UtcNow;

        // Forward execution always has an "end" (now).
        // First run will be: [null → now]
        return PipelineContext.Create(
            pipelineName: pipelineName,
            sport: sport,
            windowStartUtc: last,
            windowEndUtc: now,
            isBackfill: false);
    }

    private static void ValidateWindow(PipelineContext context)
    {
        if (!context.WindowEndUtc.HasValue)
        {
            throw new InvalidOperationException(
                $"PipelineContext.WindowEndUtc must be set for pipeline '{context.PipelineName}'.");
        }

        if (context.WindowStartUtc.HasValue &&
            context.WindowStartUtc.Value >= context.WindowEndUtc.Value)
        {
            throw new InvalidOperationException(
                $"Invalid pipeline window for '{context.PipelineName}': " +
                $"start {context.WindowStartUtc:u} >= end {context.WindowEndUtc:u}");
        }
    }
}
