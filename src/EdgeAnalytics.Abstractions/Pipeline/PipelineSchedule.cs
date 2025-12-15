namespace EdgeAnalytics.Abstractions.Pipeline;

public sealed record PipelineSchedule
(
    string PipelineName,
    PipelineExecutionPolicy Policy,
    TimeSpan? Interval
);
