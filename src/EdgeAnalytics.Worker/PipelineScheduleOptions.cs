using EdgeAnalytics.Abstractions.Pipeline;

namespace EdgeAnalytics.Worker;

public sealed class PipelineScheduleOptions
{
    public List<PipelineSchedule> Pipelines { get; init; } = new();
}
