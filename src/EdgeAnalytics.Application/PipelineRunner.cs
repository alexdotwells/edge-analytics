using EdgeAnalytics.Abstractions.Pipeline;

namespace EdgeAnalytics.Application;

public sealed class PipelineRunner
{
    private readonly IEnumerable<IPipeline> _pipelines;

    public PipelineRunner(IEnumerable<IPipeline> pipelines)
    {
        _pipelines = pipelines;
    }

    public async Task RunAsync(string pipelineName, CancellationToken ct)
    {
        var pipeline = _pipelines.Single(p => p.Name == pipelineName);
        await pipeline.RunAsync(ct);
    }
}
