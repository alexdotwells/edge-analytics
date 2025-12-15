using EdgeAnalytics.Abstractions.Pipeline;
using EdgeAnalytics.Domain.Common;

namespace EdgeAnalytics.Application;

public sealed class PipelineRunner
{
    private readonly IEnumerable<IPipeline> _pipelines;

    public PipelineRunner(IEnumerable<IPipeline> pipelines)
    {
        _pipelines = pipelines;
    }

    public async Task RunAsync(string pipelineName, Sport sport, CancellationToken ct)
    {
        var pipeline = _pipelines.Single(p => p.Name == pipelineName);

        var context = PipelineContext.Create(
            pipelineName: pipelineName,
            sport: sport
        );

        await pipeline.RunAsync(context, ct);
    }
}
